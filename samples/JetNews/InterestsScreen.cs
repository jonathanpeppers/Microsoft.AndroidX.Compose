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

    // Faithfully mirrors upstream JetNews's `InterestsAdaptiveContentLayout`:
    // row-major chunked placement (items 0..cols-1 in row 0, cols..2*cols-1
    // in row 1, …), per-row height = max(items in row), with itemSpacing,
    // topPadding, and an itemMaxWidth cap. Number of columns is driven by
    // the parent's available width vs `multipleColumnsBreakPoint` in Dp.
    static Layout BuildAdaptiveTopicSection(
        IReadOnlyList<ComposableNode> rows,
        float topPadding = 0f)
    {
        const float itemSpacingDp              = 4f;
        const float itemMaxWidthDp             = 450f;
        const float multipleColumnsBreakPointDp = 600f;

        var layout = new Layout(measurePolicy: (scope, measurables, outerConstraints) =>
        {
            int multipleColumnsBreakPointPx = scope.RoundToPx(multipleColumnsBreakPointDp);
            int topPaddingPx                = scope.RoundToPx(topPadding);
            int itemSpacingPx               = scope.RoundToPx(itemSpacingDp);
            int itemMaxWidthPx              = scope.RoundToPx(itemMaxWidthDp);

            int outerMax = outerConstraints.HasBoundedWidth
                ? outerConstraints.MaxWidth
                : multipleColumnsBreakPointPx;
            int columns = outerMax < multipleColumnsBreakPointPx ? 1 : 2;
            int itemWidth = columns == 1
                ? outerMax
                : Math.Clamp(
                    (outerMax - (columns - 1) * itemSpacingPx) / columns,
                    0,
                    itemMaxWidthPx);
            var itemConstraints = outerConstraints.WithMaxWidth(itemWidth);

            int rowCount = (measurables.Count / columns) + 1;
            var rowHeights = new int[rowCount];
            var placeables = new Placeable[measurables.Count];
            for (int i = 0; i < measurables.Count; i++)
            {
                placeables[i] = measurables[i].Measure(itemConstraints);
                int row = i / columns;
                if (placeables[i].Height > rowHeights[row])
                    rowHeights[row] = placeables[i].Height;
            }

            int layoutHeight = topPaddingPx;
            for (int r = 0; r < rowHeights.Length; r++) layoutHeight += rowHeights[r];
            int layoutWidth = itemWidth * columns + itemSpacingPx * (columns - 1);

            return scope.Layout(
                outerConstraints.ConstrainWidth(layoutWidth),
                outerConstraints.ConstrainHeight(layoutHeight),
                placement =>
                {
                    int yPosition = topPaddingPx;
                    for (int rowStart = 0, rowIndex = 0;
                         rowStart < placeables.Length;
                         rowStart += columns, rowIndex++)
                    {
                        int xPosition = 0;
                        int end = Math.Min(rowStart + columns, placeables.Length);
                        for (int i = rowStart; i < end; i++)
                        {
                            placement.PlaceRelative(placeables[i], xPosition, yPosition);
                            xPosition += placeables[i].Width + itemSpacingPx;
                        }
                        yPosition += rowHeights[rowIndex];
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
