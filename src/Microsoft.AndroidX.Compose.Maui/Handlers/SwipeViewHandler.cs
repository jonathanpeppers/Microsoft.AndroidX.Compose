using Android.Animation;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ComposeColor = AndroidX.Compose.Color;
using ComposeLayout = AndroidX.Compose.Layout;
using MauiCollectionView = Microsoft.Maui.Controls.CollectionView;
using MauiSwipeDirection = Microsoft.Maui.SwipeDirection;
using MauiSwipeItem = Microsoft.Maui.ISwipeItem;
using MauiSwipeView = Microsoft.Maui.Controls.SwipeView;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Compose-backed handler for MAUI <see cref="MauiSwipeView"/>.
/// Supports four action directions, reveal and drag transitions,
/// executable and reveal item modes, programmatic open/close, and
/// MAUI swipe lifecycle events.
/// </summary>
/// <remarks>
/// Horizontal and vertical Compose draggable modifiers arbitrate at
/// axis-specific touch slop. A horizontal action row nested in a
/// vertical Compose <see cref="MauiCollectionView"/> therefore leaves
/// vertical scrolling to the list. Parent list scroll events retain
/// MAUI's stock behavior of closing an open row.
/// </remarks>
public partial class SwipeViewHandler : ComposeElementHandler<ISwipeView>
{
    const int AnimationDurationMilliseconds = 200;
    const float DefaultMenuItemWidthDp = 100f;

    /// <summary>Property mapper for SwipeView content, items, mode, and state.</summary>
    public static IPropertyMapper<ISwipeView, SwipeViewHandler> Mapper =
        new PropertyMapper<ISwipeView, SwipeViewHandler>(ViewHandler.ViewMapper)
        {
            ["Content"]             = MapContent,
            ["SwipeTransitionMode"] = MapSwipeTransitionMode,
            ["LeftItems"]           = MapLeftItems,
            ["TopItems"]            = MapTopItems,
            ["RightItems"]          = MapRightItems,
            ["BottomItems"]         = MapBottomItems,
            ["Threshold"]           = MapThreshold,
            ["IsEnabled"]           = MapIsEnabled,
            ["Background"]          = MapBackground,
        };

    /// <summary>Command mapper for programmatic open and close requests.</summary>
    public static CommandMapper<ISwipeView, SwipeViewHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            ["RequestOpen"]  = MapRequestOpen,
            ["RequestClose"] = MapRequestClose,
        };

    readonly MutableState<int> _contentVersion = new(0);
    readonly MutableState<int> _itemsVersion = new(0);
    readonly MutableState<int> _transitionMode = new((int)SwipeTransitionMode.Reveal);
    readonly MutableState<bool> _enabled = new(true);
    readonly MutableState<float> _offsetX = new(0f);
    readonly MutableState<float> _offsetY = new(0f);
    readonly MutableState<int> _activeDirectionState = new(0);
    readonly MutableState<long?> _background = new((long?)null);
    readonly DraggableState _horizontalDrag;
    readonly DraggableState _verticalDrag;
    readonly HashSet<Element> _observedItems = [];
    readonly Action _closeForParentScroll;
    readonly SwipeDragLifecycle _dragLifecycle = new();

    ValueAnimator? _animator;
    int _animationGeneration;
    MauiSwipeDirection? _activeDirection;
    (OpenSwipeItem Item, bool Animated)? _pendingOpen;
    bool _isSettledOpen;
    float _density = 1f;
    float _leftExtent;
    float _rightExtent;
    float _topExtent;
    float _bottomExtent;
    CollectionViewportObserver? _viewportObserver;

    /// <summary>Construct a handler with the default mappers.</summary>
    public SwipeViewHandler() : this(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SwipeViewHandler(
        IPropertyMapper? mapper,
        CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
        _horizontalDrag = new DraggableState(delta => OnDrag(horizontal: true, delta));
        _verticalDrag = new DraggableState(delta => OnDrag(horizontal: false, delta));
        // A row may leave composition immediately after the viewport
        // changes. Snap synchronously so offscreen animation lifecycle
        // cannot preserve an open offset when the cached row returns.
        _closeForParentScroll = () => Close(animated: false);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        _ = _contentVersion.Value;
        _ = _itemsVersion.Value;
        SubscribeToViewProperties();

        var view = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on SwipeViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on SwipeViewHandler.");
        BindViewportObserver(ViewportObserverBinding.Resolve(
            CollectionViewportContext.Current,
            _viewportObserver));
        int activeDirection = _activeDirectionState.Value;

        var left = BuildPanel(
            view.LeftItems,
            MauiSwipeDirection.Right,
            activeDirection == (int)MauiSwipeDirection.Right,
            composer,
            context);
        var right = BuildPanel(
            view.RightItems,
            MauiSwipeDirection.Left,
            activeDirection == (int)MauiSwipeDirection.Left,
            composer,
            context);
        var top = BuildPanel(
            view.TopItems,
            MauiSwipeDirection.Down,
            activeDirection == (int)MauiSwipeDirection.Down,
            composer,
            context);
        var bottom = BuildPanel(
            view.BottomItems,
            MauiSwipeDirection.Up,
            activeDirection == (int)MauiSwipeDirection.Up,
            composer,
            context);
        var content = view.PresentedContent is { } presented
            ? ComposeWalker.Render(presented, composer, context)
            : new Box();

        var layout = new ComposeLayout((scope, measurables, constraints) =>
            MeasureSwipeLayout(
                scope,
                measurables,
                constraints,
                activeDirection));
        layout.Add(left);
        layout.Add(right);
        layout.Add(top);
        layout.Add(bottom);
        layout.Add(content);

        bool enabled = _enabled.Value;
        Modifier modifier = Modifier.Companion.ApplyViewProperties(view);
        if (_background.Value is long background)
            modifier = modifier.Background(ComposeColor.FromPacked(background));
        modifier = modifier
            .ApplyGestures(view, context)
            .ApplySemantics(view)
            .ClipToBounds()
            .DraggableWithStop(
                _horizontalDrag,
                Orientation.Horizontal,
                enabled && HasHorizontalItems(view),
                () => OnDragStopped(horizontal: true))
            .DraggableWithStop(
                _verticalDrag,
                Orientation.Vertical,
                enabled && HasVerticalItems(view),
                () => OnDragStopped(horizontal: false));
        layout.Modifier = modifier;
        return layout;
    }

    ComposableNode BuildPanel(
        ISwipeItems items,
        MauiSwipeDirection direction,
        bool isActive,
        IComposer composer,
        IMauiContext context)
    {
        bool horizontal = IsHorizontal(direction);
        var visibleItems = items.Where(IsVisible).ToArray();
        if (SwipePanelOrder.ShouldReverse((int)direction))
            Array.Reverse(visibleItems);
        var row = new Row(
            horizontalArrangement: Arrangement.Start,
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            Modifier = horizontal
                ? Modifier.Companion.FillMaxHeight()
                : Modifier.Companion.FillMaxSize(),
        };

        foreach (var item in visibleItems)
        {
            ComposableNode itemNode;
            if (item is ISwipeItemView itemView)
            {
                itemNode = ComposeWalker.Render(itemView, composer, context);
            }
            else
            {
                var handler = item.Handler;
                if (handler is null)
                {
                    _ = item.ToHandler(context);
                    handler = item.Handler;
                }

                itemNode = handler is ISwipeMenuItemNodeProvider provider
                    ? provider.BuildSwipeItemNode()
                    : throw new NotSupportedException(
                        $"Swipe item '{item.GetType().FullName}' must use " +
                        $"{nameof(SwipeItemViewHandler)} or {nameof(SwipeItemMenuItemHandler)}.");
            }

            Modifier itemModifier;
            if (horizontal)
            {
                itemModifier = item is ISwipeItemView
                    ? Modifier.Companion
                        .WidthIn(min: new Dp(DefaultMenuItemWidthDp))
                        .FillMaxHeight()
                    : Modifier.Companion
                        .Width(new Dp(DefaultMenuItemWidthDp))
                        .FillMaxHeight();
            }
            else
            {
                itemModifier = Modifier.Companion
                    .Weight(1f)
                    .FillMaxHeight();
            }

            var clickable = new Box
            {
                Modifier = isActive && IsEnabled(item)
                    ? itemModifier.Clickable(() => InvokeItem(item, items))
                    : itemModifier,
            };
            clickable.Add(itemNode);
            row.Add(clickable);
        }

        return row;
    }

    MeasureResult MeasureSwipeLayout(
        MeasureScope scope,
        IReadOnlyList<Measurable> measurables,
        Constraints constraints,
        int activeDirection)
    {
        if (measurables.Count != 5)
            throw new InvalidOperationException(
                $"SwipeView layout expected 5 children but received {measurables.Count}.");

        var content = measurables[4].Measure(constraints);
        int width = content.Width;
        int height = content.Height;
        var horizontalConstraints = Constraints.Create(0, width, height, height);
        var verticalConstraints = Constraints.Create(width, width, 0, height);
        var left = measurables[0].Measure(horizontalConstraints);
        var right = measurables[1].Measure(horizontalConstraints);
        var top = measurables[2].Measure(verticalConstraints);
        var bottom = measurables[3].Measure(verticalConstraints);

        _density = scope.Density;
        UpdateExtents(width, height, left.Width, right.Width, top.Height, bottom.Height);
        ApplyPendingOpen();

        float offsetX = _offsetX.Value;
        float offsetY = _offsetY.Value;
        var transition = (SwipeTransitionMode)_transitionMode.Value;

        return scope.Layout(width, height, placement =>
        {
            int leftX = transition == SwipeTransitionMode.Reveal
                ? 0
                : (int)Math.Round(-_leftExtent + Math.Max(0f, offsetX));
            int rightX = transition == SwipeTransitionMode.Reveal
                ? width - right.Width
                : (int)Math.Round(width + Math.Min(0f, offsetX));
            int topY = transition == SwipeTransitionMode.Reveal
                ? 0
                : (int)Math.Round(-_topExtent + Math.Max(0f, offsetY));
            int bottomY = transition == SwipeTransitionMode.Reveal
                ? height - bottom.Height
                : (int)Math.Round(height + Math.Min(0f, offsetY));

            switch (SwipePanelPlacement.ActivePanelIndex(activeDirection))
            {
                case SwipePanelPlacement.LeftItems:
                    placement.Place(left, leftX, 0);
                    break;
                case SwipePanelPlacement.RightItems:
                    placement.Place(right, rightX, 0);
                    break;
                case SwipePanelPlacement.TopItems:
                    placement.Place(top, 0, topY);
                    break;
                case SwipePanelPlacement.BottomItems:
                    placement.Place(bottom, 0, bottomY);
                    break;
            }
            placement.Place(
                content,
                (int)Math.Round(offsetX),
                (int)Math.Round(offsetY),
                zIndex: 1f);
        });
    }

    void UpdateExtents(
        int contentWidth,
        int contentHeight,
        int leftWidth,
        int rightWidth,
        int topHeight,
        int bottomHeight)
    {
        var view = VirtualView;
        if (view is null)
            return;

        _leftExtent = Extent(view.LeftItems, leftWidth, contentWidth, horizontal: true);
        _rightExtent = Extent(view.RightItems, rightWidth, contentWidth, horizontal: true);
        _topExtent = Extent(view.TopItems, topHeight, contentHeight, horizontal: false);
        _bottomExtent = Extent(view.BottomItems, bottomHeight, contentHeight, horizontal: false);
        ReconcileOpenExtent(view);
    }

    void ReconcileOpenExtent(ISwipeView view)
    {
        if (_activeDirection is not { } direction)
            return;
        float extent = ExtentFor(direction);
        var action = SwipeExtentReconciliationPolicy.Resolve(
            view.IsOpen,
            _isSettledOpen,
            _dragLifecycle.IsDragging,
            _animator is not null,
            extent);
        if (action == SwipeExtentReconciliation.Close)
        {
            Close(animated: false);
            return;
        }
        if (action != SwipeExtentReconciliation.SnapOpen)
            return;
        if (IsHorizontal(direction))
        {
            _offsetX.Value = SignedExtent(direction);
            _offsetY.Value = 0f;
        }
        else
        {
            _offsetX.Value = 0f;
            _offsetY.Value = SignedExtent(direction);
        }
    }

    static float Extent(
        ISwipeItems items,
        int measured,
        int contentSize,
        bool horizontal)
    {
        if (!items.Any(IsVisible))
            return 0f;
        if (items.Mode == SwipeMode.Execute &&
            !items.Any(item => item is ISwipeItemView))
        {
            return contentSize * 0.8f;
        }
        if (!horizontal && !items.Any(item => item is ISwipeItemView))
            return contentSize;
        return Math.Min(measured, contentSize);
    }

    void OnDrag(bool horizontal, float delta)
    {
        var view = VirtualView;
        if (view is null || !_enabled.Value || delta == 0f)
            return;

        CancelAnimation();
        var current = horizontal ? _offsetX.Value : _offsetY.Value;
        if (_activeDirection is null)
        {
            var candidate = horizontal
                ? delta > 0 ? MauiSwipeDirection.Right : MauiSwipeDirection.Left
                : delta > 0 ? MauiSwipeDirection.Down : MauiSwipeDirection.Up;
            if (!HasVisibleItems(ItemsFor(view, candidate)))
                return;
            _activeDirection = candidate;
            _activeDirectionState.Value = (int)candidate;
        }

        var direction = _activeDirection.Value;
        if (horizontal != IsHorizontal(direction))
            return;

        if (_dragLifecycle.Begin())
        {
            _isSettledOpen = false;
            view.SwipeStarted(new SwipeViewSwipeStarted(direction));
        }

        float extent = ExtentFor(direction);
        float next = direction is MauiSwipeDirection.Right or MauiSwipeDirection.Down
            ? Math.Clamp(current + delta, 0f, extent)
            : Math.Clamp(current + delta, -extent, 0f);

        if (horizontal)
            _offsetX.Value = next;
        else
            _offsetY.Value = next;

        view.IsOpen = Math.Abs(next) > float.Epsilon;
        view.SwipeChanging(new SwipeViewSwipeChanging(direction, next / _density));
    }

    void OnDragStopped(bool horizontal)
    {
        var view = VirtualView;
        if (view is null || !_dragLifecycle.End())
            return;
        if (_activeDirection is not { } direction ||
            horizontal != IsHorizontal(direction))
        {
            return;
        }

        float current = horizontal ? _offsetX.Value : _offsetY.Value;
        float extent = ExtentFor(direction);
        float openDistance = SwipeThresholdPolicy.ResolveOpenDistancePixels(
            view.Threshold,
            _density,
            extent);
        bool shouldOpen = extent > 0f &&
            Math.Abs(current) >= openDistance;
        view.SwipeEnded(new SwipeViewSwipeEnded(direction, shouldOpen));

        var items = ItemsFor(view, direction);
        if (shouldOpen && items.Mode == SwipeMode.Execute)
        {
            foreach (var item in items.Where(IsVisible))
                InvokeEnabledItem(item);
            shouldOpen = SwipeInvocationPolicy.ShouldRemainOpen(
                items.Mode,
                items.SwipeBehaviorOnInvoked);
        }

        SetOpen(direction, shouldOpen, animated: true);
    }

    void InvokeItem(MauiSwipeItem item, ISwipeItems items)
    {
        if (!InvokeEnabledItem(item))
            return;
        if (!SwipeInvocationPolicy.ShouldRemainOpen(
            items.Mode,
            items.SwipeBehaviorOnInvoked))
        {
            Close(animated: true);
        }
    }

    static bool InvokeEnabledItem(MauiSwipeItem item)
    {
        bool enabled = IsEnabled(item);
        if (enabled)
            item.OnInvoked();
        return enabled;
    }

    static bool IsEnabled(MauiSwipeItem item) =>
        item switch
        {
            ISwipeItemMenuItem menuItem => menuItem.IsEnabled,
            ISwipeItemView itemView => itemView.IsEnabled,
            _ => true,
        };

    void SetOpen(MauiSwipeDirection direction, bool open, bool animated)
    {
        float target = open ? SignedExtent(direction) : 0f;
        _isSettledOpen = false;
        var view = VirtualView;
        if (view is not null)
            view.IsOpen = open;

        AnimateTo(
            IsHorizontal(direction) ? target : 0f,
            IsHorizontal(direction) ? 0f : target,
            animated,
            () =>
            {
                _isSettledOpen = open;
                if (!open)
                {
                    _activeDirection = null;
                    _activeDirectionState.Value = 0;
                }
                else if (VirtualView is { } currentView)
                {
                    ReconcileOpenExtent(currentView);
                }
            });
    }

    void Open(OpenSwipeItem item, bool animated)
    {
        var view = VirtualView;
        if (view is null)
            return;
        var direction = item switch
        {
            OpenSwipeItem.LeftItems => MauiSwipeDirection.Right,
            OpenSwipeItem.RightItems => MauiSwipeDirection.Left,
            OpenSwipeItem.TopItems => MauiSwipeDirection.Down,
            OpenSwipeItem.BottomItems => MauiSwipeDirection.Up,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, "Unknown swipe item side."),
        };
        if (!HasVisibleItems(ItemsFor(view, direction)))
        {
            Close(animated: false);
            return;
        }

        if (_activeDirection is { } previous && previous != direction)
        {
            CancelAnimation();
            _isSettledOpen = false;
            _offsetX.Value = 0f;
            _offsetY.Value = 0f;
            view.IsOpen = false;
        }
        _activeDirection = direction;
        _activeDirectionState.Value = (int)direction;
        if (ExtentFor(direction) <= 0f)
        {
            _pendingOpen = (item, animated);
            return;
        }
        SetOpen(direction, open: true, animated);
    }

    void ApplyPendingOpen()
    {
        if (_pendingOpen is not { } pending)
            return;
        _pendingOpen = null;
        Open(pending.Item, pending.Animated);
    }

    void Close(bool animated)
    {
        _pendingOpen = null;
        _dragLifecycle.Cancel();
        if (_activeDirection is { } direction)
            SetOpen(direction, open: false, animated);
        else
        {
            _isSettledOpen = false;
            _offsetX.Value = 0f;
            _offsetY.Value = 0f;
            if (VirtualView is { } view)
                view.IsOpen = false;
            _activeDirectionState.Value = 0;
        }
    }

    void AnimateTo(float x, float y, bool animated, Action completed)
    {
        CancelAnimation();
        if (!animated)
        {
            _offsetX.Value = x;
            _offsetY.Value = y;
            completed();
            return;
        }

        bool horizontal = Math.Abs(x - _offsetX.Value) >= Math.Abs(y - _offsetY.Value);
        float start = horizontal ? _offsetX.Value : _offsetY.Value;
        float target = horizontal ? x : y;
        if (Math.Abs(target - start) <= float.Epsilon)
        {
            completed();
            return;
        }

        int generation = ++_animationGeneration;
        var animator = ValueAnimator.OfFloat(start, target)
            ?? throw new InvalidOperationException("ValueAnimator could not be created.");
        animator.SetDuration(AnimationDurationMilliseconds);
        animator.Update += (_, args) =>
        {
            if (generation != _animationGeneration ||
                args.Animation.AnimatedValue is not Java.Lang.Float value)
            {
                return;
            }
            if (horizontal)
                _offsetX.Value = value.FloatValue();
            else
                _offsetY.Value = value.FloatValue();
        };
        animator.AnimationEnd += (_, _) =>
        {
            if (generation != _animationGeneration)
                return;
            if (horizontal)
                _offsetX.Value = target;
            else
                _offsetY.Value = target;
            if (ReferenceEquals(_animator, animator))
                _animator = null;
            animator.Dispose();
            completed();
        };
        _animator = animator;
        animator.Start();
    }

    void CancelAnimation()
    {
        _animationGeneration++;
        var animator = _animator;
        _animator = null;
        if (animator is null)
            return;
        animator.Cancel();
        animator.Dispose();
    }

    float SignedExtent(MauiSwipeDirection direction) =>
        direction is MauiSwipeDirection.Right or MauiSwipeDirection.Down
            ? ExtentFor(direction)
            : -ExtentFor(direction);

    float ExtentFor(MauiSwipeDirection direction) => direction switch
    {
        MauiSwipeDirection.Right => _leftExtent,
        MauiSwipeDirection.Left => _rightExtent,
        MauiSwipeDirection.Down => _topExtent,
        MauiSwipeDirection.Up => _bottomExtent,
        _ => 0f,
    };

    static ISwipeItems ItemsFor(ISwipeView view, MauiSwipeDirection direction) =>
        direction switch
        {
            MauiSwipeDirection.Right => view.LeftItems,
            MauiSwipeDirection.Left => view.RightItems,
            MauiSwipeDirection.Down => view.TopItems,
            MauiSwipeDirection.Up => view.BottomItems,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown swipe direction."),
        };

    static bool IsHorizontal(MauiSwipeDirection direction) =>
        direction is MauiSwipeDirection.Left or MauiSwipeDirection.Right;

    static bool IsVisible(MauiSwipeItem item) => item switch
    {
        ISwipeItemView itemView => itemView.Visibility == Visibility.Visible,
        ISwipeItemMenuItem menuItem => menuItem.Visibility == Visibility.Visible,
        _ => true,
    };

    static bool HasVisibleItems(ISwipeItems items) => items.Any(IsVisible);

    static bool HasHorizontalItems(ISwipeView view) =>
        HasVisibleItems(view.LeftItems) || HasVisibleItems(view.RightItems);

    static bool HasVerticalItems(ISwipeView view) =>
        HasVisibleItems(view.TopItems) || HasVisibleItems(view.BottomItems);

    void RefreshItemSubscriptions(ISwipeView view)
    {
        var current = new HashSet<Element>();
        Add(view.LeftItems);
        Add(view.RightItems);
        Add(view.TopItems);
        Add(view.BottomItems);

        foreach (var removed in _observedItems.Except(current).ToArray())
        {
            removed.PropertyChanged -= OnSwipeItemPropertyChanged;
            _observedItems.Remove(removed);
        }
        foreach (var added in current.Except(_observedItems))
        {
            added.PropertyChanged += OnSwipeItemPropertyChanged;
            _observedItems.Add(added);
        }
        return;

        void Add(ISwipeItems items)
        {
            foreach (var item in items)
            {
                if (item is Element element)
                    current.Add(element);
            }
        }
    }

    void OnSwipeItemPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e) =>
        _itemsVersion.Value++;

    void ClearItemSubscriptions()
    {
        foreach (var item in _observedItems)
            item.PropertyChanged -= OnSwipeItemPropertyChanged;
        _observedItems.Clear();
    }

    void BindViewportObserver(CollectionViewportObserver? observer)
    {
        if (ReferenceEquals(_viewportObserver, observer))
            return;
        _viewportObserver?.Unregister(_closeForParentScroll);
        _viewportObserver = observer;
        _viewportObserver?.Register(_closeForParentScroll);
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        CancelAnimation();
        ClearItemSubscriptions();
        BindViewportObserver(null);
        base.DisconnectHandler(platformView);
    }

    /// <summary>Recompose when content changes.</summary>
    public static void MapContent(SwipeViewHandler handler, ISwipeView _) =>
        handler._contentVersion.Value++;

    /// <summary>Recompose when left items change.</summary>
    public static void MapLeftItems(SwipeViewHandler handler, ISwipeView view) =>
        handler.MapItems(view);

    /// <summary>Recompose when top items change.</summary>
    public static void MapTopItems(SwipeViewHandler handler, ISwipeView view) =>
        handler.MapItems(view);

    /// <summary>Recompose when right items change.</summary>
    public static void MapRightItems(SwipeViewHandler handler, ISwipeView view) =>
        handler.MapItems(view);

    /// <summary>Recompose when bottom items change.</summary>
    public static void MapBottomItems(SwipeViewHandler handler, ISwipeView view) =>
        handler.MapItems(view);

    void MapItems(ISwipeView view)
    {
        RefreshItemSubscriptions(view);
        _itemsVersion.Value++;
    }

    /// <summary>Rebuild item sizing when the swipe threshold changes.</summary>
    public static void MapThreshold(SwipeViewHandler handler, ISwipeView _)
    {
        if (handler.VirtualView?.IsOpen == true &&
            handler._activeDirection is { } direction)
        {
            OpenSwipeItem? item = direction switch
            {
                MauiSwipeDirection.Right => OpenSwipeItem.LeftItems,
                MauiSwipeDirection.Left => OpenSwipeItem.RightItems,
                MauiSwipeDirection.Down => OpenSwipeItem.TopItems,
                MauiSwipeDirection.Up => OpenSwipeItem.BottomItems,
                _ => null,
            };
            handler._pendingOpen = item is { } pending
                ? (pending, false)
                : null;
            handler.CancelAnimation();
            handler._isSettledOpen = false;
            handler._offsetX.Value = 0f;
            handler._offsetY.Value = 0f;
        }
        handler._itemsVersion.Value++;
    }

    /// <summary>Map the platform transition mode.</summary>
    public static void MapSwipeTransitionMode(
        SwipeViewHandler handler,
        ISwipeView view) =>
        handler._transitionMode.Value = (int)view.SwipeTransitionMode;

    /// <summary>Map enabled state to both axis gesture modifiers.</summary>
    public static void MapIsEnabled(SwipeViewHandler handler, ISwipeView view) =>
        handler._enabled.Value = view.IsEnabled;

    /// <summary>
    /// Recompose the root modifier when background changes.
    /// </summary>
    public static void MapBackground(SwipeViewHandler handler, ISwipeView view)
    {
        handler._background.Value = view.Background is SolidPaint solid
            ? ColorMapping.ToPackedLong(solid.Color)
            : null;
    }

    /// <summary>Handle a programmatic open request.</summary>
    public static void MapRequestOpen(
        SwipeViewHandler handler,
        ISwipeView _,
        object? args)
    {
        if (args is SwipeViewOpenRequest request)
            handler.Open(request.OpenSwipeItem, request.Animated);
    }

    /// <summary>Handle a programmatic close request.</summary>
    public static void MapRequestClose(
        SwipeViewHandler handler,
        ISwipeView _,
        object? args)
    {
        if (args is SwipeViewCloseRequest request)
            handler.Close(request.Animated);
    }
}
