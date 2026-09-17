using Android.Util;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using AndroidX.DrawerLayout.Widget;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
using DrawerValue = AndroidX.Compose.Material3.DrawerValue;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Compose-backed Android handler for MAUI <see cref="Microsoft.Maui.Controls.FlyoutPage"/>.
/// It renders the effective <see cref="FlyoutBehavior.Flyout"/> mode with a
/// Material 3 <see cref="ModalNavigationDrawer"/>, the effective
/// <see cref="FlyoutBehavior.Locked"/> mode with a
/// <see cref="PermanentNavigationDrawer"/>, and preserves each child page's
/// own MAUI handler inside stable Android view hosts.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Microsoft.Maui.Controls.FlyoutPage.FlyoutLayoutBehavior"/> is a
/// Controls-layer property. MAUI resolves it, device idiom, and display
/// orientation into <see cref="IFlyoutView.FlyoutBehavior"/> before invoking
/// this handler. The handler deliberately consumes that effective contract
/// instead of duplicating MAUI's adaptive policy.
/// </para>
/// <para>
/// MAUI's <see cref="IFlyoutView.FlyoutWidth"/> is also already adaptive:
/// phones report the platform default sentinel while larger displays report a
/// bounded width. Positive values are applied to the Compose drawer sheet;
/// the <c>-1</c> sentinel fills the available width to preserve MAUI's
/// <c>MatchParent</c> contract.
/// </para>
/// </remarks>
public partial class FlyoutViewHandler : ViewHandler<IFlyoutView, DrawerLayout>, IFlyoutViewHandler
{
    const string LogTag = "ComposeFlyout";

    // MAUI applies this mapper before the normal mapper so Flyout and Detail
    // exist before dependent mappings such as Toolbar run.
    static readonly IPropertyMapper<IFlyoutView, FlyoutViewHandler> FlyoutLayoutMapper =
        new PropertyMapper<IFlyoutView, FlyoutViewHandler>
        {
            [nameof(IFlyoutView.Flyout)]          = MapFlyout,
            [nameof(IFlyoutView.Detail)]          = MapDetail,
        };

    /// <summary>
    /// Property mapper for effective layout behavior, presentation, adaptive
    /// width, gestures, toolbar integration, and inherited view properties.
    /// </summary>
    public static IPropertyMapper<IFlyoutView, FlyoutViewHandler> Mapper =
        new PropertyMapper<IFlyoutView, FlyoutViewHandler>(
            ViewHandler.ViewMapper,
            FlyoutLayoutMapper)
        {
            [nameof(IFlyoutView.IsPresented)]     = MapIsPresented,
            [nameof(IFlyoutView.FlyoutBehavior)]  = MapFlyoutBehavior,
            [nameof(IFlyoutView.FlyoutWidth)]     = MapFlyoutWidth,
            [nameof(IFlyoutView.IsGestureEnabled)] = MapIsGestureEnabled,
            ["Toolbar"]                           = MapToolbar,
        };

    /// <summary>Command mapper inherited from MAUI's base view handler.</summary>
    public static CommandMapper<IFlyoutView, FlyoutViewHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    readonly MutableState<int> _layoutVersion = new(0);
    DrawerStateHolder _drawerState = new();
    ComposeView? _composeView;
    ThemeManager? _theme;
    bool _publishingPresentation;

    /// <summary>Construct a handler with the default mappers.</summary>
    public FlyoutViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom property and command mappers.</summary>
    public FlyoutViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    IFlyoutView IFlyoutViewHandler.VirtualView =>
        VirtualView ?? throw new InvalidOperationException(
            "VirtualView not set on FlyoutViewHandler.");

    /// <inheritdoc/>
    AView IFlyoutViewHandler.PlatformView =>
        PlatformView ?? throw new InvalidOperationException(
            "PlatformView not set on FlyoutViewHandler.");

    /// <inheritdoc/>
    protected override DrawerLayout CreatePlatformView()
    {
        var context = Context
            ?? throw new InvalidOperationException("Context not set on FlyoutViewHandler.");
        var mauiContext = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on FlyoutViewHandler.");
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on FlyoutViewHandler.");

        _theme = mauiContext.Services.GetService<ThemeManager>();
        _drawerState = CreateDrawerState(virtualView.IsPresented);

        var compose = _composeView = new ComposeView(context)
        {
            LayoutParameters = new AViewGroup.LayoutParams(
                AViewGroup.LayoutParams.MatchParent,
                AViewGroup.LayoutParams.MatchParent),
        };
        compose.SetContent(Build);

        var root = new DrawerLayout(context)
        {
            LayoutParameters = new AViewGroup.LayoutParams(
                AViewGroup.LayoutParams.MatchParent,
                AViewGroup.LayoutParams.MatchParent),
        };
        root.AddView(compose, new DrawerLayout.LayoutParams(
            DrawerLayout.LayoutParams.MatchParent,
            DrawerLayout.LayoutParams.MatchParent));
        return root;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(DrawerLayout platformView)
    {
        _composeView?.DisposeComposition();
        _composeView = null;
        _theme = null;
        base.DisconnectHandler(platformView);
    }

    ComposableNode Build(IComposer composer)
    {
        _ = _layoutVersion.Value;

        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on FlyoutViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on FlyoutViewHandler.");

        var content = virtualView.FlyoutBehavior switch
        {
            FlyoutBehavior.Disabled => BuildPageHost(virtualView.Detail, context),
            FlyoutBehavior.Locked   => BuildPermanentDrawer(virtualView, context),
            _                       => BuildModalDrawer(virtualView, context),
        };

        if (_theme is null)
            return content;

        var themed = new MaterialTheme
        {
            Dark = _theme.IsDark.Value,
            UseDynamicColor = false,
        };
        themed.Add(content);
        return themed;
    }

    ComposableNode BuildModalDrawer(IFlyoutView virtualView, IMauiContext context)
    {
        var drawer = new ModalNavigationDrawer(
            drawerState: _drawerState,
            gesturesEnabled: virtualView.IsGestureEnabled)
        {
            Drawer = BuildModalSheet(virtualView, context),
            Content = BuildObservedDetail(virtualView.Detail, context),
        };
        return drawer;
    }

    ComposableNode BuildPermanentDrawer(IFlyoutView virtualView, IMauiContext context) =>
        new PermanentNavigationDrawer
        {
            Drawer = BuildPermanentSheet(virtualView, context),
            Content = BuildPageHost(virtualView.Detail, context),
        };

    ComposableNode BuildModalSheet(IFlyoutView virtualView, IMauiContext context)
    {
        if (virtualView.FlyoutWidth < 0)
            return BuildFullWidthSheet(virtualView.Flyout, context);

        var sheet = new ModalDrawerSheet
        {
            Modifier = DrawerSheetModifier(virtualView.FlyoutWidth),
        };
        sheet.Add(BuildPageHost(virtualView.Flyout, context));
        return sheet;
    }

    ComposableNode BuildPermanentSheet(IFlyoutView virtualView, IMauiContext context)
    {
        if (virtualView.FlyoutWidth < 0)
            return BuildFullWidthSheet(virtualView.Flyout, context);

        var sheet = new PermanentDrawerSheet
        {
            Modifier = DrawerSheetModifier(virtualView.FlyoutWidth),
        };
        sheet.Add(BuildPageHost(virtualView.Flyout, context));
        return sheet;
    }

    ComposableNode BuildObservedDetail(IView detail, IMauiContext context)
    {
        var box = new Box
        {
            Modifier = Modifier.FillMaxSize(),
        };
        box.Add(BuildPageHost(detail, context));
        box.Add(new FlyoutPresentationObserver(_drawerState, PublishSettledPresentation));
        return box;
    }

    static Modifier DrawerSheetModifier(double width)
    {
        var modifier = Modifier.FillMaxHeight();
        return width switch
        {
            > 0 => modifier.Width(new Dp((float)width)),
            _   => modifier,
        };
    }

    static ComposableNode BuildFullWidthSheet(IView page, IMauiContext context)
    {
        var sheet = new Surface
        {
            Modifier = Modifier.FillMaxSize(),
        };
        sheet.Add(BuildPageHost(page, context));
        return sheet;
    }

    static ComposableNode BuildPageHost(IView? page, IMauiContext context) =>
        new AndroidView(
            factory: androidContext => new FlyoutPageHost(androidContext)
            {
                LayoutParameters = new AViewGroup.LayoutParams(
                    AViewGroup.LayoutParams.MatchParent,
                    AViewGroup.LayoutParams.MatchParent),
            },
            update: host =>
            {
                ((FlyoutPageHost)host).UpdatePage(page, context);
            })
        {
            Modifier = Modifier.FillMaxSize(),
        };

    void PublishSettledPresentation(bool isPresented)
    {
        var virtualView = VirtualView;
        if (virtualView is null ||
            virtualView.FlyoutBehavior != FlyoutBehavior.Flyout ||
            virtualView.IsPresented == isPresented)
        {
            return;
        }

        _publishingPresentation = true;
        try
        {
            virtualView.IsPresented = isPresented;
        }
        finally
        {
            _publishingPresentation = false;
        }
    }

    void ApplyRequestedPresentation(bool isPresented)
    {
        if (_drawerState.Jvm is null)
        {
            if (_drawerState.IsOpen != isPresented)
            {
                _drawerState = CreateDrawerState(isPresented);
                _layoutVersion.Value++;
            }
            return;
        }

        var target = _drawerState.TargetValue;
#if DEBUG
        Log.Debug(
            LogTag,
            $"Presentation request: requested={isPresented}, current={_drawerState.CurrentValue}, target={target}.");
#endif
        if ((isPresented && target == DrawerValue.Open) ||
            (!isPresented && target == DrawerValue.Closed))
        {
            return;
        }

        _ = ApplyRequestedPresentationAsync(isPresented);
    }

    async Task ApplyRequestedPresentationAsync(bool isPresented)
    {
        try
        {
            if (isPresented)
                await _drawerState.OpenAsync();
            else
                await _drawerState.CloseAsync();
        }
        catch (OperationCanceledException)
        {
            // A caller token was cancelled. The settled observer publishes
            // the actual result.
        }
        catch (Java.Util.Concurrent.CancellationException)
        {
            // Kotlin's MutatorMutex resumes a preempted animateTo with its
            // native CancellationException, which SuspendBridge preserves as
            // the Java Throwable. Competing requests and user drags are
            // expected ownership changes; non-cancellation failures below
            // remain visible.
        }
        catch (Exception ex)
        {
            Log.Error(LogTag, $"Drawer presentation transition failed: {ex}");
        }
    }

    static DrawerStateHolder CreateDrawerState(bool isPresented) =>
        new(isPresented ? DrawerValue.Open : DrawerValue.Closed);

    static void InvalidateLayout(FlyoutViewHandler handler) =>
        handler._layoutVersion.Value++;

    /// <summary>Recompose when the flyout child page changes.</summary>
    public static void MapFlyout(FlyoutViewHandler handler, IFlyoutView view) =>
        InvalidateLayout(handler);

    /// <summary>Recompose when the detail child page changes.</summary>
    public static void MapDetail(FlyoutViewHandler handler, IFlyoutView view) =>
        InvalidateLayout(handler);

    /// <summary>
    /// Animate the Material 3 drawer toward an externally requested
    /// presentation state. Settled gesture changes flow back through the
    /// observer without recursively starting another transition.
    /// </summary>
    public static void MapIsPresented(FlyoutViewHandler handler, IFlyoutView view)
    {
        if (handler._publishingPresentation ||
            view.FlyoutBehavior != FlyoutBehavior.Flyout)
        {
            return;
        }

        handler.ApplyRequestedPresentation(view.IsPresented);
    }

    /// <summary>Switch between detail-only, modal, and permanent layouts.</summary>
    public static void MapFlyoutBehavior(FlyoutViewHandler handler, IFlyoutView view)
    {
        if (view.FlyoutBehavior == FlyoutBehavior.Flyout)
            handler.ApplyRequestedPresentation(view.IsPresented);
        InvalidateDetailChrome(view);
        InvalidateLayout(handler);
    }

    /// <summary>Apply MAUI's adaptive flyout width to the drawer sheet.</summary>
    public static void MapFlyoutWidth(FlyoutViewHandler handler, IFlyoutView view) =>
        InvalidateLayout(handler);

    /// <summary>Enable or disable Material 3's edge-swipe gesture.</summary>
    public static void MapIsGestureEnabled(FlyoutViewHandler handler, IFlyoutView view) =>
        InvalidateLayout(handler);

    /// <summary>
    /// Preserve MAUI's toolbar propagation and refresh a Compose-backed
    /// detail <see cref="Microsoft.Maui.Controls.NavigationPage"/> so its
    /// root top bar shows or removes the drawer button as the effective
    /// adaptive behavior changes. The Compose navigation bar owns the drawer
    /// affordance; binding a native <c>ToolbarHandler</c> to the compatibility
    /// <see cref="DrawerLayout"/> would incorrectly compete with Material 3.
    /// </summary>
    public static void MapToolbar(FlyoutViewHandler handler, IFlyoutView view)
    {
        ViewHandler.MapToolbar(handler, view);
        InvalidateDetailChrome(view);
    }

    static void InvalidateDetailChrome(IFlyoutView view)
    {
        if (view.Detail?.Handler is NavigationPageHandler navigation)
            navigation.InvalidateParentChrome();
    }
}
