using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
using FrameLayout = Android.Widget.FrameLayout;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.NavigationPage"/> handler
/// that renders the navigation stack through Jetpack Compose's
/// Material 3 <see cref="Scaffold"/> + <see cref="TopAppBar"/> chrome.
/// Replaces MAUI's stock <c>NavigationViewHandler</c> when the
/// consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Unlike the stock handler — which hosts each pushed page in a
/// fragment under <c>FragmentContainerView</c> and renders chrome via
/// AppCompat <c>Toolbar</c> — this handler owns a single
/// <see cref="ComposeView"/> rooted at the navigation host and folds
/// the current top page into the composition via an embedded
/// <c>AndroidView</c>. The pushed page's own <see cref="PageHandler"/>
/// (resolved through <see cref="ComposeWalker"/> when the consumer
/// also registered the Compose-backed <c>Page</c> handler) installs
/// its own composition behind a Compose-managed
/// <see cref="FrameLayout"/> host the chrome wraps.</para>
///
/// <para><b>Navigation contract.</b> MAUI's cross-platform layer
/// calls <see cref="IStackNavigation.RequestNavigation"/> whenever
/// the stack changes (push, pop, insert-before, remove). The handler
/// snapshots the new stack, bumps a Compose
/// <see cref="MutableState{T}"/> "version" counter to trigger
/// recomposition, then invokes
/// <see cref="IStackNavigation.NavigationFinished"/> to fulfil
/// MAUI's promise. The visual swap happens in the next composition
/// pass.</para>
///
/// <para><b>Hardware back.</b> Stock MAUI's
/// <c>NavigationViewHandler</c> attaches an
/// <c>OnBackPressedCallback</c> via <c>BackButtonBehavior</c>; we
/// rely on the same cross-platform plumbing — MAUI's
/// <c>Window.HandleBackButton</c> calls
/// <see cref="Microsoft.Maui.Controls.NavigationPage.PopAsync()"/>
/// which round-trips back through
/// <see cref="IStackNavigation.RequestNavigation"/>. The top app bar
/// also renders an <see cref="IconButton"/> back arrow whenever the
/// stack depth is &gt; 1.</para>
///
/// <para><b>Animation.</b> v1 does an immediate swap. The
/// <see cref="NavigationRequest.Animated"/> hint is captured but
/// ignored; a follow-up slice will wrap the body in Compose's
/// <c>AnimatedContent</c> for slide-in / slide-out motion (#317).</para>
/// </remarks>
public partial class NavigationPageHandler : ViewHandler<IStackNavigationView, ComposeView>, INavigationViewHandler
{
    /// <summary>
    /// Property mapper. Inherits the cross-cutting <c>ViewMapper</c>
    /// entries (visibility, opacity, transforms) — none of those have
    /// Compose-side equivalents on the navigation chrome, but
    /// forwarding through <c>ViewMapper</c> keeps the outer
    /// <see cref="ComposeView"/>'s Android-side properties in sync
    /// for cases where a parent layout reads them.
    /// </summary>
    public static IPropertyMapper<IStackNavigationView, NavigationPageHandler> Mapper =
        new PropertyMapper<IStackNavigationView, NavigationPageHandler>(ViewHandler.ViewMapper);

    /// <summary>
    /// Command mapper. Adds <see cref="MapRequestNavigation"/> so MAUI's
    /// cross-platform navigation contract reaches the Compose
    /// composition.
    /// </summary>
    public static CommandMapper<IStackNavigationView, NavigationPageHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = MapRequestNavigation,
        };

    // Bumped in MapRequestNavigation; subscribed in Build so Compose
    // recomposes the Scaffold body whenever the navigation stack
    // changes. Holds a stable identity across the handler's lifetime
    // — the value is just a monotonic counter.
    readonly MutableState<int> _stackVersion = new(0);

    // Snapshot of the navigation stack as of the last
    // RequestNavigation call. Read inside Build *after* registering a
    // snapshot read on _stackVersion so writes here always trigger a
    // recomposition.
    IReadOnlyList<IView> _stack = [];

    // Cached singleton resolved at CreatePlatformView time so Build
    // can wrap its Scaffold in a MaterialTheme that flips with MAUI's
    // RequestedTheme. The pushed page lives in its own ComposeView
    // and gets its own MaterialTheme via PageHandler.MapContent —
    // theming does NOT propagate across separate compositions, so
    // this handler's chrome (TopAppBar background, title color, back
    // arrow's LocalContentColor) needs its own wrapper to render
    // correctly in dark mode (#262 footgun).
    ThemeManager? _theme;

    /// <summary>Construct a handler with the default mappers.</summary>
    public NavigationPageHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public NavigationPageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    IStackNavigationView INavigationViewHandler.VirtualView =>
        VirtualView ?? throw new InvalidOperationException(
            "VirtualView not set on NavigationPageHandler.");

    /// <inheritdoc/>
    AView INavigationViewHandler.PlatformView =>
        PlatformView ?? throw new InvalidOperationException(
            "PlatformView not set on NavigationPageHandler.");

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var context = Context
            ?? throw new InvalidOperationException("Context not set on NavigationPageHandler.");
        var mauiContext = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on NavigationPageHandler.");

        // Resolve once. GetService (not GetRequiredService) so a
        // consumer who registered this handler manually without
        // calling UseAndroidXCompose() still works — Build falls
        // through to a bare Scaffold (matches the leaf-handler
        // contract used by ComposeElementHandler / LabelHandler).
        _theme = mauiContext.Services.GetService<ThemeManager>();

        var compose = new ComposeView(context)
        {
            LayoutParameters = new AViewGroup.LayoutParams(
                AViewGroup.LayoutParams.MatchParent,
                AViewGroup.LayoutParams.MatchParent),
        };
        compose.SetContent(Build);
        return compose;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    /// <summary>
    /// MAUI's cross-platform navigation entry point. Captures the new
    /// stack, bumps the recomposition trigger, and resolves the
    /// promise. Animation is currently ignored (immediate swap).
    /// </summary>
    public static void MapRequestNavigation(NavigationPageHandler handler, IStackNavigationView view, object? arg)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(view);

        if (arg is not NavigationRequest request)
            return;

        handler._stack = request.NavigationStack;
        handler._stackVersion.Value++;

        // Tell MAUI we've taken delivery so its NavigationProxy can
        // complete the outstanding push/pop Task. We finish
        // synchronously even though the actual Compose recomposition
        // is asynchronous — matches stock semantics for
        // non-animated transitions and avoids stalling
        // back-to-back PushAsync() calls.
        view.NavigationFinished(request.NavigationStack);
    }

    ComposableNode Build(IComposer composer)
    {
        // Snapshot-read the version counter so any write to it in
        // MapRequestNavigation invalidates this composition.
        _ = _stackVersion.Value;

        var stack   = _stack;
        var current = stack.Count > 0 ? stack[stack.Count - 1] : null;
        var context = MauiContext
            ?? throw new InvalidOperationException(
                "MauiContext not set on NavigationPageHandler.");

        var scaffold = new Scaffold
        {
            TopBar = BuildTopBar(current, stack.Count),
            Body   = BuildBody(current, context),
        };

        // Bare Scaffold when ThemeManager wasn't registered (consumer
        // skipped UseAndroidXCompose). Otherwise wrap so the chrome
        // tracks MAUI's RequestedTheme. Reading IsDark.Value inside
        // the composable scope ties this composition to the singleton
        // state, so theme flips recompose the chrome.
        if (_theme is null)
            return scaffold;

        // C# disallows mixing property assignments with collection-init
        // items in one initializer block (CS0747), so build then Add.
        var themed = new MaterialTheme
        {
            Dark            = _theme.IsDark.Value,
            UseDynamicColor = false,
        };
        themed.Add(scaffold);
        return themed;
    }

    ComposableNode BuildTopBar(IView? current, int stackDepth)
    {
        var title = current switch
        {
            MauiPage page => page.Title ?? string.Empty,
            _             => string.Empty,
        };

        var bar = new TopAppBar { Title = new Text(title) };
        if (stackDepth > 1)
        {
            bar.NavigationIcon = new IconButton(onClick: OnBackPressed)
            {
                // Plain glyph until the M3 ArrowBack icon is wrapped
                // (tracked alongside #234). Renders with
                // LocalContentColor = onSurface so it tints correctly
                // in both light + dark themes — the MaterialTheme
                // wrapper in Build (this handler's own composition)
                // is what supplies LocalContentColor here, NOT
                // PageHandler's MaterialTheme: the pushed page lives
                // in a separate ComposeView and theme contexts don't
                // propagate across compositions.
                new Text("\u2190"),
            };
        }
        return bar;
    }

    void OnBackPressed()
    {
        if (VirtualView is Microsoft.Maui.Controls.NavigationPage np &&
            np.Navigation.NavigationStack.Count > 1)
        {
            _ = np.Navigation.PopAsync();
        }
    }

    ComposableNode BuildBody(IView? current, IMauiContext context)
    {
        if (current is null)
            return new Box { Modifier = Modifier.FillMaxSize() };

        // Capture the page reference for the update lambda. A single
        // long-lived FrameLayout is the AndroidView host; the update
        // lambda swaps its child whenever current changes. Compose
        // caches the FrameLayout across recompositions (AndroidView's
        // documented behaviour) so we don't churn host views per
        // push/pop.
        var page = current;
        return new AndroidView(
            factory: ctx => new FrameLayout(ctx)
            {
                LayoutParameters = new AViewGroup.LayoutParams(
                    AViewGroup.LayoutParams.MatchParent,
                    AViewGroup.LayoutParams.MatchParent),
            },
            update: host =>
            {
                var frame    = (FrameLayout)host;
                var platform = page.ToPlatform(context);

                // Already showing the right view? Done.
                if (frame.ChildCount == 1 && ReferenceEquals(frame.GetChildAt(0), platform))
                    return;

                // Drop whatever's there. The detached pages keep their
                // handlers (we never DisconnectHandler), so popping
                // back reuses the same PlatformView.
                frame.RemoveAllViews();

                // The pushed page may have been hosted elsewhere in a
                // prior layout pass; detach before re-adding.
                if (platform.Parent is AViewGroup oldParent)
                    oldParent.RemoveView(platform);

                frame.AddView(platform, new FrameLayout.LayoutParams(
                    FrameLayout.LayoutParams.MatchParent,
                    FrameLayout.LayoutParams.MatchParent));
            })
        {
            Modifier = Modifier.FillMaxSize(),
        };
    }
}
