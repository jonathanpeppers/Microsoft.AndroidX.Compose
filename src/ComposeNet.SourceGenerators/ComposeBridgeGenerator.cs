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

        // Locate the matching declarative [ComposeDefaults] attribute, if any.
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
        else
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.BridgeMissingDefaults, loc, defaultsEnumName ?? "?", method.Name)
            });
        }

        // C# parameter walk. composer is required and must be last.
        var csParams = method.Parameters;
        if (csParams.Length == 0 || !ComposeDefaultsGenerator.IsComposer(csParams[csParams.Length - 1].Type))
        {
            return new GenerationResult(null, null, new[] {
                Diagnostic.Create(Diagnostics.MalformedAttribute, loc, method.Name)
            });
        }
        var composerParam = csParams[csParams.Length - 1];

        // Detect optional `int defaults` immediately before composer.
        bool callerProvidesDefaults = false;
        int userTail = csParams.Length - 1;
        if (userTail > 0 &&
            csParams[userTail - 1].Type.SpecialType == SpecialType.System_Int32 &&
            csParams[userTail - 1].Name == "defaults")
        {
            callerProvidesDefaults = true;
            userTail -= 1;
        }
        var userParams = csParams.Take(userTail).ToArray();

        // Detect leading IntPtr <name>Scope receiver.
        IParameterSymbol? receiverParam = null;
        if (userParams.Length > 0 &&
            userParams[0].Type.SpecialType == SpecialType.System_IntPtr &&
            userParams[0].Name.EndsWith("Scope", StringComparison.Ordinal))
        {
            receiverParam = userParams[0];
            userParams = userParams.Skip(1).ToArray();
        }

        // Match each remaining user param to a Kotlin bit position by name.
        // Names with '!' prefix in the declarative list mean "always supplied,
        // not in enum"; the matching is purely positional-by-name.
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
        if (diags.Count > 0)
            return new GenerationResult(null, null, diags.ToArray());

        var changedSlots = ChangedSlotCount(sigParams);
        var actualUserSlots = sigParams.Count - (receiverParam is null ? 0 : 1) - 1 /*composer*/ - changedSlots - 1 /*default*/;
        if (actualUserSlots != kotlinNames.Count)
        {
            // Sanity check: sig param count = receiver? + kotlin params + composer + $changed* + $default.
            // We expose the mismatch via diagnostic — common cause is wrong Defaults enum.
            diags.Add(Diagnostic.Create(Diagnostics.BridgeSignatureMismatch, loc,
                method.Name, actualUserSlots, defaultsEnumName ?? "?", kotlinNames.Count));
            return new GenerationResult(null, null, diags.ToArray());
        }

        var source = Emit(method, attr, className, jvmName, signature, defaultsEnumName!, sigParams,
            kotlinNames, userParams, userBitOf, receiverParam, callerProvidesDefaults, instanceField);
        var hint = $"ComposeNet.{method.ContainingType.Name}.{method.Name}.g.cs";
        return new GenerationResult(source, hint, Array.Empty<Diagnostic>());
    }

    /// <summary>Number of trailing <c>I</c> params *between* the composer and the final <c>$default</c>.</summary>
    static int ChangedSlotCount(IReadOnlyList<JniType> sigParams)
    {
        // Find composer position from the right: composer is the L Composer; before the trailing Is.
        // Walk back over all trailing I's; the count of them minus one is changedSlots.
        int trailingInts = 0;
        for (int i = sigParams.Count - 1; i >= 0 && sigParams[i].Code == 'I' && sigParams[i].ArrayDepth == 0; i--)
            trailingInts++;
        return Math.Max(0, trailingInts - 1);
    }

    static string Emit(
        IMethodSymbol method, AttributeData _attr,
        string className, string jvmName, string signature, string enumName,
        IReadOnlyList<JniType> sigParams,
        IReadOnlyList<string> kotlinNames,
        IParameterSymbol[] userParams,
        Dictionary<string, int> userBitOf,
        IParameterSymbol? receiverParam,
        bool callerProvidesDefaults,
        string? instanceField)
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
        if (instanceField is null)
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

        // Defaults computation (only when caller didn't pass one).
        if (!callerProvidesDefaults)
        {
            sb.Append("        int defaults = (int)global::ComposeNet.").Append(enumName).AppendLine(".All;");
            foreach (var p in userParams)
            {
                if (!userBitOf.TryGetValue(p.Name, out var bit)) continue;
                // Skip bits whose Kotlin name has '!' prefix — no enum member.
                if (kotlinNames[bit].StartsWith("!", StringComparison.Ordinal)) continue;

                var member = PascalCase(p.Name);
                if (p.NullableAnnotation == NullableAnnotation.Annotated || p.Type.IsReferenceType)
                {
                    sb.Append("        if (").Append(p.Name).Append(" is not null) defaults &= ~(int)global::ComposeNet.")
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
              .Append(" = global::Android.Runtime.JNIEnv.NewString(").Append(sp.Name).AppendLine(");");
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
            sb.Append("                args[").Append(idx).Append("] = new global::Android.Runtime.JValue(").Append(receiverParam.Name).AppendLine(");");
            idx++;
        }

        // Kotlin user params: walk bits 0..kotlinNames.Count-1.
        for (int bit = 0; bit < kotlinNames.Count; bit++, idx++)
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

        // Composer.
        var composer = method.Parameters[method.Parameters.Length - 1];
        sb.Append("                args[").Append(idx).Append("] = new global::Android.Runtime.JValue(((global::Java.Lang.Object)").Append(composer.Name).AppendLine(").Handle);");
        idx++;

        // $changed slots.
        int changedCount = ChangedSlotCount(sigParams);
        for (int c = 0; c < changedCount; c++, idx++)
            sb.Append("                args[").Append(idx).AppendLine("] = new global::Android.Runtime.JValue(0);");

        // $default.
        sb.Append("                args[").Append(idx).AppendLine("] = new global::Android.Runtime.JValue(defaults);");

        // Call.
        var callTarget = instanceField is null ? $"s_{sym}_class" : $"s_{sym}_instance";
        bool returnsValue = !method.ReturnsVoid;
        string callMethod;
        if (returnsValue)
            callMethod = instanceField is null ? "CallStaticObjectMethod" : "CallObjectMethod";
        else
            callMethod = instanceField is null ? "CallStaticVoidMethod" : "CallVoidMethod";
        sb.Append("                ");
        if (returnsValue) sb.Append("return ");
        sb.Append("global::Android.Runtime.JNIEnv.").Append(callMethod).Append('(').Append(callTarget)
          .Append(", s_").Append(sym).AppendLine("_method, args);");
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
                sb.Append("            global::System.GC.KeepAlive(").Append(p.Name).AppendLine(");");
        }
        sb.Append("            global::System.GC.KeepAlive(").Append(composer.Name).AppendLine(");");
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
            sb.Append(' ').Append(p.Name);
        }
        sb.AppendLine(")");
    }

    static void EmitUserArgValue(StringBuilder sb, IParameterSymbol p, JniType sigType)
    {
        if (p.Type.SpecialType == SpecialType.System_String)
        {
            sb.Append("__ref_").Append(p.Name);
            return;
        }
        if (p.Type.SpecialType == SpecialType.System_IntPtr)
        {
            sb.Append(p.Name);
            return;
        }
        if (p.Type.SpecialType is SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double)
        {
            sb.Append(p.Name);
            return;
        }
        if (IsModifierType(p.Type))
        {
            sb.Append("global::ComposeNet.ComposeBridges.ModifierHandle(").Append(p.Name).Append(')');
            return;
        }
        // Reference type → handle, with null check for nullable annotations.
        bool nullable = p.NullableAnnotation == NullableAnnotation.Annotated;
        if (nullable)
        {
            sb.Append(p.Name).Append(" is null ? global::System.IntPtr.Zero : ((global::Java.Lang.Object)")
              .Append(p.Name).Append(").Handle");
        }
        else
        {
            sb.Append("((global::Java.Lang.Object)").Append(p.Name).Append(").Handle");
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
        return true;
    }

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
