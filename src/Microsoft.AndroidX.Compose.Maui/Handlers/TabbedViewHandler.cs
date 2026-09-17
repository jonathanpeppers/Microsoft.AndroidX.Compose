using System.ComponentModel;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AViewGroup = Android.Views.ViewGroup;
using FrameLayout = Android.Widget.FrameLayout;
using MauiPage = Microsoft.Maui.Controls.Page;
using MauiTabbedPage = Microsoft.Maui.Controls.TabbedPage;
using MauiAndroidTabbedPage = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage;
using ComposeColor = AndroidX.Compose.Color;
using ComposeTab = AndroidX.Compose.Tab;
using ToolbarPlacement = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Compose-backed handler for MAUI <see cref="MauiTabbedPage"/>. Renders top
/// placement with Material 3 <see cref="TabRow"/> chrome, bottom placement
/// with <see cref="NavigationBar"/>, and page content through
/// <see cref="HorizontalPager{T}"/>.
/// </summary>
public partial class TabbedViewHandler : ViewHandler<ITabbedView, ComposeView>, ITabbedViewHandler
{
    /// <summary>Property mapper for tab items, selection, colors, and Android pager options.</summary>
    public static IPropertyMapper<ITabbedView, TabbedViewHandler> Mapper =
        new PropertyMapper<ITabbedView, TabbedViewHandler>(ViewHandler.ViewMapper)
        {
            ["ItemsSource"] = MapItemsSource,
            ["ItemTemplate"] = MapItemsSource,
            ["SelectedItem"] = MapCurrentPage,
            ["CurrentPage"] = MapCurrentPage,
            ["BarBackground"] = MapBarColors,
            ["BarBackgroundColor"] = MapBarColors,
            ["BarTextColor"] = MapBarColors,
            ["UnselectedTabColor"] = MapBarColors,
            ["SelectedTabColor"] = MapBarColors,
            ["IsSwipePagingEnabled"] = MapIsSwipePagingEnabled,
            ["OffscreenPageLimit"] = MapOffscreenPageLimit,
        };

    /// <summary>Command mapper inheriting the standard view commands.</summary>
    public static CommandMapper<ITabbedView, TabbedViewHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    readonly MutableState<int> _pagesVersion = new(0);
    readonly MutableState<int> _selectedIndex = new(0);
    readonly MutableState<bool> _userScrollEnabled = new(true);
    readonly MutableState<int> _offscreenPageLimit = new(3);
    readonly MutableState<long?> _barBackground = new((long?)null);
    readonly MutableState<long?> _barText = new((long?)null);
    readonly MutableState<long?> _selectedTab = new((long?)null);
    readonly MutableState<long?> _unselectedTab = new((long?)null);
    readonly Dictionary<MauiPage, int> _pageKeys = [];
    readonly Dictionary<MauiPage, TabbedPageIcon> _icons = [];

    IReadOnlyList<MauiPage> _pages = [];
    PagerState? _pagerState;
    ThemeManager? _theme;
    CancellationTokenSource? _selectionCancellation;
    int _nextPageKey;
    int _requestedIndex = -1;
    int _requestGracePasses;
    long _selectionRequestVersion;
    bool _requestIsAnimating;
    bool _updatingCurrentPageFromPager;

    /// <summary>Construct a handler with the default mappers.</summary>
    public TabbedViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public TabbedViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    ITabbedView ITabbedViewHandler.VirtualView =>
        VirtualView ?? throw new InvalidOperationException("VirtualView not set on TabbedViewHandler.");

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var context = Context
            ?? throw new InvalidOperationException("Context not set on TabbedViewHandler.");
        var mauiContext = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on TabbedViewHandler.");

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
        UnsubscribePages();
        foreach (var icon in _icons.Values)
            icon.Reset();
        _icons.Clear();
        _pageKeys.Clear();
        _pages = [];

        _selectionCancellation?.Cancel();
        _selectionCancellation?.Dispose();
        _selectionCancellation = null;
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    /// <summary>Refresh the page snapshot after children, templates, or tab titles change.</summary>
    public static void MapItemsSource(TabbedViewHandler handler, ITabbedView view)
    {
        if (view is not MauiTabbedPage tabbed)
            return;

        handler.ReplacePages(tabbed);
    }

    /// <summary>Synchronize MAUI's current page into the Compose pager.</summary>
    public static void MapCurrentPage(TabbedViewHandler handler, ITabbedView view)
    {
        if (view is not MauiTabbedPage tabbed)
            return;

        int index = tabbed.CurrentPage is null ? -1 : tabbed.Children.IndexOf(tabbed.CurrentPage);
        if (index < 0 && tabbed.Children.Count > 0)
            index = 0;
        if (index < 0)
            return;

        handler._selectedIndex.Value = index;
        if (handler._updatingCurrentPageFromPager)
            return;
        handler.MovePagerTo(index, MauiAndroidTabbedPage.GetIsSmoothScrollEnabled(tabbed));
    }

    /// <summary>Map Android's swipe-paging option to pager gesture input.</summary>
    public static void MapIsSwipePagingEnabled(TabbedViewHandler handler, ITabbedView view)
    {
        if (view is MauiTabbedPage tabbed)
            handler._userScrollEnabled.Value = MauiAndroidTabbedPage.GetIsSwipePagingEnabled(tabbed);
    }

    /// <summary>Map Android's obsolete offscreen-page limit to Compose's beyond-viewport count.</summary>
    public static void MapOffscreenPageLimit(TabbedViewHandler handler, ITabbedView view)
    {
        if (view is MauiTabbedPage tabbed)
        {
#pragma warning disable CS0618
            handler._offscreenPageLimit.Value = MauiAndroidTabbedPage.GetOffscreenPageLimit(tabbed);
#pragma warning restore CS0618
        }
    }

    /// <summary>Map MAUI tab-bar colors into the nested Material 3 theme.</summary>
    public static void MapBarColors(TabbedViewHandler handler, ITabbedView view)
    {
        if (view is not MauiTabbedPage tabbed)
            return;

        var background = tabbed.BarBackground is SolidColorBrush solid
            ? solid.Color
            : tabbed.BarBackgroundColor;
        handler._barBackground.Value = ColorMapping.ToPackedLong(background);
        handler._barText.Value = ColorMapping.ToPackedLong(tabbed.BarTextColor);
        handler._selectedTab.Value = ColorMapping.ToPackedLong(tabbed.SelectedTabColor);
        handler._unselectedTab.Value = ColorMapping.ToPackedLong(tabbed.UnselectedTabColor);
    }

    ComposableNode Build(IComposer composer)
    {
        _ = _pagesVersion.Value;
        var pages = _pages;
        if (pages.Count == 0)
            return WrapTheme(new Surface { Modifier = Modifier.FillMaxSize() }, composer);

        int requestedIndex = Math.Clamp(_selectedIndex.Value, 0, pages.Count - 1);
        var state = _pagerState ??= new PagerState(() => _pages.Count, requestedIndex);
        int pagerIndex = Math.Clamp(state.CurrentPage, 0, pages.Count - 1);
        bool swipeEnabled = _userScrollEnabled.Value;
        int offscreenLimit = _offscreenPageLimit.Value;

        composer.SideEffect(() => SynchronizeCurrentPage(pagerIndex, pages));

        var pager = new HorizontalPager<MauiPage>(pages, page => BuildPage(page))
        {
            State = state,
            Key = page => _pageKeys[page],
            UserScrollEnabled = swipeEnabled,
            BeyondViewportPageCount = offscreenLimit,
            Modifier = Modifier.FillMaxSize(),
        };

        var scaffold = new Scaffold { Body = pager };
        if (GetToolbarPlacement() == ToolbarPlacement.Bottom)
            scaffold.BottomBar = BuildBottomBar(pages, pagerIndex);
        else
            scaffold.TopBar = BuildTopBar(pages, pagerIndex);

        return WrapTheme(scaffold, composer);
    }

    ComposableNode BuildTopBar(IReadOnlyList<MauiPage> pages, int selectedIndex)
    {
        var row = new TabRow(selectedIndex);
        for (int i = 0; i < pages.Count; i++)
        {
            int index = i;
            var page = pages[i];
            var tab = new ComposeTab(selectedIndex == index, () => SelectPage(index), page.IsEnabled)
            {
                Text = new Text(page.Title ?? string.Empty)
                {
                    Color = ResolveTabTextColor(selectedIndex == index),
                },
            };
            if (_icons.TryGetValue(page, out var icon) && page.IconImageSource is not null)
                tab.Icon = icon.BuildNode();
            row.Add(tab);
        }
        return row;
    }

    ComposableNode BuildBottomBar(IReadOnlyList<MauiPage> pages, int selectedIndex)
    {
        var bar = new NavigationBar();
        for (int i = 0; i < pages.Count; i++)
        {
            int index = i;
            var page = pages[i];
            var item = new NavigationBarItem(
                selected: selectedIndex == index,
                onClick: () => SelectPage(index),
                enabled: page.IsEnabled)
            {
                Icon = _icons.TryGetValue(page, out var icon)
                    ? icon.BuildNode()
                    : new Box { Modifier = Modifier.Size(new Dp(24)) },
                Label = new Text(page.Title ?? string.Empty)
                {
                    Color = ResolveTabTextColor(selectedIndex == index),
                },
            };
            bar.Add(item);
        }
        return bar;
    }

    ComposableNode BuildPage(MauiPage page)
    {
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on TabbedViewHandler.");
        FrameLayout? frame = null;
        var host = new AndroidView(
            factory: androidContext =>
            {
                frame = new FrameLayout(androidContext)
                {
                    LayoutParameters = new AViewGroup.LayoutParams(
                        AViewGroup.LayoutParams.MatchParent,
                        AViewGroup.LayoutParams.MatchParent),
                };
                return frame;
            },
            update: view =>
            {
                var currentFrame = (FrameLayout)view;
                var platform = page.ToPlatform(context);
                if (currentFrame.ChildCount == 1 &&
                    ReferenceEquals(currentFrame.GetChildAt(0), platform))
                {
                    return;
                }

                currentFrame.RemoveAllViews();
                if (platform.Parent is AViewGroup oldParent)
                    oldParent.RemoveView(platform);
                currentFrame.AddView(platform, new FrameLayout.LayoutParams(
                    FrameLayout.LayoutParams.MatchParent,
                    FrameLayout.LayoutParams.MatchParent));
            })
        {
            Modifier = Modifier.FillMaxSize(),
        };

        var container = new Box { Modifier = Modifier.FillMaxSize() };
        container.Add(host);
        container.Add(new DisposableEffect(_pageKeys[page], () => () =>
        {
            frame?.RemoveAllViews();
            frame = null;
        }));
        return container;
    }

    ComposableNode WrapTheme(ComposableNode content, IComposer composer)
    {
        if (_theme is null)
            return content;

        bool dark = _theme.IsDark.Value;
        var background = ToComposeColor(_barBackground.Value);
        var selected = ToComposeColor(_selectedTab.Value ?? _barText.Value);
        var unselected = ToComposeColor(_unselectedTab.Value ?? _barText.Value);
        var scheme = composer.RememberKeyed(
            () => dark
                ? MaterialTheme.DarkColorScheme(
                    primary: selected,
                    onSurface: selected,
                    onSurfaceVariant: unselected,
                    surface: background,
                    surfaceContainer: background)
                : MaterialTheme.LightColorScheme(
                    primary: selected,
                    onSurface: selected,
                    onSurfaceVariant: unselected,
                    surface: background,
                    surfaceContainer: background),
            [dark, _barBackground.Value, _selectedTab.Value, _unselectedTab.Value, _barText.Value]);

        var themed = new MaterialTheme
        {
            ColorScheme = scheme,
            Dark = dark,
            UseDynamicColor = false,
        };
        themed.Add(content);
        return themed;
    }

    ComposeColor? ResolveTabTextColor(bool selected) =>
        ToComposeColor(selected
            ? _selectedTab.Value ?? _barText.Value
            : _unselectedTab.Value ?? _barText.Value);

    static ComposeColor? ToComposeColor(long? packed) =>
        packed is long value ? ComposeColor.FromPacked(value) : null;

    ToolbarPlacement GetToolbarPlacement() =>
        VirtualView is MauiTabbedPage tabbed
            ? MauiAndroidTabbedPage.GetToolbarPlacement(tabbed)
            : ToolbarPlacement.Top;

    void ReplacePages(MauiTabbedPage tabbed)
    {
        UnsubscribePages();
        var pages = tabbed.Children.ToArray();
        var live = pages.ToHashSet();

        foreach (var removed in _icons.Keys.Where(page => !live.Contains(page)).ToArray())
        {
            _icons[removed].Reset();
            _icons.Remove(removed);
            _pageKeys.Remove(removed);
        }

        foreach (var page in pages)
        {
            page.PropertyChanged += OnPagePropertyChanged;
            if (!_pageKeys.ContainsKey(page))
                _pageKeys.Add(page, _nextPageKey++);
            if (!_icons.TryGetValue(page, out var icon))
            {
                icon = new TabbedPageIcon(page, this);
                _icons.Add(page, icon);
            }
            RefreshIcon(icon);
        }

        _pages = pages;
        _pagesVersion.Value++;

        int selected = tabbed.CurrentPage is null ? -1 : tabbed.Children.IndexOf(tabbed.CurrentPage);
        if (selected < 0 && pages.Length > 0)
        {
            tabbed.CurrentPage = pages[0];
            selected = 0;
        }
        if (selected >= 0)
        {
            _selectedIndex.Value = selected;
            MovePagerTo(selected, smooth: false);
        }
    }

    void UnsubscribePages()
    {
        foreach (var page in _pages)
            page.PropertyChanged -= OnPagePropertyChanged;
    }

    void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MauiPage page)
            return;

        if (e.PropertyName == MauiPage.IconImageSourceProperty.PropertyName &&
            _icons.TryGetValue(page, out var icon))
        {
            RefreshIcon(icon);
        }

        if (e.PropertyName == MauiPage.TitleProperty.PropertyName ||
            e.PropertyName == MauiPage.IconImageSourceProperty.PropertyName ||
            e.PropertyName == Microsoft.Maui.Controls.VisualElement.IsEnabledProperty.PropertyName)
        {
            _pagesVersion.Value++;
        }
    }

    async void RefreshIcon(TabbedPageIcon icon)
    {
        try
        {
            await icon.RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[TabbedViewHandler] tab icon load failed: {ex.Message}");
        }
    }

    void SelectPage(int index)
    {
        if (VirtualView is not MauiTabbedPage tabbed ||
            index < 0 || index >= tabbed.Children.Count ||
            !tabbed.Children[index].IsEnabled)
        {
            return;
        }

        _selectedIndex.Value = index;
        tabbed.CurrentPage = tabbed.Children[index];
    }

    void MovePagerTo(int index, bool smooth)
    {
        if (_pagerState is null || index < 0 || index >= _pages.Count)
            return;

        long requestVersion = ++_selectionRequestVersion;
        _requestedIndex = index;
        _requestGracePasses = 1;
        _requestIsAnimating = smooth;
        _selectionCancellation?.Cancel();
        _selectionCancellation?.Dispose();
        _selectionCancellation = new CancellationTokenSource();
        if (!smooth)
        {
            _pagerState.RequestScrollToPage(index);
            return;
        }

        AnimatePagerTo(index, requestVersion, _selectionCancellation.Token);
    }

    async void AnimatePagerTo(int index, long requestVersion, CancellationToken cancellationToken)
    {
        var state = _pagerState;
        if (state is null)
            return;

        try
        {
            await state.AnimateScrollToPageAsync(index, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[TabbedViewHandler] pager selection failed: {ex.Message}");
        }
        finally
        {
            if (_selectionRequestVersion == requestVersion)
            {
                _requestedIndex = -1;
                _requestIsAnimating = false;
            }
        }
    }

    void SynchronizeCurrentPage(int index, IReadOnlyList<MauiPage> pages)
    {
        if (index < 0 || index >= pages.Count)
            return;
        if (_requestedIndex >= 0)
        {
            if (index != _requestedIndex)
            {
                if (_requestIsAnimating || _requestGracePasses-- > 0)
                    return;
            }
            _requestedIndex = -1;
            _requestIsAnimating = false;
        }

        var page = pages[index];
        if (VirtualView is MauiTabbedPage tabbed && !ReferenceEquals(tabbed.CurrentPage, page))
        {
            _selectedIndex.Value = index;
            _updatingCurrentPageFromPager = true;
            try
            {
                tabbed.CurrentPage = page;
            }
            finally
            {
                _updatingCurrentPageFromPager = false;
            }
        }
    }
}
