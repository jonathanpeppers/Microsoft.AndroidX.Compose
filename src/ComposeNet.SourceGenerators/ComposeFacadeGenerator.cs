using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ComposeNet.SourceGenerators;

/// <summary>
/// Emits user-facing facade classes from <c>[ComposeFacade]</c>-decorated
/// bridge methods on <c>ComposeNet.ComposeBridges</c>. The bridge owns
/// the JNI plumbing; the facade is a thin
/// <see cref="ComposableNode"/> / <see cref="ComposableContainer"/>
/// wrapper that builds the bridge's user-controlled args (Action →
/// <c>ComposableLambda0</c>, modifier → <c>BuildModifier()</c>, content
/// → <c>ComposableLambdas.Wrap2/3</c>) and forwards through.
///
/// Phases supported (see issue #78):
/// <list type="bullet">
/// <item>Phase 1 — modifier, IFunction0 onClick, IFunction2/3 content,
/// primitives, scope publication.</item>
/// <item>Phase 2 — IFunction1 + <c>[Callback(typeof(T))]</c> → typed
/// <c>Action&lt;T&gt;</c> ctor with JNI un-boxing.</item>
/// <item>Phase 3 — multiple <c>IFunction2?</c> / <c>IFunction3?</c> slots
/// surfaced as named <c>ComposableNode?</c> properties; required Fn2/3
/// slots become required <c>ComposableNode</c> properties with a
/// throwing null-check. Auto-mask emission against the bridge's
/// <c>$default</c> enum.</item>
/// <item>Phase 6 — <c>[ComposeFacade(DefaultColorFromTheme="...")]</c>
/// adds a <c>long ContainerColor</c> property with a
/// <c>MaterialTheme.colorScheme</c> fallback.</item>
/// <item>Phase 7 — <c>[PainterResource]</c> on an <c>IntPtr</c> param
/// (the painter handle the bridge forwards) emits a synthetic
/// <c>int drawableResourceId</c> ctor arg + <c>painterResource()</c>
/// + <c>try</c>/<c>finally</c> + <c>DeleteLocalRef</c> dance.</item>
/// </list>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ComposeFacadeGenerator : IIncrementalGenerator
{
    const string FacadeAttributeMetadataName = "ComposeNet.ComposeFacadeAttribute";
    const string BridgeAttributeMetadataName = "ComposeNet.ComposeBridgeAttribute";
    const string DeclarativeDefaultsAttributeMetadataName = "ComposeNet.ComposeDefaultsAttribute";
    const string SlotAttributeMetadataName = "ComposeNet.SlotAttribute";
    const string CallbackAttributeMetadataName = "ComposeNet.CallbackAttribute";
    const string PainterResourceAttributeMetadataName = "ComposeNet.PainterResourceAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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
            var facadeAttr = compilation.GetTypeByMetadataName(FacadeAttributeMetadataName);
            var bridgeAttr = compilation.GetTypeByMetadataName(BridgeAttributeMetadataName);
            var declarativeAttr = compilation.GetTypeByMetadataName(DeclarativeDefaultsAttributeMetadataName);
            var slotAttr = compilation.GetTypeByMetadataName(SlotAttributeMetadataName);
            var callbackAttr = compilation.GetTypeByMetadataName(CallbackAttributeMetadataName);
            var painterAttr = compilation.GetTypeByMetadataName(PainterResourceAttributeMetadataName);
            if (facadeAttr is null) return;

            var method = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node)!;
            var attr = method.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, facadeAttr));
            if (attr is null) return;

            var ctxObj = new Context(method, attr, bridgeAttr, declarativeAttr, slotAttr, callbackAttr, painterAttr, compilation);
            var result = Build(ctxObj);
            foreach (var diag in result.Diagnostics)
                spc.ReportDiagnostic(diag);
            if (result.Source is { } source && result.HintName is { } hint)
                spc.AddSource(hint, SourceText.From(source, Encoding.UTF8));
        });
    }

    sealed class Context
    {
        public IMethodSymbol Method { get; }
        public AttributeData Attr { get; }
        public INamedTypeSymbol? BridgeAttr { get; }
        public INamedTypeSymbol? DeclarativeAttr { get; }
        public INamedTypeSymbol? SlotAttr { get; }
        public INamedTypeSymbol? CallbackAttr { get; }
        public INamedTypeSymbol? PainterAttr { get; }
        public Compilation Compilation { get; }

        public Context(IMethodSymbol method, AttributeData attr, INamedTypeSymbol? bridgeAttr,
            INamedTypeSymbol? declarativeAttr, INamedTypeSymbol? slotAttr, INamedTypeSymbol? callbackAttr,
            INamedTypeSymbol? painterAttr, Compilation compilation)
        {
            Method = method;
            Attr = attr;
            BridgeAttr = bridgeAttr;
            DeclarativeAttr = declarativeAttr;
            SlotAttr = slotAttr;
            CallbackAttr = callbackAttr;
            PainterAttr = painterAttr;
            Compilation = compilation;
        }
    }

    static GenerationResult Build(Context c)
    {
        var method = c.Method;
        var attr = c.Attr;
        var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;

        // CN3001 — facade must attach to ComposeBridges.
        var container = method.ContainingType;
        if (container.Name != "ComposeBridges" ||
            container.ContainingNamespace?.ToDisplayString() != "ComposeNet")
        {
            return Fail(Diagnostics.FacadeWrongContainingType, loc, method.Name, container.ToDisplayString());
        }

        // CN3004 — paired with [ComposeBridge]. Relaxed for "wrapper"
        // facades: a partial method with a hand-written body and no
        // [ComposeBridge] is a tiny pass-through to a bound binding
        // (e.g. BoxKt.Box) — its $default enum is read from
        // [ComposeFacade].Defaults instead.
        AttributeData? bridge = null;
        if (c.BridgeAttr is not null)
        {
            bridge = method.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.BridgeAttr));
            if (bridge is null && !HasMethodBody(method))
            {
                return Fail(Diagnostics.FacadeMissingBridge, loc, method.Name);
            }
        }

        string className = ReadString(attr, "ClassName") ?? method.Name;
        string? scope = ReadString(attr, "Scope");
        string? themeColor = ReadString(attr, "DefaultColorFromTheme");
        string? colorParameter = ReadString(attr, "ColorParameter");

        // Composer is the trailing param for @Composable bridges (the only
        // shape facade generation supports).
        var csParams = method.Parameters;
        if (csParams.Length == 0 || !ComposeDefaultsGenerator.IsComposer(csParams[csParams.Length - 1].Type))
        {
            return Fail(Diagnostics.FacadeUnsupportedParameter, loc, method.Name, "<composer>",
                "bridge must be @Composable (trailing IComposer parameter) for facade generation");
        }
        var composerParam = csParams[csParams.Length - 1];
        var userParams = csParams.Take(csParams.Length - 1).ToArray();

        // Look up the bridge's Defaults enum so Phase 3 can emit the
        // matching auto-mask code. Bridge-mode reads it from
        // [ComposeBridge].Defaults; wrapper-mode (no bridge) falls
        // back to [ComposeFacade].Defaults.
        DefaultsInfo? defaults = null;
        INamedTypeSymbol? defaultsType = bridge is not null ? ReadType(bridge, "Defaults") : null;
        defaultsType ??= ReadType(attr, "Defaults");
        if (defaultsType is not null)
        {
            // Try declarative form first; fall back to walking the enum
            // directly (covers generic-form-generated enums).
            if (c.DeclarativeAttr is not null)
            {
                defaults = DefaultsInfo.TryRead(c.Compilation, c.DeclarativeAttr, defaultsType.Name);
            }
            defaults ??= DefaultsInfo.TryReadFromEnum(defaultsType);
        }

        // Look up a sibling "defaults: int" param the caller manages. If
        // present, the bridge does NOT auto-mask — the facade owns every
        // bit. (We still support Phase 1 bridges that don't have this
        // param: those rely on bridge-side auto-mask logic.)
        bool callerProvidesDefaults = false;
        IParameterSymbol? defaultsParam = null;
        if (userParams.Length > 0)
        {
            var last = userParams[userParams.Length - 1];
            if (last.Type.SpecialType == SpecialType.System_Int32 && last.Name == "defaults")
            {
                callerProvidesDefaults = true;
                defaultsParam = last;
                userParams = userParams.Take(userParams.Length - 1).ToArray();
            }
        }

        // Classify each user param.
        var slots = new List<FacadeSlot>(userParams.Length);
        var diags = new List<Diagnostic>();
        int fnContentCount = 0;

        foreach (var p in userParams)
        {
            var slot = Classify(p, c, method.Name, loc, diags);
            if (slot is null) continue;
            slots.Add(slot.Value);
            if (slot.Value.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3)
                fnContentCount++;
        }

        // Multi-slot detection: if any Fn2/3 slot is nullable, or any has
        // an explicit [Slot], OR there's >1 Fn2/3 slot, treat the facade
        // as a multi-slot LEAF (named properties). Otherwise stick with
        // the Phase 1 "container with children" shape.
        bool hasMultiSlot = slots.Any(s => s.IsNullableSlot || s.HasSlotAttribute) || fnContentCount > 1;
        if (hasMultiSlot)
        {
            // Re-classify the Fn2/Fn3 slots into property slots (the
            // generator picks one shape per bridge; mixing is invalid).
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.Kind is FacadeSlotKind.Content2)
                    slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                        ? FacadeSlotKind.NamedFunction2 : FacadeSlotKind.RequiredFunction2);
                else if (s.Kind is FacadeSlotKind.Content3)
                    slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                        ? FacadeSlotKind.NamedFunction3 : FacadeSlotKind.RequiredFunction3);
            }
        }

        // Scope only makes sense with a Phase 1 Content3 (container shape).
        if (!string.IsNullOrEmpty(scope))
        {
            if (hasMultiSlot || !slots.Any(s => s.Kind == FacadeSlotKind.Content3))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeScopeMisuse, loc, method.Name, scope ?? "?"));
            }
            else if (!IsKnownScopeKind(c.Compilation, scope!))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeScopeMisuse, loc, method.Name, scope!));
            }
        }

        // Phase 6 — bind DefaultColorFromTheme to a `long` user param.
        FacadeSlot? colorSlot = null;
        if (!string.IsNullOrEmpty(themeColor))
        {
            var longSlots = slots
                .Select((s, i) => (s, i))
                .Where(t => t.s.Param.Type.SpecialType == SpecialType.System_Int64)
                .ToArray();
            FacadeSlot? picked = null;
            if (!string.IsNullOrEmpty(colorParameter))
            {
                var match = longSlots.FirstOrDefault(t => t.s.Param.Name == colorParameter);
                if (match.s.Param is null)
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeColorThemeBindingFailed, loc, method.Name,
                        $"ColorParameter='{colorParameter}' does not match any 'long' user parameter"));
                }
                else
                {
                    picked = match.s;
                    slots[match.i] = match.s.WithKind(FacadeSlotKind.ThemeColor);
                }
            }
            else if (longSlots.Length == 1)
            {
                picked = longSlots[0].s;
                slots[longSlots[0].i] = longSlots[0].s.WithKind(FacadeSlotKind.ThemeColor);
            }
            else if (longSlots.Length == 0)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeColorThemeBindingFailed, loc, method.Name,
                    "no 'long' user parameter to bind ContainerColor to"));
            }
            else
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeColorThemeBindingFailed, loc, method.Name,
                    $"multiple 'long' user parameters — set ColorParameter to one of: {string.Join(", ", longSlots.Select(t => t.s.Param.Name))}"));
            }
            colorSlot = picked;
        }

        // Phase 7 — at most one [PainterResource] parameter per bridge.
        int painterCount = 0;
        foreach (var s in slots)
            if (s.Kind == FacadeSlotKind.PainterResource) painterCount++;
        if (painterCount > 1)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeSlotConflict, loc, method.Name,
                $"[PainterResource] may only be applied to one bridge parameter, found {painterCount}"));
        }

        // Phase 3 sanity — if the bridge takes a caller-controlled
        // `int defaults`, we must know its enum (`Defaults = typeof(...)`
        // + matching `[assembly: ComposeDefaults(...)]`) or the emitted
        // code will reference an undeclared `__defaults` local.
        if (callerProvidesDefaults && defaults is null)
        {
            string reason = defaultsType is null
                ? "the bridge has an 'int defaults' parameter but no 'Defaults = typeof(...)' on [ComposeBridge]"
                : $"no '[assembly: ComposeDefaults(\"{defaultsType.Name}\", ...)]' declaration was found for the bridge's Defaults enum";
            diags.Add(Diagnostic.Create(Diagnostics.FacadeSlotConflict, loc, method.Name, reason));
        }

        if (diags.Count > 0)
            return new GenerationResult(null, null, diags);

        var source = Emit(className, method.Name, scope, composerParam, slots, hasMultiSlot,
            callerProvidesDefaults, defaultsParam, defaults, defaultsType?.Name, themeColor, colorSlot);
        var hint = $"ComposeNet.Facade.{className}.g.cs";
        return new GenerationResult(source, hint, Array.Empty<Diagnostic>());
    }

    static FacadeSlot? Classify(IParameterSymbol p, Context c, string methodName, Location loc, List<Diagnostic> diags)
    {
        // [PainterResource] — annotates the IntPtr bridge param that
        // takes the resolved Painter handle. The facade exposes a
        // synthetic `int drawableResourceId` ctor arg in its place.
        if (c.PainterAttr is not null &&
            p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.PainterAttr)))
        {
            if (p.Type.SpecialType != SpecialType.System_IntPtr)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadePainterMisuse, loc, methodName,
                    $"[PainterResource] must annotate an 'IntPtr' parameter; '{p.Name}' is '{p.Type.ToDisplayString()}'"));
                return null;
            }
            return new FacadeSlot(p, FacadeSlotKind.PainterResource);
        }

        // [Callback(typeof(T))]
        if (c.CallbackAttr is not null)
        {
            var cba = p.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.CallbackAttr));
            if (cba is not null)
            {
                if (KotlinFunctionArity(p.Type) != 1)
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeSlotConflict, loc, methodName,
                        $"[Callback] on parameter '{p.Name}' requires a Kotlin.Jvm.Functions.IFunction1 parameter type; was '{p.Type.ToDisplayString()}'"));
                    return null;
                }
                var typeArg = cba.ConstructorArguments.Length > 0 ? cba.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                if (typeArg is null)
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeCallbackUnsupportedType, loc, methodName, p.Name, "<missing>"));
                    return null;
                }
                if (typeArg.SpecialType is not (SpecialType.System_Boolean or SpecialType.System_String or SpecialType.System_Single))
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeCallbackUnsupportedType, loc, methodName, p.Name,
                        typeArg.ToDisplayString()));
                    return null;
                }
                return new FacadeSlot(p, FacadeSlotKind.Callback, callbackType: typeArg);
            }
        }

        // IModifier?
        if (IsModifier(p.Type))
            return new FacadeSlot(p, FacadeSlotKind.Modifier);

        // Kotlin extension receiver — IntPtr with a "Scope"-suffixed name.
        // Auto-bound to RenderContext.CurrentScope; not a ctor slot, not
        // tracked in the $default bitmask.
        if (p.Type.SpecialType == SpecialType.System_IntPtr &&
            p.Name.EndsWith("Scope", StringComparison.Ordinal))
            return new FacadeSlot(p, FacadeSlotKind.ScopeReceiver);

        var funcArity = KotlinFunctionArity(p.Type);
        if (funcArity == 0)
            return new FacadeSlot(p, FacadeSlotKind.OnClick);

        if (funcArity == 2 || funcArity == 3)
        {
            string? slotName = ReadSlotAttribute(p, c.SlotAttr);
            return new FacadeSlot(p, funcArity == 2 ? FacadeSlotKind.Content2 : FacadeSlotKind.Content3,
                slotPropertyName: slotName);
        }

        if (funcArity > 0)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc, methodName, p.Name,
                $"IFunction{funcArity} (mark with [Callback(typeof(T))] for a typed Action<T> ctor slot)"));
            return null;
        }

        if (IsPrimitiveCtorType(p.Type))
            return new FacadeSlot(p, FacadeSlotKind.Primitive);

        diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc, methodName, p.Name, p.Type.ToDisplayString()));
        return null;
    }

    static string Emit(string className, string bridgeMethodName, string? scope,
        IParameterSymbol composerParam, IReadOnlyList<FacadeSlot> slots,
        bool isMultiSlot, bool callerProvidesDefaults, IParameterSymbol? defaultsParam,
        DefaultsInfo? defaults, string? defaultsEnumName,
        string? themeColor, FacadeSlot? colorSlot)
    {
        bool isContainer = !isMultiSlot && slots.Any(s => s.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3);
        string baseClass = isContainer ? "global::ComposeNet.ComposableContainer" : "global::ComposeNet.ComposableNode";

        // Ctor slots: every non-modifier, non-named-property slot.
        var ctorSlots = slots.Where(s => IsCtorSlot(s)).ToArray();
        // Named-property slots (Phase 3).
        var namedSlots = slots.Where(s => s.Kind is FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3
            or FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by ComposeNet.SourceGenerators.ComposeFacadeGenerator.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ComposeNet");
        sb.AppendLine("{");

        sb.Append("    public sealed partial class ").Append(className).Append(" : ").AppendLine(baseClass);
        sb.AppendLine("    {");

        // Backing fields for ctor slots.
        foreach (var s in ctorSlots)
        {
            var typeRef = CtorFieldType(s);
            sb.Append("        readonly ").Append(typeRef).Append(" _").Append(CtorIdentifier(s)).AppendLine(";");
        }

        // Phase 6 — ContainerColor property + (no theme fallback here; happens in Render).
        if (themeColor is not null && colorSlot is not null)
        {
            sb.AppendLine("        /// <summary>Optional packed Compose <c>Color</c> (long). Leave <c>0L</c> to inherit the active <c>MaterialTheme.colorScheme</c> fallback.</summary>");
            sb.AppendLine("        public long ContainerColor { get; set; }");
        }

        // Phase 3 — named properties.
        foreach (var s in namedSlots)
        {
            sb.Append("        public global::ComposeNet.ComposableNode? ").Append(PropertyName(s)).AppendLine(" { get; set; }");
        }

        // Constructor.
        if (ctorSlots.Length > 0)
        {
            sb.Append("        public ").Append(className).Append('(');
            for (int i = 0; i < ctorSlots.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(CtorParamType(ctorSlots[i])).Append(' ').Append(EscapeIdent(CtorIdentifier(ctorSlots[i])));
            }
            sb.AppendLine(")");
            sb.AppendLine("        {");
            foreach (var s in ctorSlots)
                sb.Append("            _").Append(CtorIdentifier(s)).Append(" = ").Append(EscapeIdent(CtorIdentifier(s))).AppendLine(";");
            sb.AppendLine("        }");
        }

        // Render
        var composerName = EscapeIdent(composerParam.Name);
        sb.Append("        internal override void Render(global::AndroidX.Compose.Runtime.IComposer ")
          .Append(composerName).AppendLine(")");
        sb.AppendLine("        {");

        // Required-named-slot null checks.
        foreach (var s in namedSlots.Where(s => s.Kind is FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3))
        {
            var name = PropertyName(s);
            sb.Append("            if (").Append(name).AppendLine(" is null)");
            sb.Append("                throw new global::System.InvalidOperationException(\"")
              .Append(className).Append('.').Append(name).AppendLine(" is required (the Kotlin parameter has no default).\");");
        }

        // OnClick wrappers — one per line so slot-table keys stay distinct.
        foreach (var s in slots.Where(s => s.Kind == FacadeSlotKind.OnClick))
        {
            sb.Append("            var __").Append(s.Param.Name)
              .Append(" = new global::ComposeNet.ComposableLambda0(_").Append(s.Param.Name).AppendLine(");");
        }

        // Callback wrappers (Phase 2).
        foreach (var s in slots.Where(s => s.Kind == FacadeSlotKind.Callback))
        {
            EmitCallbackWrapper(sb, s);
        }

        // Modifier evaluation. Only hoist to a local when needed for
        // the auto-mask (callerProvidesDefaults). Otherwise the bridge
        // call uses BuildModifier() inline to keep Phase 1 output stable.
        var modifierSlot = slots.FirstOrDefault(s => s.Kind == FacadeSlotKind.Modifier);
        bool hoistModifier = modifierSlot.Param is not null && callerProvidesDefaults;
        if (hoistModifier)
        {
            sb.AppendLine("            var __modifier = BuildModifier();");
        }

        // Named slot wrappers (Phase 3). One per line.
        foreach (var s in namedSlots)
        {
            var name = PropertyName(s);
            string wrap = s.Kind is FacadeSlotKind.NamedFunction3 or FacadeSlotKind.RequiredFunction3
                ? "Wrap3"
                : "Wrap2";
            bool nullable = s.Kind is FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3;
            if (nullable)
            {
                sb.Append("            var __").Append(s.Param.Name).Append(" = ").Append(name)
                  .Append(" is null ? null : global::ComposeNet.ComposableLambdas.").Append(wrap)
                  .Append('(').Append(composerName).Append(", c => ").Append(name).AppendLine(".Render(c));");
            }
            else
            {
                sb.Append("            var __").Append(s.Param.Name).Append(" = global::ComposeNet.ComposableLambdas.")
                  .Append(wrap).Append('(').Append(composerName).Append(", c => ").Append(name).AppendLine("!.Render(c));");
            }
        }

        // Content wrapper (Phase 1 only — multi-slot already handled).
        if (isContainer)
        {
            var contentSlot = slots.First(s => s.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3);
            int arity = contentSlot.Kind == FacadeSlotKind.Content3 ? 3 : 2;
            sb.Append("            var __").Append(contentSlot.Param.Name).Append(" = global::ComposeNet.ComposableLambdas.");
            if (arity == 2)
            {
                sb.Append("Wrap2(").Append(composerName).AppendLine(", c => RenderChildren(c));");
            }
            else if (!string.IsNullOrEmpty(scope))
            {
                sb.Append("Wrap3(").Append(composerName).AppendLine(", (__scope, c) =>");
                sb.AppendLine("            {");
                sb.Append("                using var __scopeFrame = global::ComposeNet.RenderContext.PushScope(__scope, global::ComposeNet.ScopeKind.")
                  .Append(scope).AppendLine(");");
                sb.AppendLine("                RenderChildren(c);");
                sb.AppendLine("            });");
            }
            else
            {
                sb.Append("Wrap3(").Append(composerName).AppendLine(", c => RenderChildren(c));");
            }
        }

        // Phase 6 — theme color resolution.
        if (themeColor is not null && colorSlot is not null)
        {
            sb.Append("            long __color = ContainerColor != 0L ? ContainerColor : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(")
              .Append(composerName).Append(", 0).").Append(Pascal(themeColor)).AppendLine(";");
        }

        // Phase 7 — PainterResource preamble.
        var painterIdSlot = slots.FirstOrDefault(s => s.Kind == FacadeSlotKind.PainterResource);
        bool hasPainter = painterIdSlot.Param is not null;
        if (hasPainter)
        {
            sb.AppendLine("            global::System.IntPtr __painterRef = global::ComposeNet.ComposeBridges.PainterResource(_drawableResourceId, "
                + composerName + ");");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
        }

        string indent = hasPainter ? "                " : "            ";

        // Phase 3 — auto-mask defaults.
        if (callerProvidesDefaults && defaults is { } d)
        {
            EmitDefaultsMask(sb, indent, d, slots, namedSlots, modifierSlot.Param is not null, hasPainter);
        }

        // Bridge call. Preserve original bridge param order.
        sb.Append(indent).Append("global::ComposeNet.ComposeBridges.").Append(bridgeMethodName).Append('(');
        bool first = true;
        // Walk method.Parameters via slots in their original order; the
        // defaults param (if any) was removed from `slots` so we add it
        // back here from the local __defaults symbol.
        foreach (var s in slots)
        {
            if (!first) sb.Append(", ");
            first = false;
            sb.Append(BridgeArgExpr(s, hoistModifier));
        }
        if (callerProvidesDefaults)
        {
            if (!first) sb.Append(", ");
            sb.Append("__defaults");
            first = false;
        }
        if (!first) sb.Append(", ");
        sb.Append(composerName).AppendLine(");");

        if (hasPainter)
        {
            sb.AppendLine("            }");
            sb.AppendLine("            finally");
            sb.AppendLine("            {");
            sb.AppendLine("                global::Android.Runtime.JNIEnv.DeleteLocalRef(__painterRef);");
            sb.AppendLine("            }");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static void EmitCallbackWrapper(StringBuilder sb, FacadeSlot s)
    {
        var t = s.CallbackType!;
        string expr = t.SpecialType switch
        {
            SpecialType.System_Boolean => "v is global::Java.Lang.Boolean __b && __b.BooleanValue()",
            SpecialType.System_Single  => "v is global::Java.Lang.Float __f ? __f.FloatValue() : 0f",
            SpecialType.System_String  => "v?.ToString() ?? string.Empty",
            _ => "default!",
        };
        sb.Append("            var __").Append(s.Param.Name)
          .Append(" = new global::ComposeNet.ComposableLambda1(v => _")
          .Append(s.Param.Name).Append('(').Append(expr).AppendLine("));");
    }

    static void EmitDefaultsMask(StringBuilder sb, string indent, DefaultsInfo d,
        IReadOnlyList<FacadeSlot> slots, IReadOnlyList<FacadeSlot> namedSlots,
        bool hasModifier, bool hasPainter)
    {
        sb.Append(indent).Append("int __defaults = (int)global::ComposeNet.").Append(d.EnumName).AppendLine(".All;");

        // For each slot the facade DEFINITELY supplies, clear its bit.
        // - Modifier: clear when __modifier != null.
        // - Primitive / Callback / OnClick: bit is `!`-suppressed by
        //   convention (caller-owned); skip silently when absent.
        // - Named optional slot: clear when property non-null.
        // - Required slot (Function2/3 / PainterHandle): always cleared.

        foreach (var s in slots)
        {
            string? bitMember = MatchEnumMember(d, s);
            if (bitMember is null) continue;
            switch (s.Kind)
            {
                case FacadeSlotKind.Modifier:
                    sb.Append(indent).Append("if (__modifier is not null) __defaults &= ~(int)global::ComposeNet.")
                      .Append(d.EnumName).Append('.').Append(bitMember).AppendLine(";");
                    break;
                case FacadeSlotKind.NamedFunction2:
                case FacadeSlotKind.NamedFunction3:
                    sb.Append(indent).Append("if (__").Append(s.Param.Name).Append(" is not null) __defaults &= ~(int)global::ComposeNet.")
                      .Append(d.EnumName).Append('.').Append(bitMember).AppendLine(";");
                    break;
                case FacadeSlotKind.RequiredFunction2:
                case FacadeSlotKind.RequiredFunction3:
                case FacadeSlotKind.PainterResource:
                case FacadeSlotKind.Primitive:
                case FacadeSlotKind.ThemeColor:
                    sb.Append(indent).Append("__defaults &= ~(int)global::ComposeNet.")
                      .Append(d.EnumName).Append('.').Append(bitMember).AppendLine(";");
                    break;
            }
        }
    }

    static string? MatchEnumMember(DefaultsInfo d, FacadeSlot s)
    {
        // For named slots: prefer the C# property name (e.g. user
        // overrode via [Slot]). For everything else: the bridge param
        // name maps to the Kotlin name 1:1.
        string lookupName = s.Kind switch
        {
            FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3
                or FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3
                => PropertyName(s),
            _ => s.Param.Name,
        };
        // First try Kotlin name (== bridge param name). Then fall back
        // to property-name match (case-insensitive).
        var bk = d.FindByKotlinName(s.Param.Name);
        if (bk is { } bks && bks.EnumMember is { } em1) return em1;
        var bp = d.FindByEnumMember(lookupName);
        if (bp is { } bps && bps.EnumMember is { } em2) return em2;
        return null;
    }

    static string BridgeArgExpr(FacadeSlot s, bool hoistModifier) =>
        s.Kind switch
        {
            FacadeSlotKind.Modifier         => hoistModifier ? "__modifier" : "BuildModifier()",
            FacadeSlotKind.OnClick          => "__" + s.Param.Name,
            FacadeSlotKind.Content2         => "__" + s.Param.Name,
            FacadeSlotKind.Content3         => "__" + s.Param.Name,
            FacadeSlotKind.NamedFunction2   => "__" + s.Param.Name,
            FacadeSlotKind.NamedFunction3   => "__" + s.Param.Name,
            FacadeSlotKind.RequiredFunction2 => "__" + s.Param.Name,
            FacadeSlotKind.RequiredFunction3 => "__" + s.Param.Name,
            FacadeSlotKind.Callback         => "__" + s.Param.Name,
            FacadeSlotKind.Primitive        => "_" + s.Param.Name,
            FacadeSlotKind.PainterResource  => "__painterRef",
            FacadeSlotKind.ThemeColor       => "__color",
            FacadeSlotKind.ScopeReceiver    => "global::ComposeNet.RenderContext.CurrentScope",
            _ => "default",
        };

    static bool IsCtorSlot(FacadeSlot s) =>
        s.Kind is FacadeSlotKind.OnClick or FacadeSlotKind.Primitive or FacadeSlotKind.Callback or FacadeSlotKind.PainterResource;

    static string CtorFieldType(FacadeSlot slot) => CtorParamType(slot);

    static string CtorIdentifier(FacadeSlot slot) =>
        slot.Kind == FacadeSlotKind.PainterResource ? "drawableResourceId" : slot.Param.Name;

    static string CtorParamType(FacadeSlot slot)
    {
        var format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        return slot.Kind switch
        {
            FacadeSlotKind.OnClick   => "global::System.Action",
            FacadeSlotKind.Callback  => "global::System.Action<" + slot.CallbackType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)) + ">",
            FacadeSlotKind.PainterResource => "int",
            FacadeSlotKind.Primitive => slot.Param.Type.ToDisplayString(format),
            _ => slot.Param.Type.ToDisplayString(),
        };
    }

    static string PropertyName(FacadeSlot s) => s.SlotPropertyName ?? Pascal(s.Param.Name);

    static string Pascal(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    static bool IsModifier(ITypeSymbol type) =>
        type is INamedTypeSymbol n &&
        n.Name == "IModifier" &&
        n.ContainingNamespace?.ToDisplayString() == "AndroidX.Compose.UI";

    static int KotlinFunctionArity(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol n) return -1;
        if (n.ContainingNamespace?.ToDisplayString() != "Kotlin.Jvm.Functions") return -1;
        var name = n.Name;
        if (!name.StartsWith("IFunction", StringComparison.Ordinal)) return -1;
        var tail = name.Substring("IFunction".Length);
        return int.TryParse(tail, out var arity) ? arity : -1;
    }

    static bool IsPrimitiveCtorType(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Enum ||
        type.SpecialType is SpecialType.System_String
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Boolean
            or SpecialType.System_Single
            or SpecialType.System_Double;

    static bool IsKnownScopeKind(Compilation compilation, string scope)
    {
        var enumType = compilation.GetTypeByMetadataName("ComposeNet.ScopeKind");
        if (enumType is null) return scope is "Row" or "Column";
        foreach (var m in enumType.GetMembers())
            if (m.Kind == SymbolKind.Field && m.Name == scope)
                return true;
        return false;
    }

    static GenerationResult Fail(DiagnosticDescriptor desc, Location loc, params object?[] args) =>
        new(null, null, new[] { Diagnostic.Create(desc, loc, args) });

    static string? ReadString(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
            if (na.Key == name && na.Value.Value is string s) return s;
        return null;
    }

    static INamedTypeSymbol? ReadType(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
            if (na.Key == name && na.Value.Value is INamedTypeSymbol t) return t;
        return null;
    }

    static string? ReadSlotAttribute(IParameterSymbol p, INamedTypeSymbol? slotAttr)
    {
        if (slotAttr is null) return null;
        var a = p.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, slotAttr));
        if (a is null) return null;
        if (a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s) return s;
        return null;
    }

    static string EscapeIdent(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    static bool HasMethodBody(IMethodSymbol method)
    {
        if (HasBodyInRefs(method)) return true;
        if (method.PartialImplementationPart is { } impl && HasBodyInRefs(impl)) return true;
        if (method.PartialDefinitionPart is { } defn && HasBodyInRefs(defn)) return true;
        return false;
    }

    static bool HasBodyInRefs(IMethodSymbol m)
    {
        foreach (var sr in m.DeclaringSyntaxReferences)
        {
            if (sr.GetSyntax() is MethodDeclarationSyntax mds &&
                (mds.Body is not null || mds.ExpressionBody is not null))
            {
                return true;
            }
        }
        return false;
    }

    internal enum FacadeSlotKind
    {
        Modifier,
        OnClick,
        Content2,
        Content3,
        Primitive,
        Callback,
        NamedFunction2,
        NamedFunction3,
        RequiredFunction2,
        RequiredFunction3,
        PainterResource,
        ThemeColor,
        ScopeReceiver,
    }

    internal readonly struct FacadeSlot
    {
        public FacadeSlot(IParameterSymbol param, FacadeSlotKind kind,
            ITypeSymbol? callbackType = null, string? slotPropertyName = null)
        {
            Param = param;
            Kind = kind;
            CallbackType = callbackType;
            SlotPropertyName = slotPropertyName;
        }
        public IParameterSymbol Param { get; }
        public FacadeSlotKind Kind { get; }
        public ITypeSymbol? CallbackType { get; }
        public string? SlotPropertyName { get; }
        public bool HasSlotAttribute => SlotPropertyName is not null;
        public bool IsNullableSlot => Param.NullableAnnotation == NullableAnnotation.Annotated
            && KindIsFnSlot(Kind);
        static bool KindIsFnSlot(FacadeSlotKind k) =>
            k is FacadeSlotKind.Content2 or FacadeSlotKind.Content3
              or FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3
              or FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3;
        public FacadeSlot WithKind(FacadeSlotKind newKind) =>
            new(Param, newKind, CallbackType, SlotPropertyName);
    }
}
