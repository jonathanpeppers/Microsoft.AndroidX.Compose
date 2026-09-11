using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using IComposer = AndroidX.Compose.Runtime.IComposer;
using System.Collections.Concurrent;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts mutable records with composition-local state.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/CollectionKeysTestActivity")]
public class CollectionKeysTestActivity : ComponentActivity
{
    internal static CollectionKeysTestActivity? Current { get; private set; }
    internal static MutableManagedState<IReadOnlyList<int>> Items { get; private set; } = new([]);
    internal static ConcurrentDictionary<int, (MutableNumberState<int> Local, MutableNumberState<int> Saved)> RowStates { get; } = new();
    internal static ConcurrentDictionary<int, (int Generation, int Local, int Saved)> Observed { get; } = new();
    internal static LazyListState ListState { get; private set; } = new();
    internal static LazyGridState GridState { get; private set; } = new();
    internal static LazyStaggeredGridState StaggeredState { get; private set; } = new();
    internal static PagerState PageState { get; private set; } = new(() => Items.Value.Count);
    internal static int Generation { get; private set; }
    internal static int EmptyGeneration { get; private set; }
    static int s_surface;
    static int s_style;
    static bool s_keyed;
    static bool s_conditionalPager;

    internal static int FirstIndex => s_surface switch
    {
        0 or 1 => ListState.FirstVisibleItemIndex,
        2 or 3 => GridState.FirstVisibleItemIndex,
        4 or 5 => StaggeredState.FirstVisibleItemIndex,
        _ => PageState.CurrentPage,
    };

    internal static Task ScrollToAsync(int index) => s_surface switch
    {
        0 or 1 => ListState.ScrollToItemAsync(index),
        2 or 3 => GridState.ScrollToItemAsync(index),
        4 or 5 => StaggeredState.ScrollToItemAsync(index),
        _ => PageState.ScrollToPageAsync(index),
    };

    internal static void Reset(int surface, int style, bool keyed, bool conditionalPager = false)
    {
        Current = null;
        Items = new(Enumerable.Range(1, 50).ToArray());
        ListState = new(20);
        GridState = new(20);
        StaggeredState = new(20);
        PageState = new(() => Items.Value.Count, initialPage: 20);
        Generation = 0;
        EmptyGeneration = -1;
        s_surface = surface;
        s_style = style;
        s_keyed = keyed;
        s_conditionalPager = conditionalPager;
        RowStates.Clear();
        Observed.Clear();
    }

    internal static void Mutate(IReadOnlyList<int> items)
    {
        Generation++;
        Items.Value = items;
    }

    internal static void SetKeyed(bool keyed)
    {
        s_keyed = keyed;
        Mutate([.. Items.Value]);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent(RenderRoot);
        Current = this;
    }

    static void RenderRoot(IComposer composer)
    {
        bool showCollection = !s_conditionalPager || PageState.PageCount > 0;
        // Explicit branch groups isolate this regression from compiler control-flow lowering.
        composer.StartReplaceableGroup(showCollection ? 351001 : 351002);
        try
        {
            if (showCollection)
                RenderCollection(composer);
            else
                EmptyGeneration = Generation;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    static void RenderCollection(IComposer composer)
    {
        var items = Items.Value;
        int generation = Generation;
        Func<int, object>? key = s_keyed ? static item => item : null;
        var modifier = Modifier.Size(280);
        if (s_style == 0)
        {
            Func<int, ComposableNode> row = item => new CollectionKeyRow(item, generation);
            ComposableNode node = s_surface switch
            {
                0 => new LazyColumn<int>(items, row) { State = ListState, Key = key },
                1 => new LazyRow<int>(items, row) { State = ListState, Key = key },
                2 => new LazyVerticalGrid<int>(GridCells.Fixed(1), items, row) { State = GridState, Key = key },
                3 => new LazyHorizontalGrid<int>(GridCells.Fixed(1), items, row) { State = GridState, Key = key },
                4 => new LazyVerticalStaggeredGrid<int>(StaggeredGridCells.Fixed(1), items, row) { State = StaggeredState, Key = key },
                5 => new LazyHorizontalStaggeredGrid<int>(StaggeredGridCells.Fixed(1), items, row) { State = StaggeredState, Key = key },
                6 => new HorizontalPager<int>(items, row) { State = PageState, Key = key },
                7 => new VerticalPager<int>(items, row) { State = PageState, Key = key },
                _ => throw new InvalidOperationException("Unknown collection surface."),
            };
            node.Modifier = modifier;
            node.Render(composer);
        }
        else if (s_style == 1)
        {
            Action<int, IComposer> row = (item, c) => new CollectionKeyRow(item, generation).Render(c);
            switch (s_surface)
            {
                case 0: Composables.LazyColumn(composer, items, row, modifier, ListState, key: key); break;
                case 1: Composables.LazyRow(composer, items, row, modifier, ListState, key: key); break;
                case 2: Composables.LazyVerticalGrid(composer, GridCells.Fixed(1), items, row, modifier, GridState, key: key); break;
                case 3: Composables.LazyHorizontalGrid(composer, GridCells.Fixed(1), items, row, modifier, GridState, key: key); break;
                case 4: Composables.LazyVerticalStaggeredGrid(composer, StaggeredGridCells.Fixed(1), items, row, modifier, StaggeredState, key: key); break;
                case 5: Composables.LazyHorizontalStaggeredGrid(composer, StaggeredGridCells.Fixed(1), items, row, modifier, StaggeredState, key: key); break;
                case 6: Composables.HorizontalPager(composer, items, row, modifier, PageState, key: key); break;
                case 7: Composables.VerticalPager(composer, items, row, modifier, PageState, key: key); break;
            }
        }
        else if (s_style == 3)
        {
            RenderOriginalSignatures(items, generation, modifier);
        }
        else
        {
            RenderImplicit(items, generation, modifier, key);
        }
    }

    [Composable]
    internal static void RenderOriginalSignatures(IReadOnlyList<int> items, int generation, Modifier modifier)
    {
        switch (s_surface)
        {
            case 0: Composables.LazyColumn(items, item => RenderRow(item, generation), modifier, ListState, false, null, null); break;
            case 1: Composables.LazyRow(items, item => RenderRow(item, generation), modifier, ListState, null, null); break;
            case 2: Composables.LazyVerticalGrid(GridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, GridState, null, null, null); break;
            case 3: Composables.LazyHorizontalGrid(GridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, GridState, null); break;
            case 4: Composables.LazyVerticalStaggeredGrid(StaggeredGridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, StaggeredState, null); break;
            case 5: Composables.LazyHorizontalStaggeredGrid(StaggeredGridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, StaggeredState, null); break;
            case 6: Composables.HorizontalPager(items, item => RenderRow(item, generation), modifier, PageState, null); break;
            case 7: Composables.VerticalPager(items, item => RenderRow(item, generation), modifier, PageState, null); break;
        }
    }

    [Composable]
    internal static void RenderImplicit(IReadOnlyList<int> items, int generation, Modifier modifier, Func<int, object>? key)
    {
        switch (s_surface)
        {
            case 0: Composables.LazyColumn(items, item => RenderRow(item, generation), modifier, ListState, key: key); break;
            case 1: Composables.LazyRow(items, item => RenderRow(item, generation), modifier, ListState, key: key); break;
            case 2: Composables.LazyVerticalGrid(GridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, GridState, key: key); break;
            case 3: Composables.LazyHorizontalGrid(GridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, GridState, key: key); break;
            case 4: Composables.LazyVerticalStaggeredGrid(StaggeredGridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, StaggeredState, key: key); break;
            case 5: Composables.LazyHorizontalStaggeredGrid(StaggeredGridCells.Fixed(1), items, item => RenderRow(item, generation), modifier, StaggeredState, key: key); break;
            case 6: Composables.HorizontalPager(items, item => RenderRow(item, generation), modifier, PageState, key: key); break;
            case 7: Composables.VerticalPager(items, item => RenderRow(item, generation), modifier, PageState, key: key); break;
        }
    }

    [Composable]
    internal static void RenderRow(int item, int generation)
    {
        new CollectionKeyRow(item, generation).Render();
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }
}
