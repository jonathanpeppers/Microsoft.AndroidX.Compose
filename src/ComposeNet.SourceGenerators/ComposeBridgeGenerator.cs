using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ComposeNet.SourceGenerators;

/// <summary>
/// Emits JNI-bridge bodies for <c>partial</c> methods on
/// <c>ComposeNet.ComposeBridges</c> decorated with
/// <c>[ComposeBridge(...)]</c>. The author writes a stub like
/// <code>
/// [ComposeBridge(Class="...Kt", JvmName="Foo", Signature="(...III)V",
///                Defaults=typeof(FooDefault))]
/// public static partial void Foo(IFunction0 onClick, IModifier? modifier,
///                                IFunction3 content, IComposer composer);
/// </code>
/// and the generator emits the cache fields, lazy class/method ID
/// resolution, JValue array fill, <c>$default</c> bitmask, and
/// <c>try</c>/<c>finally</c> with <c>GC.KeepAlive</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ComposeBridgeGenerator : IIncrementalGenerator
{
    const string BridgeAttributeMetadataName = "ComposeNet.ComposeBridgeAttribute";
    const string DeclarativeDefaultsAttributeMetadataName = "ComposeNet.ComposeDefaultsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Attributes.cs (shared with ComposeDefaultsGenerator) already emits
        // ComposeBridgeAttribute via RegisterPostInitializationOutput.

        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is MethodDeclarationSyntax m &&
                    m.Modifiers.Any(t => t.IsKind(SyntaxKind.PartialKeyword)) &&
                    m.AttributeLists.Count > 0,
                transform: static (ctx, _) => ctx)
            .Where(static ctx => ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node) is IMethodSymbol);

        var combined = methods.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (ctx, compilation) = pair;
            var bridgeAttr = compilation.GetTypeByMetadataName(BridgeAttributeMetadataName);
            if (bridgeAttr is null) return;

            var declarativeAttr = compilation.GetTypeByMetadataName(DeclarativeDefaultsAttributeMetadataName);

            var method = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node)!;
            var attr = method.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bridgeAttr));
            if (attr is null) return;

            var result = Build(method, attr, declarativeAttr, compilation);
            foreach (var diag in result.Diagnostics)
                spc.ReportDiagnostic(diag);
            if (result.Source is { } source && result.HintName is { } hint)
                spc.AddSource(hint, SourceText.From(source, Encoding.UTF8));
        });
    }

    static GenerationResult Build(IMethodSymbol method, AttributeData attr, INamedTypeSymbol? declarativeAttr, Compilation compilation)
    {
        var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
        string? className = ReadString(attr, "Class");
        string? jvmName = ReadString(attr, "JvmName");
        string? signature = ReadString(attr, "Signature");
        string? instanceField = ReadString(attr, "InstanceField");
        var defaultsType = ReadType(attr, "Defaults");

        if (className is null || jvmName is null || signature is null)
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.MalformedAttribute, loc, method.Name)
            });
        }

        if (!JniSignature.TryParse(signature, out var sigParams, out _, out var sigError))
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.BridgeMalformedSignature, loc, method.Name, sigError ?? "?")
            });
        }

        // Detect the JNI signature "shape". Five supported today:
        //   1. ComposableWithDefault — ...Composer;I+ trailing (multiple Is).
        //   2. ComposableNoDefault   — ...Composer;I  trailing (single I).
        //   3. ExtensionWithDefault  — non-@Composable Kotlin extension
        //      function with default values; tail is `I L<marker>`,
        //      no Composer. The marker is always Ljava/lang/Object; and
        //      is passed `null` (IntPtr.Zero).
        //   4. PlainStatic           — plain Kotlin static method on a Kt
        //      class with no Composer slot and no $default bitmask; just
        //      (args…)Return. Used for Modifier-chain helpers and other
        //      synchronous Kotlin utilities (e.g. RoundedCornerShape).
        //   5. Constructor           — JvmName is "<init>". Emits
        //      GetMethodID + NewObject and wraps the returned handle via
        //      Java.Lang.Object.GetObject<TReturn>(.., TransferLocalRef).
        //      Used for stripped Kotlin ctors whose parameters were
        //      mangled by inline-class compilation (e.g.
        //      GridCells.Adaptive(Dp)).
        bool isConstructor = jvmName == "<init>";
        if (isConstructor)
        {
            // Ctor-specific validation: no Composer, no $default, no
            // InstanceField, no Defaults attr, non-void return,
            // signature must end with `V`.
            if (instanceField is not null)
                return ConstructorError(loc, method.Name, "InstanceField is not valid with a constructor bridge");
            if (defaultsType is not null)
                return ConstructorError(loc, method.Name, "Defaults is not valid with a constructor bridge");
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if (ComposeDefaultsGenerator.IsComposer(method.Parameters[i].Type))
                    return ConstructorError(loc, method.Name, "constructors have no Composer slot — remove the IComposer parameter");
            }
            if (method.ReturnsVoid)
                return ConstructorError(loc, method.Name, "return type must be the constructed object, not void");
            // Signature must end with `V` (JVM constructors return void at
            // the bytecode level even though NewObject hands back a handle).
            if (!signature.EndsWith(")V", StringComparison.Ordinal))
                return ConstructorError(loc, method.Name, "JNI signature must end with ')V' (constructors return void at the bytecode level)");
        }

        int composerSigIdx = -1;
        if (!isConstructor)
        {
            for (int i = 0; i < sigParams.Count; i++)
            {
                if (sigParams[i].Code == 'L' && sigParams[i].ClassName == "androidx/compose/runtime/Composer")
                {
                    composerSigIdx = i;
                    break;
                }
            }
        }
        bool hasComposerSlot = composerSigIdx >= 0;

        bool extensionWithDefault = false;
        bool signatureHasDefault;
        if (isConstructor)
        {
            // Ctor shape never has $default or extension marker — every
            // C# user param maps positionally to a JNI slot.
            signatureHasDefault = false;
        }
        else if (hasComposerSlot)
        {
            // Kotlin's @Composable codegen emits ceil(userParams/10) (min 1)
            // $changed groups, and one $default slot iff at least one
            // parameter has a default value:
            //   trailingI = $changedGroups + ($default ? 1 : 0)
            int trailingInts = 0;
            for (int i = sigParams.Count - 1; i >= 0 && sigParams[i].Code == 'I' && sigParams[i].ArrayDepth == 0; i--)
                trailingInts++;
            int sigUserParamCount = sigParams.Count - 1 /*composer*/ - trailingInts;
            int expectedChangedSlots = Math.Max(1, (sigUserParamCount + 9) / 10);
            signatureHasDefault = trailingInts > expectedChangedSlots;
        }
        else if (sigParams.Count >= 2 &&
                 sigParams[sigParams.Count - 1].Code == 'L' &&
                 sigParams[sigParams.Count - 1].ClassName == "java/lang/Object" &&
                 sigParams[sigParams.Count - 1].ArrayDepth == 0 &&
                 sigParams[sigParams.Count - 2].Code == 'I' &&
                 sigParams[sigParams.Count - 2].ArrayDepth == 0)
        {
            // Non-@Composable extension function with $default + synthetic
            // marker. Kotlin's bytecode passes the marker as null at every
            // call site; we always emit IntPtr.Zero for that slot.
            extensionWithDefault = true;
            signatureHasDefault = true;
        }
        else
        {
            // Plain static call, no Composer, no $default — e.g.
            // PaddingKt.padding(Modifier, Dp), RoundedCornerShape(Dp).
            // Every Kotlin parameter is required; the C# stub lists them
            // positionally (with an optional leading IntPtr receiver
            // when the Kotlin function is an extension method).
            signatureHasDefault = false;
        }

        bool hasDefaultSlot = signatureHasDefault;

        // Cross-validate signature against attribute metadata: a mismatch
        // would silently produce a broken bridge (wrong $default slot
        // packing, missing bitmask, etc.) so we fail fast.
        if (hasDefaultSlot && defaultsType is null)
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.BridgeDefaultsMismatch, loc, method.Name,
                    "JNI signature has a $default slot but [ComposeBridge] omits 'Defaults'")
            });
        }
        if (!hasDefaultSlot && defaultsType is not null)
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.BridgeDefaultsMismatch, loc, method.Name,
                    "[ComposeBridge] specifies 'Defaults' but the JNI signature has no $default slot")
            });
        }

        // Locate the matching declarative [ComposeDefaults] attribute, if any.
        // When [ComposeBridge] omits Defaults the bridge targets a
        // @Composable function whose Kotlin codegen emitted only $changed
        // slots (no $default bitmask) — every parameter is required.
        IReadOnlyList<string> kotlinNames;
        string? defaultsEnumName = defaultsType?.Name;
        if (defaultsType is not null && declarativeAttr is not null)
        {
            var match = compilation.Assembly.GetAttributes()
                .FirstOrDefault(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, declarativeAttr) &&
                    a.ConstructorArguments.Length >= 1 &&
                    a.ConstructorArguments[0].Value is string s &&
                    s == defaultsEnumName);
            if (match is null)
            {
                return new GenerationResult(null, null, new[] {
                    Diagnostic.Create(Diagnostics.BridgeMissingDefaults, loc, defaultsEnumName ?? "?", method.Name)
                });
            }
            var arr = match.ConstructorArguments[1];
            kotlinNames = arr.Kind == TypedConstantKind.Array
                ? arr.Values.Select(v => v.Value as string ?? string.Empty).ToArray()
                : Array.Empty<string>();
        }
        else if (defaultsType is not null)
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.BridgeMissingDefaults, loc, defaultsEnumName ?? "?", method.Name)
            });
        }
        else
        {
            kotlinNames = Array.Empty<string>();
        }

        // C# parameter walk. For composable shapes the last param must be
        // an IComposer; for extension shapes there is no Composer slot and
        // the last param is just the trailing user parameter.
        var csParams = method.Parameters;
        IParameterSymbol? composerParam;
        int csTail; // exclusive upper bound of "user-controlled" params before composer
        if (hasComposerSlot)
        {
            if (csParams.Length == 0 || !ComposeDefaultsGenerator.IsComposer(csParams[csParams.Length - 1].Type))
            {
                return new GenerationResult(null, null, new[] {
                    Diagnostic.Create(Diagnostics.MalformedAttribute, loc, method.Name)
                });
            }
            composerParam = csParams[csParams.Length - 1];
            csTail = csParams.Length - 1;
        }
        else
        {
            composerParam = null;
            csTail = csParams.Length;
        }

        // Detect optional `int defaults` immediately before composer (or
        // at the very end for extension shapes). Caller-controlled bitmask
        // is only meaningful when the function actually has a $default slot.
        bool callerProvidesDefaults = false;
        int userTail = csTail;
        if (hasDefaultSlot && userTail > 0 &&
            csParams[userTail - 1].Type.SpecialType == SpecialType.System_Int32 &&
            csParams[userTail - 1].Name == "defaults")
        {
            callerProvidesDefaults = true;
            userTail -= 1;
        }
        var userParams = csParams.Take(userTail).ToArray();

        // Detect leading extension receiver. For composable shapes the
        // convention is `IntPtr <name>Scope` (e.g. `IntPtr rowScope`); for
        // extension-with-default shapes the receiver is whatever the first
        // sigParam is (e.g. Modifier) and we bind it positionally to the
        // first IntPtr C# parameter regardless of name; for the plain-static
        // shape we treat the first user param as the receiver iff the
        // first sigParam is an object (`L`) AND the first C# param is
        // `IntPtr` — that covers Modifier-chain extensions while leaving
        // non-extension static calls (e.g. `RoundedCornerShape(Dp)`) alone.
        IParameterSymbol? receiverParam = null;
        if (extensionWithDefault)
        {
            if (userParams.Length == 0 ||
                userParams[0].Type.SpecialType != SpecialType.System_IntPtr)
            {
                return new GenerationResult(null, null, new[] {
                    Diagnostic.Create(Diagnostics.MalformedAttribute, loc, method.Name)
                });
            }
            receiverParam = userParams[0];
            userParams = userParams.Skip(1).ToArray();
        }
        else if (hasComposerSlot &&
                 userParams.Length > 0 &&
                 userParams[0].Type.SpecialType == SpecialType.System_IntPtr &&
                 userParams[0].Name.EndsWith("Scope", StringComparison.Ordinal))
        {
            receiverParam = userParams[0];
            userParams = userParams.Skip(1).ToArray();
        }
        else if (!hasComposerSlot && !extensionWithDefault && !isConstructor &&
                 userParams.Length > 0 && sigParams.Count > 0 &&
                 userParams[0].Type.SpecialType == SpecialType.System_IntPtr &&
                 sigParams[0].Code == 'L' && sigParams[0].ArrayDepth == 0)
        {
            receiverParam = userParams[0];
            userParams = userParams.Skip(1).ToArray();
        }

        // Match each remaining user param to a Kotlin bit position.
        // With $default: by Kotlin name from the declarative list.
        // Without $default: positionally — the user param at index N goes
        // into JNI slot N (after any extension receiver).
        var kotlinNameMap = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int bit = 0; bit < kotlinNames.Count; bit++)
        {
            var raw = kotlinNames[bit];
            if (raw.Length > 0 && raw[0] == '!') raw = raw.Substring(1);
            if (!string.IsNullOrEmpty(raw) && !kotlinNameMap.ContainsKey(raw))
                kotlinNameMap[raw] = bit;
        }

        // userParam name -> kotlin bit
        var userBitOf = new Dictionary<string, int>(StringComparer.Ordinal);
        var diags = new List<Diagnostic>();
        if (hasDefaultSlot)
        {
            foreach (var p in userParams)
            {
                if (!kotlinNameMap.TryGetValue(p.Name, out var bit))
                {
                    diags.Add(Diagnostic.Create(Diagnostics.BridgeUnknownParameter, loc,
                        method.Name, p.Name, defaultsEnumName ?? "?"));
                    continue;
                }
                userBitOf[p.Name] = bit;
            }
        }
        else
        {
            for (int i = 0; i < userParams.Length; i++)
                userBitOf[userParams[i].Name] = i;
        }
        if (diags.Count > 0)
            return new GenerationResult(null, null, diags.ToArray());

        // CN2007: recognized Compose value types (Color/Dp/Sp/Em/TextAlign)
        // rely on the auto-default-mask logic to leave the $default bit
        // set when null. Without a $default slot there's no way to
        // signal "use Kotlin default" to the call, so reject them up
        // front rather than silently emitting a value-zeroed argument.
        if (!hasDefaultSlot)
        {
            foreach (var p in userParams)
            {
                if (ComposeValueTypes.TryGet(p.Type, out var vtName, out _))
                {
                    diags.Add(Diagnostic.Create(Diagnostics.BridgeValueTypeNeedsDefaults, loc,
                        method.Name, p.Name, vtName));
                }
            }
            if (diags.Count > 0)
                return new GenerationResult(null, null, diags.ToArray());
        }

        var changedSlots = ChangedSlotCount(sigParams, hasDefaultSlot, hasComposerSlot);
        int defaultSlotCount = hasDefaultSlot ? 1 : 0;
        int receiverSlotCount = receiverParam is null ? 0 : 1;
        int composerSlotCount = hasComposerSlot ? 1 : 0;
        int markerSlotCount = extensionWithDefault ? 1 : 0;
        int actualUserSlots = sigParams.Count - receiverSlotCount - composerSlotCount - changedSlots - defaultSlotCount - markerSlotCount;
        int expectedUserSlots = hasDefaultSlot ? kotlinNames.Count : userParams.Length;
        if (actualUserSlots != expectedUserSlots)
        {
            diags.Add(Diagnostic.Create(Diagnostics.BridgeSignatureMismatch, loc,
                method.Name, actualUserSlots, defaultsEnumName ?? "?", expectedUserSlots));
            return new GenerationResult(null, null, diags.ToArray());
        }

        var source = Emit(method, attr, className, jvmName, signature, defaultsEnumName, sigParams,
            kotlinNames, userParams, userBitOf, receiverParam, callerProvidesDefaults, hasDefaultSlot,
            hasComposerSlot, extensionWithDefault, instanceField, isConstructor);
        var hint = $"ComposeNet.{method.ContainingType.Name}.{method.Name}.g.cs";
        return new GenerationResult(source, hint, Array.Empty<Diagnostic>());
    }

    static GenerationResult ConstructorError(Location loc, string methodName, string message) =>
        new(null, null, new[] {
            Diagnostic.Create(Diagnostics.BridgeConstructorShape, loc, methodName, message)
        });

    /// <summary>
    /// Number of trailing <c>I</c> params that represent <c>$changed</c>
    /// slots. <c>$changed</c> only exists in @Composable signatures, so
    /// when <paramref name="hasComposer"/> is <c>false</c> this returns
    /// <c>0</c>. With <paramref name="hasDefaultSlot"/>=<c>true</c> the
    /// very last <c>I</c> is the <c>$default</c> bitmask, so this returns
    /// <c>trailing-1</c>; otherwise every trailing <c>I</c> is part of
    /// the <c>$changed</c> group(s).
    /// </summary>
    static int ChangedSlotCount(IReadOnlyList<JniType> sigParams, bool hasDefaultSlot, bool hasComposer)
    {
        if (!hasComposer) return 0;
        int trailingInts = 0;
        for (int i = sigParams.Count - 1; i >= 0 && sigParams[i].Code == 'I' && sigParams[i].ArrayDepth == 0; i--)
            trailingInts++;
        return hasDefaultSlot ? Math.Max(0, trailingInts - 1) : trailingInts;
    }

    static string Emit(
        IMethodSymbol method, AttributeData _attr,
        string className, string jvmName, string signature, string? enumName,
        IReadOnlyList<JniType> sigParams,
        IReadOnlyList<string> kotlinNames,
        IParameterSymbol[] userParams,
        Dictionary<string, int> userBitOf,
        IParameterSymbol? receiverParam,
        bool callerProvidesDefaults,
        bool hasDefaultSlot,
        bool hasComposerSlot,
        bool extensionWithDefault,
        string? instanceField,
        bool isConstructor)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by ComposeNet.SourceGenerators.ComposeBridgeGenerator.");
        sb.AppendLine("#nullable enable");

        var containingType = method.ContainingType;
        var ns = containingType.ContainingNamespace?.IsGlobalNamespace == false
            ? containingType.ContainingNamespace.ToDisplayString()
            : null;
        if (ns is not null)
        {
            sb.Append("namespace ").Append(ns).AppendLine(";");
        }

        var typeKeyword = containingType.IsStatic ? "static partial class" : (containingType.IsValueType ? "partial struct" : "partial class");
        sb.Append(typeKeyword).Append(' ').AppendLine(containingType.Name);
        sb.AppendLine("{");

        // Cache fields. Suffix with method name to avoid clashes when one
        // ComposeBridges file declares multiple bridges on the same Kt class.
        var sym = SafeSym(method.Name);
        sb.Append("    static global::System.IntPtr s_").Append(sym).AppendLine("_class;");
        sb.Append("    static global::System.IntPtr s_").Append(sym).AppendLine("_method;");
        if (instanceField is not null)
            sb.Append("    static global::System.IntPtr s_").Append(sym).AppendLine("_instance;");
        sb.AppendLine();

        // Method signature — match the partial declaration.
        EmitMethodSignature(sb, method);
        sb.AppendLine("    {");

        // Lazy init.
        sb.Append("        if (s_").Append(sym).AppendLine("_method == global::System.IntPtr.Zero)");
        sb.AppendLine("        {");
        sb.Append("            s_").Append(sym).Append("_class = global::Android.Runtime.JNIEnv.FindClass(\"").Append(className).AppendLine("\");");
        if (isConstructor)
        {
            // Constructor ID lookup is GetMethodID with name "<init>".
            sb.Append("            s_").Append(sym).Append("_method = global::Android.Runtime.JNIEnv.GetMethodID(s_")
              .Append(sym).Append("_class, \"").Append(jvmName).Append("\", \"").Append(signature).AppendLine("\");");
        }
        else if (instanceField is null)
        {
            sb.Append("            s_").Append(sym).Append("_method = global::Android.Runtime.JNIEnv.GetStaticMethodID(s_")
              .Append(sym).Append("_class, \"").Append(jvmName).Append("\", \"").Append(signature).AppendLine("\");");
        }
        else
        {
            sb.Append("            var __fid = global::Android.Runtime.JNIEnv.GetStaticFieldID(s_").Append(sym)
              .Append("_class, \"").Append(instanceField).Append("\", \"L").Append(className).AppendLine(";\");");
            sb.Append("            var __local = global::Android.Runtime.JNIEnv.GetStaticObjectField(s_").Append(sym).AppendLine("_class, __fid);");
            sb.Append("            s_").Append(sym).AppendLine("_instance = global::Android.Runtime.JNIEnv.NewGlobalRef(__local);");
            sb.AppendLine("            global::Android.Runtime.JNIEnv.DeleteLocalRef(__local);");
            sb.Append("            s_").Append(sym).Append("_method = global::Android.Runtime.JNIEnv.GetMethodID(s_")
              .Append(sym).Append("_class, \"").Append(jvmName).Append("\", \"").Append(signature).AppendLine("\");");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // Defaults computation (only when caller didn't pass one and the
        // function actually has a $default slot).
        if (hasDefaultSlot && !callerProvidesDefaults)
        {
            sb.Append("        int defaults = (int)global::ComposeNet.").Append(enumName).AppendLine(".All;");
            foreach (var p in userParams)
            {
                if (!userBitOf.TryGetValue(p.Name, out var bit)) continue;
                // Skip bits whose Kotlin name has '!' prefix — no enum member.
                if (kotlinNames[bit].StartsWith("!", StringComparison.Ordinal)) continue;

                var member = PascalCase(p.Name);
                bool nullableValueType = ComposeValueTypes.TryGet(p.Type, out _, out _);
                if (p.NullableAnnotation == NullableAnnotation.Annotated || p.Type.IsReferenceType || nullableValueType || IsNullableIntPtr(p.Type))
                {
                    sb.Append("        if (").Append(EscapeIdent(p.Name)).Append(" is not null) defaults &= ~(int)global::ComposeNet.")
                      .Append(enumName).Append('.').Append(member).AppendLine(";");
                }
                else
                {
                    sb.Append("        defaults &= ~(int)global::ComposeNet.").Append(enumName).Append('.').Append(member).AppendLine(";");
                }
            }
            sb.AppendLine();
        }

        // String NewString hoist.
        var stringParams = userParams.Where(p => p.Type.SpecialType == SpecialType.System_String).ToList();
        foreach (var sp in stringParams)
        {
            sb.Append("        global::System.IntPtr __ref_").Append(sp.Name)
              .Append(" = global::Android.Runtime.JNIEnv.NewString(").Append(EscapeIdent(sp.Name)).AppendLine(");");
        }

        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            unsafe");
        sb.AppendLine("            {");

        int argCount = sigParams.Count + (instanceField is not null ? 0 : 0); // sigParams already excludes 'this'
        sb.Append("                global::Android.Runtime.JValue* args = stackalloc global::Android.Runtime.JValue[").Append(argCount).AppendLine("];");

        // Walk sigParams positions and emit assignments.
        // Layout: receiver? + kotlin params + composer + $changed* + $default.
        int idx = 0;
        if (receiverParam is not null)
        {
            sb.Append("                args[").Append(idx).Append("] = new global::Android.Runtime.JValue(").Append(EscapeIdent(receiverParam.Name)).AppendLine(");");
            idx++;
        }

        // Kotlin user params: walk one bit per JNI slot before composer.
        int userSlotCount = hasDefaultSlot ? kotlinNames.Count : userParams.Length;
        for (int bit = 0; bit < userSlotCount; bit++, idx++)
        {
            var sigType = sigParams[idx];
            var supplier = userParams.FirstOrDefault(p => userBitOf.TryGetValue(p.Name, out var b) && b == bit);
            sb.Append("                args[").Append(idx).Append("] = new global::Android.Runtime.JValue(");
            if (supplier is null)
            {
                sb.Append(sigType.ZeroLiteral);
            }
            else
            {
                EmitUserArgValue(sb, supplier, sigType);
            }
            sb.AppendLine(");");
        }

        // Composer (only present in @Composable shapes).
        IParameterSymbol? composer = hasComposerSlot ? method.Parameters[method.Parameters.Length - 1] : null;
        if (composer is not null)
        {
            sb.Append("                args[").Append(idx).Append("] = new global::Android.Runtime.JValue(((global::Java.Lang.Object)").Append(EscapeIdent(composer.Name)).AppendLine(").Handle);");
            idx++;
        }

        // $changed slots.
        int changedCount = ChangedSlotCount(sigParams, hasDefaultSlot, hasComposerSlot);
        for (int c = 0; c < changedCount; c++, idx++)
            sb.Append("                args[").Append(idx).AppendLine("] = new global::Android.Runtime.JValue(0);");

        // $default (only when present in the bytecode signature).
        if (hasDefaultSlot)
        {
            sb.Append("                args[").Append(idx).AppendLine("] = new global::Android.Runtime.JValue(defaults);");
            idx++;
        }

        // Synthetic-overload marker for non-@Composable extension functions
        // with $default. Kotlin always passes null here.
        if (extensionWithDefault)
        {
            sb.Append("                args[").Append(idx).AppendLine("] = new global::Android.Runtime.JValue(global::System.IntPtr.Zero);");
            idx++;
        }

        // Call.
        var callTarget = instanceField is null ? $"s_{sym}_class" : $"s_{sym}_instance";
        bool returnsValue = !method.ReturnsVoid;
        if (isConstructor)
        {
            // Constructor shape: NewObject + wrap handle into the C#
            // return type. Java.Lang.Object.GetObject<T>(handle,
            // TransferLocalRef) consumes the local ref so we don't need
            // to DeleteLocalRef it here.
            var returnTypeFq = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                    | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
            sb.Append("                var __handle = global::Android.Runtime.JNIEnv.NewObject(s_")
              .Append(sym).Append("_class, s_").Append(sym).AppendLine("_method, args);");
            sb.Append("                return global::Java.Lang.Object.GetObject<")
              .Append(returnTypeFq).AppendLine(">(__handle, global::Android.Runtime.JniHandleOwnership.TransferLocalRef)!;");
        }
        else
        {
            string callMethod;
            if (returnsValue)
                callMethod = instanceField is null ? "CallStaticObjectMethod" : "CallObjectMethod";
            else
                callMethod = instanceField is null ? "CallStaticVoidMethod" : "CallVoidMethod";
            sb.Append("                ");
            if (returnsValue) sb.Append("return ");
            sb.Append("global::Android.Runtime.JNIEnv.").Append(callMethod).Append('(').Append(callTarget)
              .Append(", s_").Append(sym).AppendLine("_method, args);");
        }
        sb.AppendLine("            }");
        sb.AppendLine("        }");

        // finally: DeleteLocalRef strings, KeepAlive managed handles.
        sb.AppendLine("        finally");
        sb.AppendLine("        {");
        foreach (var sp in stringParams)
            sb.Append("            global::Android.Runtime.JNIEnv.DeleteLocalRef(__ref_").Append(sp.Name).AppendLine(");");
        foreach (var p in userParams)
        {
            if (NeedsKeepAlive(p))
                sb.Append("            global::System.GC.KeepAlive(").Append(EscapeIdent(p.Name)).AppendLine(");");
        }
        if (composer is not null)
            sb.Append("            global::System.GC.KeepAlive(").Append(EscapeIdent(composer.Name)).AppendLine(");");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static void EmitMethodSignature(StringBuilder sb, IMethodSymbol method)
    {
        sb.Append("    ");
        if (method.DeclaredAccessibility == Accessibility.Public) sb.Append("public ");
        else if (method.DeclaredAccessibility == Accessibility.Internal) sb.Append("internal ");
        if (method.IsStatic) sb.Append("static ");
        sb.Append("partial ");
        sb.Append(method.ReturnType.ToDisplayString()).Append(' ').Append(method.Name).Append('(');
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            var p = method.Parameters[i];
            if (i > 0) sb.Append(", ");
            sb.Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                    | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                    | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)));
            sb.Append(' ').Append(EscapeIdent(p.Name));
        }
        sb.AppendLine(")");
    }

    static void EmitUserArgValue(StringBuilder sb, IParameterSymbol p, JniType sigType)
    {
        var name = EscapeIdent(p.Name);
        if (p.Type.SpecialType == SpecialType.System_String)
        {
            sb.Append("__ref_").Append(p.Name);
            return;
        }
        if (p.Type.SpecialType == SpecialType.System_IntPtr)
        {
            sb.Append(name);
            return;
        }
        if (IsNullableIntPtr(p.Type))
        {
            // `IntPtr? x` → null means "let the Kotlin default kick in";
            // pass IntPtr.Zero in that slot. The auto-mask logic still
            // clears the corresponding $default bit only when the user
            // supplied a value.
            sb.Append('(').Append(p.Name).Append(" ?? global::System.IntPtr.Zero)");
            return;
        }
        if (ComposeValueTypes.TryGet(p.Type, out _, out var info))
        {
            // Recognized Compose @JvmInline value class. The lowering
            // template takes the Nullable<T> wrapper directly and
            // returns the JNI primitive — null cases yield the Kotlin
            // default zero (the auto-mask leaves the bit set).
            sb.Append(string.Format(info.LowerTemplate, name));
            return;
        }
        if (p.Type.SpecialType is SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double)
        {
            sb.Append(name);
            return;
        }
        if (IsModifierType(p.Type))
        {
            sb.Append("global::ComposeNet.ComposeBridges.ModifierHandle(").Append(name).Append(')');
            return;
        }
        // Reference type → handle, with null check for nullable annotations.
        bool nullable = p.NullableAnnotation == NullableAnnotation.Annotated;
        if (nullable)
        {
            sb.Append(name).Append(" is null ? global::System.IntPtr.Zero : ((global::Java.Lang.Object)")
              .Append(name).Append(").Handle");
        }
        else
        {
            sb.Append("((global::Java.Lang.Object)").Append(name).Append(").Handle");
        }
    }

    static bool NeedsKeepAlive(IParameterSymbol p)
    {
        if (p.Type.SpecialType is SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_IntPtr
            or SpecialType.System_String)
            return false;
        if (IsNullableIntPtr(p.Type)) return false;
        // Recognized Compose value types (Color/Dp/Sp/Em/TextAlign) lower
        // to JNI primitives — no managed handle to keep alive.
        if (ComposeValueTypes.TryGet(p.Type, out _, out _)) return false;
        return true;
    }

    static bool IsNullableIntPtr(ITypeSymbol t) =>
        t is INamedTypeSymbol n &&
        n.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
        n.TypeArguments.Length == 1 &&
        n.TypeArguments[0].SpecialType == SpecialType.System_IntPtr;

    static bool IsModifierType(ITypeSymbol t)
    {
        if (t is INamedTypeSymbol n)
        {
            if (n.Name == "IModifier" && n.ContainingNamespace?.ToDisplayString() == "ComposeNet")
                return true;
        }
        return false;
    }

    static string PascalCase(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (char.IsUpper(raw[0])) return raw;
        return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
    }

    static string SafeSym(string s) => s.Replace('-', '_');

    // Wrap C# reserved keywords in `@` so a Kotlin parameter literally named
    // `checked`/`event`/etc. can be declared as a C# parameter and referenced
    // inside the generated body.
    static string EscapeIdent(string name) =>
        Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name)
            == Microsoft.CodeAnalysis.CSharp.SyntaxKind.None
        ? name : "@" + name;

    static string? ReadString(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
        {
            if (na.Key == name && na.Value.Value is string s) return s;
        }
        return null;
    }

    static INamedTypeSymbol? ReadType(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
        {
            if (na.Key == name && na.Value.Value is INamedTypeSymbol t) return t;
        }
        return null;
    }
}
