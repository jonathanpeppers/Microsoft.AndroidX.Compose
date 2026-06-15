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

    /// <summary>
    /// The DI-registered <see cref="ThemeManager"/> singleton (or
    /// <c>null</c> when the consumer skipped
    /// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>).
    /// Cached once per handler from
    /// <see cref="ViewHandler.MauiContext"/> via
    /// <see cref="SetMauiContext"/> so subclasses can read
    /// <see cref="ThemeManager.IsDark"/> inside <see cref="BuildNode"/>
    /// without paying a DI lookup per recomposition.
    /// </summary>
    /// <remarks>
    /// Reading <see cref="ThemeManager.IsDark"/>'s
    /// <see cref="MutableState{T}.Value"/> inside the composable scope
    /// registers a snapshot read, so flipping MAUI's
    /// <see cref="Microsoft.Maui.Controls.Application.RequestedTheme"/>
    /// recomposes any subscribing leaf against the new theme.
    /// </remarks>
    protected ThemeManager? Theme { get; private set; }

    /// <inheritdoc/>
    protected ComposeElementHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
        : base(mapper, commandMapper) { }

    /// <inheritdoc/>
    protected sealed override ComposeView CreatePlatformView() => new(Context);

    /// <inheritdoc/>
    public override void SetMauiContext(IMauiContext mauiContext)
    {
        base.SetMauiContext(mauiContext);
        // Re-cache on every SetMauiContext call so the Theme refresh
        // tracks any context swap MAUI may push through (rare in
        // practice but contractually allowed).
        Theme = mauiContext.Services.GetService<ThemeManager>();
    }

    /// <inheritdoc/>
    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        // ComposeView.SetContent is lazy — it only kicks off a
        // composition when the view is attached to a window. Inside
        // a Compose-aware parent the walker calls BuildNode directly
        // and this composition is never created.
        var platformView = PlatformView
            ?? throw new InvalidOperationException("PlatformView not set on ComposeElementHandler.");
        platformView.SetContent(BuildNode);
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

