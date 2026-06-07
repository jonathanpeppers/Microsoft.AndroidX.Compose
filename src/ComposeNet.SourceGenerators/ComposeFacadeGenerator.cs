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
/// adds a <c>Color ContainerColor</c> property with a
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
    const string StateHolderAttributeMetadataName = "ComposeNet.StateHolderAttribute";

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
            var stateHolderAttr = compilation.GetTypeByMetadataName(StateHolderAttributeMetadataName);
            if (facadeAttr is null) return;

            var method = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol((MethodDeclarationSyntax)ctx.Node)!;
            var attr = method.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, facadeAttr));
            if (attr is null) return;

            var ctxObj = new Context(method, attr, bridgeAttr, declarativeAttr, slotAttr, callbackAttr, painterAttr, stateHolderAttr, compilation);
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
        public INamedTypeSymbol? StateHolderAttr { get; }
        public Compilation Compilation { get; }

        public Context(IMethodSymbol method, AttributeData attr, INamedTypeSymbol? bridgeAttr,
            INamedTypeSymbol? declarativeAttr, INamedTypeSymbol? slotAttr, INamedTypeSymbol? callbackAttr,
            INamedTypeSymbol? painterAttr, INamedTypeSymbol? stateHolderAttr, Compilation compilation)
        {
            Method = method;
            Attr = attr;
            BridgeAttr = bridgeAttr;
            DeclarativeAttr = declarativeAttr;
            SlotAttr = slotAttr;
            CallbackAttr = callbackAttr;
            PainterAttr = painterAttr;
            StateHolderAttr = stateHolderAttr;
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
        string? branchOn = ReadString(attr, "BranchOn");
        string? alternateBridgeName = ReadString(attr, "AlternateBridge");

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
        // Hybrid container exception: exactly 1 non-nullable Fn3 (+ no
        // [Slot]) PLUS 1+ nullable Fn2/3 slots AND `[ComposeFacade(Scope = "...")]`
        // explicitly set — the non-nullable Fn3 stays as the container
        // body (RenderChildren) and the nullable slots become named
        // properties. The Scope opt-in disambiguates from leafs that
        // happen to have a required Fn2 label slot (e.g. AssistChip).
        bool hasMultiSlot = slots.Any(s => s.IsNullableSlot || s.HasSlotAttribute) || fnContentCount > 1;
        bool isHybridContainer = false;
        if (hasMultiSlot && !string.IsNullOrEmpty(scope))
        {
            var nonNullableFn3 = slots.Where(s =>
                s.Kind == FacadeSlotKind.Content3 &&
                s.Param.NullableAnnotation != NullableAnnotation.Annotated &&
                !s.HasSlotAttribute).ToArray();
            var nullableContent = slots.Where(s =>
                s.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3 &&
                s.Param.NullableAnnotation == NullableAnnotation.Annotated).ToArray();
            isHybridContainer =
                nonNullableFn3.Length == 1 &&
                nullableContent.Length >= 1 &&
                !slots.Any(s => s.HasSlotAttribute);
        }
        if (hasMultiSlot)
        {
            // Re-classify the Fn2/Fn3 slots into property slots (the
            // generator picks one shape per bridge; mixing is invalid).
            // Hybrid container: leave the sole non-nullable Fn3 as
            // Content3 so it renders the container body.
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                bool isContainerBody = isHybridContainer
                    && s.Kind == FacadeSlotKind.Content3
                    && s.Param.NullableAnnotation != NullableAnnotation.Annotated
                    && !s.HasSlotAttribute;
                if (s.Kind is FacadeSlotKind.Content2 && !isContainerBody)
                    slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                        ? FacadeSlotKind.NamedFunction2 : FacadeSlotKind.RequiredFunction2);
                else if (s.Kind is FacadeSlotKind.Content3 && !isContainerBody)
                    slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                        ? FacadeSlotKind.NamedFunction3 : FacadeSlotKind.RequiredFunction3);
            }
        }

        // Scope only makes sense with a Content3 (container shape — Phase 1
        // pure container, or the new hybrid container with named slots).
        if (!string.IsNullOrEmpty(scope))
        {
            if ((hasMultiSlot && !isHybridContainer) || !slots.Any(s => s.Kind == FacadeSlotKind.Content3))
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

        // Branching (CN3010). When BranchOn/AlternateBridge are set, the
        // facade dispatches between this primary bridge and a sibling
        // bridge whose param list is a strict superset (one extra
        // optional slot). The extra slot becomes a nullable property on
        // the facade; the if-branch passes it, the else-branch omits it.
        BranchInfo? branchInfo = null;
        if (!string.IsNullOrEmpty(branchOn) || !string.IsNullOrEmpty(alternateBridgeName))
        {
            branchInfo = BuildBranchInfo(c, method, branchOn, alternateBridgeName, loc,
                userParams, slots, callerProvidesDefaults, isHybridContainer, diags);
            if (branchInfo is not null)
            {
                // Append the synthesised slot, force the facade into
                // multi-slot leaf shape.
                slots.Add(branchInfo.BranchedSlot);
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.Kind == FacadeSlotKind.Content2)
                        slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                            ? FacadeSlotKind.NamedFunction2 : FacadeSlotKind.RequiredFunction2);
                    else if (s.Kind == FacadeSlotKind.Content3)
                        slots[i] = s.WithKind(s.Param.NullableAnnotation == NullableAnnotation.Annotated
                            ? FacadeSlotKind.NamedFunction3 : FacadeSlotKind.RequiredFunction3);
                }
                hasMultiSlot = true;
            }
        }

        if (diags.Count > 0)
            return new GenerationResult(null, null, diags);

        var source = Emit(className, method.Name, scope, composerParam, slots, hasMultiSlot,
            callerProvidesDefaults, defaultsParam, defaults, defaultsType?.Name, themeColor, colorSlot,
            userParams, branchInfo);
        var hint = $"ComposeNet.Facade.{className}.g.cs";
        return new GenerationResult(source, hint, Array.Empty<Diagnostic>());
    }

    static BranchInfo? BuildBranchInfo(Context c, IMethodSymbol primary,
        string? branchOn, string? alternateBridgeName, Location loc,
        IReadOnlyList<IParameterSymbol> primaryUserParams,
        IReadOnlyList<FacadeSlot> primarySlots,
        bool callerProvidesDefaults, bool isHybridContainer,
        List<Diagnostic> diags)
    {
        // Both required.
        if (string.IsNullOrEmpty(branchOn) || string.IsNullOrEmpty(alternateBridgeName))
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                "BranchOn and AlternateBridge must both be set"));
            return null;
        }

        // Primary must use the caller-managed-defaults shape.
        if (!callerProvidesDefaults)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                "branching requires the primary bridge to declare a trailing 'int defaults' parameter"));
            return null;
        }

        // Container shape disallowed — branching only supports multi-slot
        // leafs (every slot exposed as a property). Hybrid containers
        // (Scope opt-in with a named-slot mix) are explicitly rejected
        // because the container body would bleed across branches in
        // surprising ways. Pure Phase 1 container shapes are silently
        // reclassified to leaf at the call site in Build().
        if (isHybridContainer)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                "branching is not supported on hybrid container shapes (Scope opt-in with named slots)"));
            return null;
        }

        // Resolve alternate method symbol on ComposeBridges.
        var bridgesType = c.Compilation.GetTypeByMetadataName("ComposeNet.ComposeBridges");
        if (bridgesType is null)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                "ComposeNet.ComposeBridges not found in compilation"));
            return null;
        }

        var allOverloads = bridgesType.GetMembers(alternateBridgeName!).OfType<IMethodSymbol>()
            .Where(m => m.IsStatic).ToArray();
        // Require @Composable shape: trailing IComposer + (optionally) a
        // [ComposeBridge] attribute. Filter to the candidate set first
        // (this matches what the StateHolder validator does for Remember).
        var candidates = allOverloads.Where(m =>
            m.Parameters.Length >= 1 &&
            ComposeDefaultsGenerator.IsComposer(m.Parameters[m.Parameters.Length - 1].Type) &&
            (c.BridgeAttr is null ||
             m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.BridgeAttr)))
        ).ToArray();

        if (candidates.Length == 0)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"no static @Composable method 'ComposeBridges.{alternateBridgeName}' with [ComposeBridge] and a trailing IComposer parameter was found"));
            return null;
        }
        if (candidates.Length > 1)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"AlternateBridge='{alternateBridgeName}' is ambiguous — {candidates.Length} matching overloads of ComposeBridges.{alternateBridgeName} found"));
            return null;
        }

        var altMethod = candidates[0];
        // Strip trailing composer + (optional) `int defaults`.
        var altAll = altMethod.Parameters;
        var altUser = altAll.Take(altAll.Length - 1).ToArray();
        IParameterSymbol? altDefaultsParam = null;
        if (altUser.Length > 0 &&
            altUser[altUser.Length - 1].Type.SpecialType == SpecialType.System_Int32 &&
            altUser[altUser.Length - 1].Name == "defaults")
        {
            altDefaultsParam = altUser[altUser.Length - 1];
            altUser = altUser.Take(altUser.Length - 1).ToArray();
        }
        if (altDefaultsParam is null)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"alternate bridge 'ComposeBridges.{alternateBridgeName}' must declare a trailing 'int defaults' parameter"));
            return null;
        }

        // Compute the diff. Alternate's user-param set must be exactly
        // primary's set plus one extra whose Pascal-cased name matches
        // BranchOn.
        var primaryNames = new HashSet<string>(primaryUserParams.Select(p => p.Name), StringComparer.Ordinal);
        var altNames = new HashSet<string>(altUser.Select(p => p.Name), StringComparer.Ordinal);
        var missing = primaryNames.Where(n => !altNames.Contains(n)).ToArray();
        if (missing.Length > 0)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"alternate bridge 'ComposeBridges.{alternateBridgeName}' is missing primary parameters: {string.Join(", ", missing)}"));
            return null;
        }
        var extras = altUser.Where(p => !primaryNames.Contains(p.Name)).ToArray();
        if (extras.Length != 1)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"alternate bridge 'ComposeBridges.{alternateBridgeName}' must add exactly one parameter vs primary; found {extras.Length} ({string.Join(", ", extras.Select(e => e.Name))})"));
            return null;
        }
        var extra = extras[0];
        var pascalExtra = Pascal(extra.Name);
        if (!string.Equals(pascalExtra, branchOn, StringComparison.Ordinal))
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"extra parameter '{extra.Name}' (Pascal '{pascalExtra}') does not match BranchOn='{branchOn}'"));
            return null;
        }

        // Shape compatibility on shared params. Compose function types
        // are compared by Kotlin arity (nullability is allowed to differ
        // because the facade hides it); everything else is compared by
        // canonical type symbol equality (nullable-aware).
        foreach (var pp in primaryUserParams)
        {
            var ap = altUser.First(a => a.Name == pp.Name);
            if (!AreCompatibleSharedParams(pp, ap))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                    $"shared parameter '{pp.Name}' has incompatible types between primary ({pp.Type.ToDisplayString()}) and alternate ({ap.Type.ToDisplayString()})"));
                return null;
            }
        }

        // Synthesise the branched slot. It must be a Kotlin function so
        // the facade can expose it as a ComposableNode? property.
        var extraArity = KotlinFunctionArity(extra.Type);
        FacadeSlotKind branchedKind;
        if (extraArity == 2) branchedKind = FacadeSlotKind.NamedFunction2;
        else if (extraArity == 3) branchedKind = FacadeSlotKind.NamedFunction3;
        else
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"extra parameter '{extra.Name}' must be Kotlin.Jvm.Functions.IFunction2 or IFunction3; was '{extra.Type.ToDisplayString()}'"));
            return null;
        }

        // Resolve the alternate's Defaults enum (declarative first, then
        // generic-form enum walk).
        AttributeData? altBridgeAttr = c.BridgeAttr is null ? null
            : altMethod.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.BridgeAttr));
        INamedTypeSymbol? altDefaultsType = altBridgeAttr is not null ? ReadType(altBridgeAttr, "Defaults") : null;
        if (altDefaultsType is null)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"alternate bridge 'ComposeBridges.{alternateBridgeName}' has no '[ComposeBridge].Defaults' enum to drive the per-branch defaults mask"));
            return null;
        }
        DefaultsInfo? altDefaults = null;
        if (c.DeclarativeAttr is not null)
            altDefaults = DefaultsInfo.TryRead(c.Compilation, c.DeclarativeAttr, altDefaultsType.Name);
        altDefaults ??= DefaultsInfo.TryReadFromEnum(altDefaultsType);
        if (altDefaults is null)
        {
            diags.Add(Diagnostic.Create(Diagnostics.FacadeBranchInvalid, loc, primary.Name,
                $"could not resolve [assembly: ComposeDefaults(\"{altDefaultsType.Name}\", ...)] for the alternate bridge"));
            return null;
        }

        // Note: the branched slot's SlotPropertyName is the PascalCased
        // BranchOn; we pass it explicitly so PropertyName() agrees with
        // the if-condition the emitter produces.
        var branchedSlot = new FacadeSlot(extra, branchedKind, slotPropertyName: branchOn);

        return new BranchInfo(
            alternateMethodName: altMethod.Name,
            alternateUserParams: altUser,
            alternateDefaults: altDefaults!.Value,
            alternateDefaultsEnumName: altDefaultsType.Name,
            branchedSlot: branchedSlot,
            branchProperty: branchOn!);
    }

    static bool AreCompatibleSharedParams(IParameterSymbol a, IParameterSymbol b)
    {
        var aArity = KotlinFunctionArity(a.Type);
        var bArity = KotlinFunctionArity(b.Type);
        if (aArity >= 0 || bArity >= 0) return aArity == bArity;
        // Strip nullability and compare type symbols.
        var aType = a.Type.WithNullableAnnotation(NullableAnnotation.None);
        var bType = b.Type.WithNullableAnnotation(NullableAnnotation.None);
        return SymbolEqualityComparer.Default.Equals(aType, bType);
    }

    internal sealed class BranchInfo
    {
        public BranchInfo(string alternateMethodName, IReadOnlyList<IParameterSymbol> alternateUserParams,
            DefaultsInfo alternateDefaults, string alternateDefaultsEnumName,
            FacadeSlot branchedSlot, string branchProperty)
        {
            AlternateMethodName = alternateMethodName;
            AlternateUserParams = alternateUserParams;
            AlternateDefaults = alternateDefaults;
            AlternateDefaultsEnumName = alternateDefaultsEnumName;
            BranchedSlot = branchedSlot;
            BranchProperty = branchProperty;
        }
        public string AlternateMethodName { get; }
        /// <summary>Alternate bridge's user parameters, in declaration order, excluding trailing IComposer and `int defaults`.</summary>
        public IReadOnlyList<IParameterSymbol> AlternateUserParams { get; }
        public DefaultsInfo AlternateDefaults { get; }
        public string AlternateDefaultsEnumName { get; }
        public FacadeSlot BranchedSlot { get; }
        /// <summary>The PascalCased property name on the facade (e.g. "Subtitle").</summary>
        public string BranchProperty { get; }
    }

    static FacadeSlot? Classify(IParameterSymbol p, Context c, string methodName, Location loc, List<Diagnostic> diags)
    {
        // [StateHolder] — annotates the IntPtr bridge param carrying a
        // Kotlin state-holder handle. The facade emits a RememberXxxState
        // round-trip and an optional `.Jvm` population for a wrapper.
        AttributeData? stateAttr = null;
        if (c.StateHolderAttr is not null)
        {
            stateAttr = p.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.StateHolderAttr));
        }
        if (stateAttr is not null)
        {
            // Mutex with [PainterResource].
            if (c.PainterAttr is not null &&
                p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, c.PainterAttr)))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"parameter '{p.Name}' has both [StateHolder] and [PainterResource]; pick one"));
                return null;
            }
            if (p.Type.SpecialType != SpecialType.System_IntPtr)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] must annotate an 'IntPtr' parameter; '{p.Name}' is '{p.Type.ToDisplayString()}'"));
                return null;
            }
            string? remember = ReadString(stateAttr, "Remember");
            INamedTypeSymbol? stateType = ReadType(stateAttr, "StateType");
            if (string.IsNullOrEmpty(remember))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}' is missing required property 'Remember'"));
                return null;
            }
            if (stateType is null)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}' is missing required property 'StateType'"));
                return null;
            }
            if (!SyntaxFacts.IsValidIdentifier(remember!))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': Remember value '{remember}' is not a valid C# identifier"));
                return null;
            }

            // Validate the Remember bridge resolves to a static method on
            // ComposeNet.ComposeBridges whose last parameter is an
            // IComposer and that returns IntPtr. Any number of leading
            // user parameters is allowed (Phase 4 = zero, Phase 4b = N).
            var bridgesType = c.Compilation.GetTypeByMetadataName("ComposeNet.ComposeBridges");
            if (bridgesType is null)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': cannot resolve type 'ComposeNet.ComposeBridges'"));
                return null;
            }
            var rememberMethods = bridgesType.GetMembers(remember!).OfType<IMethodSymbol>().ToArray();
            if (rememberMethods.Length == 0)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': no static method 'ComposeBridges.{remember}' found"));
                return null;
            }
            var rememberFit = rememberMethods.FirstOrDefault(m =>
                m.IsStatic &&
                m.Parameters.Length >= 1 &&
                ComposeDefaultsGenerator.IsComposer(m.Parameters[m.Parameters.Length - 1].Type) &&
                m.ReturnType.SpecialType == SpecialType.System_IntPtr);
            if (rememberFit is null)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': 'ComposeBridges.{remember}' must be a static method whose last parameter is an IComposer and that returns IntPtr"));
                return null;
            }

            // Validate StateType has a writable instance Jvm field.
            var jvmMember = stateType.GetMembers("Jvm").OfType<IFieldSymbol>().FirstOrDefault();
            if (jvmMember is null)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': StateType '{stateType.ToDisplayString()}' has no instance field named 'Jvm'"));
                return null;
            }
            if (jvmMember.IsStatic || jvmMember.IsConst || jvmMember.IsReadOnly)
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': StateType '{stateType.ToDisplayString()}'.Jvm must be a non-static, non-const, non-readonly field"));
                return null;
            }
            if (jvmMember.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
            {
                diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                    $"[StateHolder] on '{p.Name}': StateType '{stateType.ToDisplayString()}'.Jvm must be accessible (public or internal)"));
                return null;
            }

            // Phase 4b — Remember has N user params before composer. Each
            // user param must resolve to a readable instance member on
            // the StateType (case-insensitive PascalCase match; fall back
            // to `Initial<PascalCase>` for the Kotlin "initialX → live X"
            // wrapper convention). Phase 4b also requires an accessible
            // parameterless construction path on the StateType so the
            // ctor can auto-create a default wrapper when the caller
            // passes null.
            var rememberUserParams = rememberFit.Parameters
                .Take(rememberFit.Parameters.Length - 1)
                .ToArray();
            string[] rememberArgExpressions = System.Array.Empty<string>();
            if (rememberUserParams.Length > 0)
            {
                if (!HasAccessibleParameterlessConstructor(stateType))
                {
                    diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                        $"[StateHolder] on '{p.Name}': parameterised Remember requires StateType '{stateType.ToDisplayString()}' to be constructible with no arguments (parameterless ctor or all-defaulted-param ctor)"));
                    return null;
                }

                rememberArgExpressions = new string[rememberUserParams.Length];
                for (int i = 0; i < rememberUserParams.Length; i++)
                {
                    var up = rememberUserParams[i];
                    var resolved = ResolveStateMember(stateType, up.Name);
                    if (resolved is null)
                    {
                        diags.Add(Diagnostic.Create(Diagnostics.FacadeStateHolderInvalid, loc, methodName,
                            $"[StateHolder] on '{p.Name}': cannot resolve Remember parameter '{up.Name}' on StateType '{stateType.ToDisplayString()}'; expected a readable instance member named '{Pascal(up.Name)}' or 'Initial{Pascal(up.Name)}'"));
                        return null;
                    }
                    rememberArgExpressions[i] = "_state!." + resolved;
                }
            }

            return new FacadeSlot(p, FacadeSlotKind.StateHolder,
                rememberMethodName: remember,
                stateWrapperType: stateType,
                stateJvmType: jvmMember.Type,
                rememberArgExpressions: rememberArgExpressions,
                sharedState: ReadBool(stateAttr, "SharedState"));
        }

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

        // Compose @JvmInline value-class types (Dp?/Sp?/Em?/TextAlign?)
        // and reference-typed wrappers (FontWeight?/TextDecoration?/
        // Shape?). Surfaced as nullable auto-properties; the bridge
        // generator handles JNI lowering and auto-mask bit clearing.
        if (IsOptionalValueType(p.Type, p.NullableAnnotation))
            return new FacadeSlot(p, FacadeSlotKind.OptionalValue);

        diags.Add(Diagnostic.Create(Diagnostics.FacadeUnsupportedParameter, loc, methodName, p.Name, p.Type.ToDisplayString()));
        return null;
    }

    static string Emit(string className, string bridgeMethodName, string? scope,
        IParameterSymbol composerParam, IReadOnlyList<FacadeSlot> slots,
        bool isMultiSlot, bool callerProvidesDefaults, IParameterSymbol? defaultsParam,
        DefaultsInfo? defaults, string? defaultsEnumName,
        string? themeColor, FacadeSlot? colorSlot,
        IReadOnlyList<IParameterSymbol> primaryUserParams,
        BranchInfo? branchInfo)
    {
        // After classification, only the container's body survives as
        // Content2/3 (multi-slot leafs re-classified to Named/Required).
        // For the hybrid "container + named slots" shape (e.g.
        // BottomAppBar's required `actions` Function3 + nullable
        // `floatingActionButton` Function2 slot), the Content2/3 body
        // and the Named slots coexist — the class still derives from
        // ComposableContainer and the body wraps RenderChildren.
        bool isContainer = slots.Any(s => s.Kind is FacadeSlotKind.Content2 or FacadeSlotKind.Content3);
        string baseClass = isContainer ? "global::ComposeNet.ComposableContainer" : "global::ComposeNet.ComposableNode";

        // Ctor slots: every non-modifier, non-named-property slot.
        // StateHolder slots go LAST (they have default = null) so all
        // required slots come first and stay positional.
        var ctorSlotsAll = slots.Where(s => IsCtorSlot(s)).ToArray();
        var ctorSlots = ctorSlotsAll
            .Where(s => s.Kind != FacadeSlotKind.StateHolder)
            .Concat(ctorSlotsAll.Where(s => s.Kind == FacadeSlotKind.StateHolder))
            .ToArray();
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
            sb.AppendLine("        /// <summary>Optional explicit <see cref=\"global::ComposeNet.Color\"/>. Leave at the default to inherit the active <c>MaterialTheme.colorScheme</c> fallback.</summary>");
            sb.AppendLine("        public global::ComposeNet.Color ContainerColor { get; set; }");
        }

        // Phase 3 — named properties.
        foreach (var s in namedSlots)
        {
            sb.Append("        public global::ComposeNet.ComposableNode? ").Append(PropertyName(s)).AppendLine(" { get; set; }");
        }

        // OptionalValue — Compose value-class types (Dp?/Sp?/Em?/
        // TextAlign?) and reference-typed wrappers (FontWeight?/
        // TextDecoration?/Shape?). Surfaced as nullable auto-properties
        // for object-init syntax: `new Text("hi") { FontSize = 24.Sp() }`.
        var optionalValueSlots = slots.Where(s => s.Kind == FacadeSlotKind.OptionalValue).ToArray();
        foreach (var s in optionalValueSlots)
        {
            sb.Append("        public ").Append(OptionalValueDisplay(s)).Append(' ')
              .Append(PropertyName(s)).AppendLine(" { get; set; }");
        }

        // Constructor.
        if (ctorSlots.Length > 0)
        {
            sb.Append("        public ").Append(className).Append('(');
            for (int i = 0; i < ctorSlots.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(CtorParamType(ctorSlots[i])).Append(' ').Append(EscapeIdent(CtorIdentifier(ctorSlots[i])));
                if (ctorSlots[i].Kind == FacadeSlotKind.StateHolder)
                    sb.Append(" = null");
            }
            sb.AppendLine(")");
            sb.AppendLine("        {");
            foreach (var s in ctorSlots)
            {
                // Phase 4b — auto-create wrapper instance when the
                // Remember bridge has user params. The Render body
                // reads init values off `_state` to pass into Remember,
                // so the field must be non-null on entry.
                if (s.IsParameterisedStateHolder)
                {
                    var fqType = s.StateWrapperType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
                    sb.Append("            _").Append(CtorIdentifier(s)).Append(" = ")
                      .Append(EscapeIdent(CtorIdentifier(s))).Append(" ?? new ").Append(fqType).AppendLine("();");
                }
                else
                {
                    sb.Append("            _").Append(CtorIdentifier(s)).Append(" = ").Append(EscapeIdent(CtorIdentifier(s))).AppendLine(";");
                }
            }
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

        // StateHolder preamble — call RememberXxxState and (optionally)
        // populate the caller-supplied wrapper's Jvm field. Phase 4
        // (zero-user-param Remember): _state is nullable, Jvm is only
        // populated when the caller supplied a wrapper. Phase 4b
        // (parameterised Remember): the ctor guaranteed _state is
        // non-null, so we read init values off it and skip the
        // null-guard on the Jvm assignment. Phase 4c (SharedState=true):
        // when _state.Jvm is already populated (because an earlier
        // sibling render bound the same wrapper), skip the Remember
        // call entirely and reuse the cached JNI handle — this is what
        // lets a TimePicker and a TimeInput share state, or the
        // SearchBar family share a SearchBarState peer across the
        // collapsed bar + expanded popup.
        foreach (var s in slots.Where(s => s.Kind == FacadeSlotKind.StateHolder))
        {
            var id = CtorIdentifier(s);
            var jvmFqn = s.StateJvmType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
            if (s.SharedState)
            {
                EmitStateHolderPreambleShared(sb, s, id, jvmFqn, composerName);
            }
            else
            {
                sb.Append("            var __").Append(s.Param.Name)
                  .Append(" = global::ComposeNet.ComposeBridges.").Append(s.RememberMethodName).Append('(');
                foreach (var argExpr in s.RememberArgExpressions)
                    sb.Append(argExpr).Append(", ");
                sb.Append(composerName).AppendLine(");");
                if (s.IsParameterisedStateHolder)
                {
                    sb.Append("            if (_").Append(id).AppendLine(".Jvm is null)");
                    sb.Append("                _").Append(id).Append(".Jvm = global::Java.Lang.Object.GetObject<")
                      .Append(jvmFqn).Append(">(__").Append(s.Param.Name)
                      .AppendLine(", global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;");
                }
                else
                {
                    sb.Append("            if (_").Append(id).Append(" is not null && _").Append(id).AppendLine(".Jvm is null)");
                    sb.Append("                _").Append(id).Append(".Jvm = global::Java.Lang.Object.GetObject<")
                      .Append(jvmFqn).Append(">(__").Append(s.Param.Name)
                      .AppendLine(", global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;");
                }
            }
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
        // Branching always hoists (both branches reference __modifier in
        // their per-branch mask).
        var modifierSlot = slots.FirstOrDefault(s => s.Kind == FacadeSlotKind.Modifier);
        bool hoistModifier = modifierSlot.Param is not null && (callerProvidesDefaults || branchInfo is not null);
        if (hoistModifier)
        {
            sb.AppendLine("            var __modifier = BuildModifier();");
        }

        // Named slot wrappers (Phase 3). One per line. When branching,
        // skip the branched slot — it gets wrapped inside the if-branch
        // only (the primary bridge has no corresponding parameter).
        foreach (var s in namedSlots)
        {
            if (branchInfo is not null &&
                SymbolEqualityComparer.Default.Equals(s.Param, branchInfo.BranchedSlot.Param))
            {
                continue;
            }
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
            sb.Append("            long __color = (long)ContainerColor != 0L ? (long)ContainerColor : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(")
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

        // Phase 3 — auto-mask defaults + bridge call.
        if (branchInfo is not null)
        {
            EmitBranchedRender(sb, indent, bridgeMethodName, slots, primaryUserParams,
                defaults!.Value, defaultsEnumName!, branchInfo, composerName, hoistModifier);
        }
        else
        {
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
        }

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

    static void EmitBranchedRender(StringBuilder sb, string indent,
        string primaryMethodName, IReadOnlyList<FacadeSlot> slots,
        IReadOnlyList<IParameterSymbol> primaryUserParams,
        DefaultsInfo primaryDefaults, string primaryDefaultsEnumName,
        BranchInfo branch, string composerName, bool hoistModifier)
    {
        // Lookup table from bridge-param-name to facade slot. Built once
        // and reused for both branches' call emission. Names that only
        // exist in the alternate (the branched slot) map to the
        // synthesized slot; names shared by both bridges map to the
        // primary's classification.
        var slotByName = new Dictionary<string, FacadeSlot>(StringComparer.Ordinal);
        foreach (var s in slots)
            slotByName[s.Param.Name] = s;

        var branched = branch.BranchedSlot;
        var branchPropName = branch.BranchProperty;
        int arity = branched.Kind == FacadeSlotKind.NamedFunction3 ? 3 : 2;
        string wrap = arity == 3 ? "Wrap3" : "Wrap2";

        var allNamedSlots = slots.Where(s => s.Kind is FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3
            or FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3).ToArray();

        // Alternate branch: the branched slot is provided.
        sb.Append(indent).Append("if (").Append(branchPropName).AppendLine(" is not null)");
        sb.Append(indent).AppendLine("{");
        var inner = indent + "    ";
        sb.Append(inner).Append("var __").Append(branched.Param.Name)
          .Append(" = global::ComposeNet.ComposableLambdas.").Append(wrap)
          .Append('(').Append(composerName).Append(", c => ")
          .Append(branchPropName).AppendLine("!.Render(c));");
        EmitDefaultsMask(sb, inner, branch.AlternateDefaults, slots, allNamedSlots,
            hasModifier: hoistModifier, hasPainter: false);
        EmitBridgeCallByParams(sb, inner, branch.AlternateMethodName, branch.AlternateUserParams,
            slotByName, composerName, hoistModifier);
        sb.Append(indent).AppendLine("}");

        // Primary branch: branched slot is null.
        sb.Append(indent).AppendLine("else");
        sb.Append(indent).AppendLine("{");
        EmitDefaultsMask(sb, inner, primaryDefaults, slots, allNamedSlots,
            hasModifier: hoistModifier, hasPainter: false);
        EmitBridgeCallByParams(sb, inner, primaryMethodName, primaryUserParams,
            slotByName, composerName, hoistModifier);
        sb.Append(indent).AppendLine("}");
    }

    static void EmitBridgeCallByParams(StringBuilder sb, string indent,
        string bridgeMethodName, IReadOnlyList<IParameterSymbol> bridgeUserParams,
        IReadOnlyDictionary<string, FacadeSlot> slotByName,
        string composerName, bool hoistModifier)
    {
        sb.Append(indent).Append("global::ComposeNet.ComposeBridges.").Append(bridgeMethodName).Append('(');
        foreach (var p in bridgeUserParams)
        {
            if (slotByName.TryGetValue(p.Name, out var slot))
                sb.Append(BridgeArgExpr(slot, hoistModifier));
            else
                sb.Append("default");
            sb.Append(", ");
        }
        sb.Append("__defaults, ").Append(composerName).AppendLine(");");
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

    static void EmitStateHolderPreambleShared(StringBuilder sb, FacadeSlot s,
        string id, string jvmFqn, string composerName)
    {
        // Phase 4c: skip the RememberXxxState call when _state.Jvm is
        // already populated (a sibling facade rendered first and bound
        // this wrapper to its peer). When the cache miss path runs,
        // populate _state.Jvm so the next sibling reuses the handle.
        sb.Append("            global::System.IntPtr __").Append(s.Param.Name).AppendLine(";");
        if (s.IsParameterisedStateHolder)
        {
            // Phase 4b shape: _state is non-null thanks to ctor auto-create.
            // The `!` on the first dereference flow-narrows _state to
            // non-null for the rest of the method (matches the existing
            // non-shared Phase 4b convention).
            sb.Append("            if (_").Append(id).AppendLine("!.Jvm is not null)");
            sb.AppendLine("            {");
            sb.Append("                __").Append(s.Param.Name)
              .Append(" = ((global::Android.Runtime.IJavaObject)_").Append(id).AppendLine(".Jvm!).Handle;");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            sb.Append("                __").Append(s.Param.Name)
              .Append(" = global::ComposeNet.ComposeBridges.").Append(s.RememberMethodName).Append('(');
            foreach (var argExpr in s.RememberArgExpressions)
                sb.Append(argExpr).Append(", ");
            sb.Append(composerName).AppendLine(");");
            sb.Append("                _").Append(id).Append(".Jvm = global::Java.Lang.Object.GetObject<")
              .Append(jvmFqn).Append(">(__").Append(s.Param.Name)
              .AppendLine(", global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;");
            sb.AppendLine("            }");
        }
        else
        {
            // Phase 4 shape: _state may be null; only cache when present.
            // Use an explicit `if (_state is not null)` so flow narrowing
            // applies to both branches without sprinkling null-forgiving
            // operators throughout.
            sb.Append("            if (_").Append(id).Append(" is not null && _").Append(id).AppendLine(".Jvm is not null)");
            sb.AppendLine("            {");
            sb.Append("                __").Append(s.Param.Name)
              .Append(" = ((global::Android.Runtime.IJavaObject)_").Append(id).AppendLine(".Jvm).Handle;");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            sb.Append("                __").Append(s.Param.Name)
              .Append(" = global::ComposeNet.ComposeBridges.").Append(s.RememberMethodName).Append('(');
            foreach (var argExpr in s.RememberArgExpressions)
                sb.Append(argExpr).Append(", ");
            sb.Append(composerName).AppendLine(");");
            sb.Append("                if (_").Append(id).AppendLine(" is not null)");
            sb.Append("                    _").Append(id).Append(".Jvm = global::Java.Lang.Object.GetObject<")
              .Append(jvmFqn).Append(">(__").Append(s.Param.Name)
              .AppendLine(", global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;");
            sb.AppendLine("            }");
        }
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
                case FacadeSlotKind.OptionalValue:
                    sb.Append(indent).Append("if (").Append(PropertyName(s)).Append(" is not null) __defaults &= ~(int)global::ComposeNet.")
                      .Append(d.EnumName).Append('.').Append(bitMember).AppendLine(";");
                    break;
                case FacadeSlotKind.RequiredFunction2:
                case FacadeSlotKind.RequiredFunction3:
                case FacadeSlotKind.PainterResource:
                case FacadeSlotKind.Primitive:
                case FacadeSlotKind.ThemeColor:
                case FacadeSlotKind.StateHolder:
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
            FacadeSlotKind.StateHolder      => "__" + s.Param.Name,
            FacadeSlotKind.OptionalValue    => PropertyName(s),
            _ => "default",
        };

    static bool IsCtorSlot(FacadeSlot s) =>
        s.Kind is FacadeSlotKind.OnClick or FacadeSlotKind.Primitive or FacadeSlotKind.Callback
            or FacadeSlotKind.PainterResource or FacadeSlotKind.StateHolder;

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
            FacadeSlotKind.StateHolder => slot.StateWrapperType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)) + "?",
            FacadeSlotKind.Primitive => slot.Param.Type.ToDisplayString(format),
            _ => slot.Param.Type.ToDisplayString(),
        };
    }

    static string PropertyName(FacadeSlot s) => s.SlotPropertyName ?? Pascal(s.Param.Name);

    /// <summary>
    /// C# type display for an <see cref="FacadeSlotKind.OptionalValue"/>
    /// property declaration. For value-class types (Dp/Sp/Em/TextAlign)
    /// the param is already <c>Nullable&lt;T&gt;</c> and round-trips
    /// fine with <c>FullyQualifiedFormat</c>. For reference-typed
    /// wrappers (FontWeight/TextDecoration/Shape) we have to append
    /// <c>?</c> manually because the unannotated symbol does not carry
    /// nullable annotation through <c>ToDisplayString</c>.
    /// </summary>
    static string OptionalValueDisplay(FacadeSlot s)
    {
        var t = s.Param.Type;
        var format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        var rendered = t.ToDisplayString(format);
        if (t.IsReferenceType && !rendered.EndsWith("?")) rendered += "?";
        return rendered;
    }

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
        IsJavaEnum(type) ||
        type.SpecialType is SpecialType.System_String
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Boolean
            or SpecialType.System_Single
            or SpecialType.System_Double;

    /// <summary>
    /// True when <paramref name="type"/> derives (transitively) from
    /// <c>Java.Lang.Enum</c> — i.e. it's a Kotlin/Java enum class
    /// generated by the Xamarin.Android binding (e.g. Compose's
    /// <c>ToggleableState</c>: <c>On</c>, <c>Off</c>,
    /// <c>Indeterminate</c>). Recognized as a primitive-like ctor
    /// slot: surfaced as a positional ctor parameter and forwarded
    /// to the bridge unchanged.
    /// </summary>
    static bool IsJavaEnum(ITypeSymbol type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.Name == "Enum" &&
                t.ContainingNamespace?.ToDisplayString() == "Java.Lang")
                return true;
        }
        return false;
    }

    /// <summary>
    /// True for parameters typed as <c>Nullable&lt;T&gt;</c> where <c>T</c>
    /// is a recognized Compose <c>@JvmInline value class</c> (Dp/Sp/Em/
    /// TextOverflow), for <em>nullable</em> reference-typed wrappers in
    /// <see cref="ComposeReferenceTypes"/> (FontWeight/FontStyle/
    /// FontFamily/TextAlign/TextDecoration/Shape), or for
    /// <c>Nullable&lt;T&gt;</c> where <c>T</c> is a JNI-friendly
    /// primitive (<c>bool</c>, <c>int</c>, <c>long</c>, <c>float</c>,
    /// <c>double</c>) — the "optional Compose primitive" shape:
    /// <c>null</c> → leave the <c>$default</c> bit set; a value clears
    /// the bit and lowers to the primitive JNI slot. All three shapes
    /// surface as <see cref="FacadeSlotKind.OptionalValue"/>
    /// auto-properties on the generated facade. Non-nullable reference
    /// wrappers do not qualify — emitting a nullable auto-property for
    /// them would pass <c>null</c> to a non-nullable bridge parameter.
    /// </summary>
    static bool IsOptionalValueType(ITypeSymbol type, NullableAnnotation annotation)
    {
        if (ComposeValueTypes.TryGet(type, out _, out _)) return true;
        // Nullable<primitive>: bool?, int?, long?, float?, double?.
        if (IsNullablePrimitive(type)) return true;
        // Nullable reference-type wrapper: T? where T is recognized.
        // The C# nullable-annotation flow gives us the underlying type
        // directly (no Nullable<T> wrapping) for reference types, so
        // we must consult the annotation explicitly.
        if (type.IsReferenceType
            && annotation == NullableAnnotation.Annotated
            && ComposeReferenceTypes.IsRecognized(type))
            return true;
        return false;
    }

    /// <summary>
    /// Mirror of <c>ComposeBridgeGenerator.IsNullablePrimitive</c> —
    /// see that helper for the rationale. Kept private here because
    /// the bridge generator's copy lives in a separate translation
    /// unit and the registry of "primitive Nullable types" is small
    /// and stable.
    /// </summary>
    static bool IsNullablePrimitive(ITypeSymbol t)
    {
        if (t is not INamedTypeSymbol n) return false;
        if (n.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T) return false;
        if (n.TypeArguments.Length != 1) return false;
        return n.TypeArguments[0].SpecialType is
            SpecialType.System_Boolean
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double;
    }

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

    static bool ReadBool(AttributeData attr, string name)
    {
        foreach (var na in attr.NamedArguments)
            if (na.Key == name && na.Value.Value is bool b) return b;
        return false;
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

    /// <summary>
    /// Phase 4b — resolve the wrapper-side member name for a Remember
    /// bridge user parameter. Tries an exact <see cref="Pascal"/> match
    /// first, then falls back to <c>Initial&lt;Pascal&gt;</c> for the
    /// Kotlin convention where <c>initialX</c> remember args correspond
    /// to live wrapper property <c>X</c>. Looks at both properties and
    /// fields, accepting any readable, non-static, accessible member.
    /// Returns the resolved C# identifier or <c>null</c> when no match.
    /// </summary>
    static string? ResolveStateMember(INamedTypeSymbol stateType, string paramName)
    {
        var pascal = Pascal(paramName);
        if (FindReadableMember(stateType, pascal) is { } direct) return direct;
        if (FindReadableMember(stateType, "Initial" + pascal) is { } prefixed) return prefixed;
        return null;
    }

    static string? FindReadableMember(INamedTypeSymbol stateType, string name)
    {
        // Walk the inheritance chain so inherited members count. We stop
        // at the first match — properties and fields share a name space
        // in C#, so a class can't declare both with the same name.
        for (var t = (INamedTypeSymbol?)stateType; t is not null; t = t.BaseType)
        {
            foreach (var m in t.GetMembers(name))
            {
                if (m.IsStatic) continue;
                if (m.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal
                    or Accessibility.ProtectedOrInternal))
                    continue;
                if (m is IPropertySymbol prop && prop.GetMethod is not null) return prop.Name;
                if (m is IFieldSymbol f) return f.Name;
            }
        }
        return null;
    }

    /// <summary>
    /// True when <paramref name="stateType"/> exposes an accessible
    /// (public or internal) parameterless construction path —
    /// <c>new T()</c> compiles. Counts an explicit parameterless ctor
    /// as well as an all-defaulted-params ctor (e.g.
    /// <c>TimePickerState(int initialHour = 12, …)</c>). Also accepts
    /// the implicit ctor when no instance ctors are declared.
    /// </summary>
    static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol stateType)
    {
        var ctors = stateType.InstanceConstructors;
        if (ctors.Length == 0) return true;
        foreach (var c in ctors)
        {
            if (c.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal
                or Accessibility.ProtectedOrInternal))
                continue;
            if (c.Parameters.Length == 0) return true;
            if (c.Parameters.All(p => p.HasExplicitDefaultValue)) return true;
        }
        return false;
    }

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
        StateHolder,
        OptionalValue,
    }

    internal readonly struct FacadeSlot
    {
        public FacadeSlot(IParameterSymbol param, FacadeSlotKind kind,
            ITypeSymbol? callbackType = null, string? slotPropertyName = null,
            string? rememberMethodName = null, INamedTypeSymbol? stateWrapperType = null,
            ITypeSymbol? stateJvmType = null, string[]? rememberArgExpressions = null,
            bool sharedState = false)
        {
            Param = param;
            Kind = kind;
            CallbackType = callbackType;
            SlotPropertyName = slotPropertyName;
            RememberMethodName = rememberMethodName;
            StateWrapperType = stateWrapperType;
            StateJvmType = stateJvmType;
            RememberArgExpressions = rememberArgExpressions ?? System.Array.Empty<string>();
            SharedState = sharedState;
        }
        public IParameterSymbol Param { get; }
        public FacadeSlotKind Kind { get; }
        public ITypeSymbol? CallbackType { get; }
        public string? SlotPropertyName { get; }
        public string? RememberMethodName { get; }
        public INamedTypeSymbol? StateWrapperType { get; }
        public ITypeSymbol? StateJvmType { get; }
        /// <summary>
        /// Phase 4b — one C# expression per user parameter of the
        /// <c>Remember*State</c> bridge (composer excluded). Each expression
        /// reads a member of the caller-supplied state wrapper, e.g.
        /// <c>_state!.InitialHour</c>. Empty when the Remember bridge has
        /// zero user params (Phase 4).
        /// </summary>
        public string[] RememberArgExpressions { get; }
        /// <summary>
        /// Phase 4c — when <c>true</c>, the generated Render preamble
        /// checks whether <c>_state.Jvm</c> is already populated (from an
        /// earlier sibling render) and skips the <c>RememberXxxState</c>
        /// call in that case, reusing the cached JNI handle.
        /// </summary>
        public bool SharedState { get; }
        public bool HasSlotAttribute => SlotPropertyName is not null;
        public bool IsNullableSlot => Param.NullableAnnotation == NullableAnnotation.Annotated
            && KindIsFnSlot(Kind);
        public bool IsParameterisedStateHolder =>
            Kind == FacadeSlotKind.StateHolder && RememberArgExpressions.Length > 0;
        static bool KindIsFnSlot(FacadeSlotKind k) =>
            k is FacadeSlotKind.Content2 or FacadeSlotKind.Content3
              or FacadeSlotKind.NamedFunction2 or FacadeSlotKind.NamedFunction3
              or FacadeSlotKind.RequiredFunction2 or FacadeSlotKind.RequiredFunction3;
        public FacadeSlot WithKind(FacadeSlotKind newKind) =>
            new(Param, newKind, CallbackType, SlotPropertyName, RememberMethodName,
                StateWrapperType, StateJvmType, RememberArgExpressions, SharedState);
    }
}
