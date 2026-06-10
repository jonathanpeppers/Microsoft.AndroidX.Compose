using Placeable = AndroidX.Compose.UI.Layout.Placeable;

namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// JetNews interests screen — a <see cref="PrimaryTabRow"/> with three
/// tabs (Topics / People / Publications) and a per-tab toggleable
/// list. Topics are rendered through a custom <see cref="Layout"/>
/// that re-balances rows across two columns on wide screens.
/// </summary>
public static class InterestsScreen
{
    /// <summary>Materialize the interests screen.</summary>
    public static Scaffold Build(
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int>        selectedTab,
        DrawerStateHolder        drawerState) =>
        new()
        {
            TopBar = new CenterAlignedTopAppBar
            {
                NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
                {
                    new Icon(Resource.Drawable.ic_menu, "Open navigation drawer"),
                },
                Title = new Text("Interests")
                {
                    FontSize   = 18,
                    FontWeight = FontWeight.SemiBold,
                },
            },
            Body = BuildBody(selectedTab, selectedTopics, selectedPeople, selectedPublications),
        };

    static Column BuildBody(
        MutableState<int>        selectedTab,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications) =>
        new()
        {
            Modifier.FillMaxSize(),
            new PrimaryTabRow(selectedTabIndex: selectedTab.Value)
            {
                BuildTab(selectedTab, 0, "Topics"),
                BuildTab(selectedTab, 1, "People"),
                BuildTab(selectedTab, 2, "Publications"),
            },
            selectedTab.Value switch
            {
                0 => BuildTopics(selectedTopics),
                1 => BuildSimpleList(InterestsRepo.People, selectedPeople),
                _ => BuildSimpleList(InterestsRepo.Publications, selectedPublications),
            },
        };

    static Tab BuildTab(MutableState<int> selectedTab, int index, string label) =>
        new(
            selected: selectedTab.Value == index,
            onClick:  () => selectedTab.Value = index)
        {
            Text = new Text(label) { FontSize = 14 },
        };

    static ComposableNode BuildTopics(MutableStateList<string> selected) =>
        new Composed(c =>
        {
            // Topics content can exceed viewport height (especially landscape),
            // so the outer column needs vertical scrolling.
            var scroll = c.Remember(() => new ScrollState());
            var col = new Column
            {
                Modifier.FillMaxWidth().VerticalScroll(scroll),
            };
            foreach (var section in InterestsRepo.Topics)
            {
                col.Add(BuildSectionHeader(section.Key));
                var rows = new List<ComposableNode>();
                foreach (var topic in section.Value)
                {
                    var key = $"{section.Key}/{topic}";
                    rows.Add(BuildToggleRow(topic, selected.Contains(key), () => Toggle(selected, key)));
                }
                col.Add(BuildAdaptiveTopicSection(rows));
                col.Add(new HorizontalDivider
                {
                    Modifier = Modifier.Padding(horizontal: 16, vertical: 8),
                });
            }
            return col;
        });

    // Mirrors upstream JetNews's `InterestsAdaptiveContentLayout` — bins
    // topic rows into N columns whose count is driven by the parent's
    // available width. Backed by Compose's low-level `Layout` primitive.
    static Layout BuildAdaptiveTopicSection(IReadOnlyList<ComposableNode> rows)
    {
        const int multiColumnBreakpointPx = 600;

        var layout = new Layout(measurePolicy: (scope, measurables, constraints) =>
        {
            int max = constraints.HasBoundedWidth
                ? constraints.MaxWidth
                : multiColumnBreakpointPx;
            int columns = max >= multiColumnBreakpointPx ? 2 : 1;
            int columnWidth = max / columns;

            var childConstraints = Constraints.FixedWidth(columnWidth);
            var placeables       = new Placeable[measurables.Count];
            var columnHeights    = new int[columns];
            var columnAssign     = new int[measurables.Count];
            for (int i = 0; i < measurables.Count; i++)
            {
                placeables[i] = measurables[i].Measure(childConstraints);
                int h = placeables[i].Height;
                int target = 0;
                for (int c = 1; c < columns; c++)
                    if (columnHeights[c] < columnHeights[target]) target = c;
                columnAssign[i] = target;
                columnHeights[target] += h;
            }

            int totalHeight = 0;
            for (int c = 0; c < columns; c++)
                if (columnHeights[c] > totalHeight) totalHeight = columnHeights[c];

            return scope.Layout(columnWidth * columns, totalHeight, placement =>
            {
                var yByColumn = new int[columns];
                for (int i = 0; i < placeables.Length; i++)
                {
                    int col = columnAssign[i];
                    placement.PlaceRelative(
                        placeables[i],
                        x: col * columnWidth,
                        y: yByColumn[col]);
                    yByColumn[col] += placeables[i].Height;
                }
            });
        })
        {
            Modifier.FillMaxWidth(),
        };
        foreach (var row in rows)
            layout.Add(row);
        return layout;
    }

    static LazyColumn<string> BuildSimpleList(IReadOnlyList<string> items,
                                              MutableStateList<string> selected) =>
        new(items: items,
            itemContent: item =>
                BuildToggleRow(item, selected.Contains(item), () => Toggle(selected, item)))
        {
            Modifier = Modifier.FillMaxSize(),
        };

    static Box BuildSectionHeader(string label) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(horizontal: 16, vertical: 12),
            new Text(label)
            {
                FontSize   = 14,
                FontWeight = FontWeight.SemiBold,
                Color      = Color.FromHex("#666666"),
            },
        };

    static Row BuildToggleRow(string label, bool selected, Action onToggle) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 12)
                .Clickable(onToggle),
            new Text(label)
            {
                FontSize = 16,
                Modifier = Modifier.Weight(1f, fill: true),
            },
            new Icon(
                selected ? Resource.Drawable.ic_check : Resource.Drawable.ic_add,
                selected ? "Subscribed" : "Subscribe"),
        };

    static void Toggle(MutableStateList<string> set, string key)
    {
        if (set.Contains(key))
            set.Remove(key);
        else
            set.Add(key);
    }
}
