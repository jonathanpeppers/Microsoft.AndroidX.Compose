using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;

namespace Microsoft.AndroidX.Compose.Maui;

/// <summary>
/// Base class for every handler in <c>Microsoft.AndroidX.Compose.Maui</c>.
/// Owns a <see cref="ComposeView"/> as <c>PlatformView</c> (so the
/// handler satisfies MAUI's "<c>PlatformView</c> non-null" contract
/// and can be inserted into a stock ViewGroup as a fallback) and
/// exposes a <see cref="BuildNode(IComposer)"/> extension point used
/// by <see cref="ComposeWalker.Render(IView, IComposer, IMauiContext)"/>
/// to fold the handler's render into the enclosing page composition.
/// </summary>
/// <remarks>
/// <para>The contract:</para>
/// <list type="bullet">
///   <item>
///     <description>Mappers write into <see cref="MutableState{T}"/>
///     slots on the handler (unchanged from the prior per-leaf
///     <c>ComposeView</c> model). The same instance is shared between
///     the fallback composition (when our <c>ComposeView</c> ends up
///     attached to a stock ViewGroup) and the page-rooted
///     composition, so MAUI property changes always trigger
///     recomposition wherever this handler is being rendered.</description>
///   </item>
///   <item>
///     <description><see cref="BuildNode(IComposer)"/> returns a
///     <see cref="ComposableNode"/>. The walker hands it the live
///     <see cref="IComposer"/>; deferred slot reads happen at
///     <see cref="ComposableNode.Render(IComposer)"/> time.</description>
///   </item>
/// </list>
///
/// <para>The fallback path is automatic: <see cref="ComposeView.SetContent"/>
/// is lazy — it stashes the content lambda and only creates the
/// composition when the view is attached to a window. Inside a
/// Compose-aware parent, the walker calls
/// <see cref="IComposeHandler.BuildNode(IComposer)"/> directly and
/// the leaf's <c>PlatformView</c> never gets attached, so no
/// composition starts and there's no per-leaf Recomposer overhead.
/// Inside a stock parent (e.g. a Grid we didn't override), MAUI calls
/// <c>ToPlatform()</c>, attaches the <c>ComposeView</c>, and the
/// fallback composition spins up automatically.</para>
/// </remarks>
public abstract class ComposeElementHandler<TVirtualView> : ViewHandler<TVirtualView, ComposeView>, IComposeHandler
    where TVirtualView : class, IView
{
    readonly MutableState<int> _viewPropertiesVersion = new(0);

    /// <inheritdoc/>
    protected ComposeElementHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
        : base(mapper, commandMapper) { }

    /// <inheritdoc/>
    protected sealed override ComposeView CreatePlatformView() => new(Context);

    /// <inheritdoc/>
    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        // ComposeView.SetContent is lazy — it only kicks off a
        // composition when the view is attached to a window. Inside
        // a Compose-aware parent the walker calls BuildNode directly
        // and this composition is never created.
        //
        // The wrapping below — MaterialTheme over the leaf's node —
        // only matters on the standalone-fallback path (the leaf's
        // ComposeView ends up attached to a stock Android ViewGroup,
        // e.g. stock Shell's FlyoutItem template renders MAUI Labels
        // through us with no surrounding Compose parent). Without it
        // the leaf renders with Compose's pre-theme defaults
        // (LocalContentColor = Color.Black, no M3 typography), so a
        // Label drawn on a dark flyout background reads black-on-dark
        // — invisible. See #248 for the user-visible Shell flyout
        // symptom this fix targets.
        //
        // The wrap is keyed off the singleton ThemeManager so MAUI's
        // RequestedTheme / UserAppTheme drives both the page-rooted
        // composition (via PageHandler.MapContent) and this fallback
        // path consistently. When the service isn't registered (the
        // host called us without UseAndroidXCompose) we skip the wrap
        // — BuildNode still runs, just without theming.
        var platformView = PlatformView
            ?? throw new InvalidOperationException("PlatformView not set on ComposeElementHandler.");
        var theme = MauiContext?.Services.GetService<ThemeManager>();
        if (theme is null)
        {
            platformView.SetContent(BuildNode);
            return;
        }
        platformView.SetContent(c =>
        {
            var node = BuildNode(c);
            // Surface paints the M3 `surface` color behind the leaf
            // and sets LocalContentColor = onSurface so descendant
            // Text / IconButton glyphs tint against the active scheme
            // without the caller specifying TextColor. This matches
            // the wrap PageHandler.MapContent emits for the page
            // root; the leaf-level wrap kicks in only when there's
            // no enclosing Compose composition already providing it.
            var surface = new Surface();
            surface.Add(node);
            var themed = new MaterialTheme
            {
                Dark            = theme.IsDark.Value,
                UseDynamicColor = false,
            };
            themed.Add(surface);
            return themed;
        });
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    /// <inheritdoc cref="IComposeHandler.BuildNode(IComposer)"/>
    /// <remarks>
    /// Implementors construct and return the <see cref="ComposableNode"/>
    /// (e.g. a <c>Text</c>, <c>Button</c>, or container) that should
    /// run inside the enclosing composition. Reads of
    /// <see cref="MutableState{T}"/> slots typically happen inside the
    /// node's own <c>Render</c>; deferring keeps the slot subscriptions
    /// pinned to the correct compose-scope (so a state change only
    /// invalidates this subtree).
    /// </remarks>
    public abstract ComposableNode BuildNode(IComposer composer);

    /// <summary>
    /// Subscribe the current composition scope to the shared
    /// view-properties version slot. Called from
    /// <c>BuildNode</c> (or inside a deferred <c>Render</c>) so the
    /// scope re-runs when a property mapper installed by
    /// <see cref="Hosting.AppHostBuilderExtensions.RemapForCompose"/>
    /// bumps the counter. The discarded value is intentional — only
    /// the read matters; Compose registers the dependency.
    /// </summary>
    /// <remarks>
    /// Marked <c>protected internal</c> so nested helper types in
    /// other handler files (e.g. <c>ScrollViewHandler.ScrollContainer</c>)
    /// can subscribe directly inside their own deferred
    /// <c>Render</c> method without exposing the slot.
    /// </remarks>
    protected internal void SubscribeToViewProperties() => _ = _viewPropertiesVersion.Value;

    /// <inheritdoc/>
    void IComposeHandler.BumpViewPropertiesVersion() => _viewPropertiesVersion.Value++;
}

