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
    public static ComposableNode Build(
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int>        selectedTab,
        DrawerStateHolder        drawerState) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            var scheme = c.ColorScheme();
            string title = c.StringResource(Resource.String.interests_title);
            string topics = c.StringResource(Resource.String.interests_section_topics);
            string people = c.StringResource(Resource.String.interests_section_people);
            string publications = c.StringResource(Resource.String.interests_section_publications);
            string openNavigation = c.StringResource(Resource.String.cd_open_navigation_drawer);
            string subscribed = c.StringResource(Resource.String.cd_subscribed);
            string subscribe = c.StringResource(Resource.String.cd_subscribe);
            return new Scaffold
            {
                TopBar = new CenterAlignedTopAppBar
                {
                    NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
                    {
                        new Icon(Resource.Drawable.ic_menu, openNavigation),
                    },
                    Title = new Text(title)
                    {
                        Color = Color.FromPacked(scheme.Primary),
                    }.WithTypography(typography.TitleLarge),
                },
                Body = BuildBody(
                    selectedTab,
                    selectedTopics,
                    selectedPeople,
                    selectedPublications,
                    topics,
                    people,
                    publications,
                    subscribed,
                    subscribe,
                    typography),
            };
        });

    static Column BuildBody(
        MutableState<int>        selectedTab,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        string topics,
        string people,
        string publications,
        string subscribed,
        string subscribe,
        AndroidX.Compose.Material3.Typography typography) =>
        new()
        {
            Modifier.FillMaxSize(),
            new PrimaryTabRow(selectedTabIndex: selectedTab.Value)
            {
                BuildTab(selectedTab, 0, topics, typography),
                BuildTab(selectedTab, 1, people, typography),
                BuildTab(selectedTab, 2, publications, typography),
            },
            selectedTab.Value switch
            {
                0 => BuildTopics(selectedTopics, subscribed, subscribe),
                1 => BuildSimpleList(
                    InterestsRepo.People,
                    selectedPeople,
                    subscribed,
                    subscribe,
                    typography),
                _ => BuildSimpleList(
                    InterestsRepo.Publications,
                    selectedPublications,
                    subscribed,
                    subscribe,
                    typography),
            },
        };

    static Tab BuildTab(
        MutableState<int> selectedTab,
        int index,
        string label,
        AndroidX.Compose.Material3.Typography typography) =>
        new(
            selected: selectedTab.Value == index,
            onClick:  () => selectedTab.Value = index)
        {
            Text = new Text(label).WithTypography(typography.LabelLarge),
        };

    static ComposableNode BuildTopics(
        MutableStateList<string> selected,
        string subscribed,
        string subscribe) =>
        new Composed(c =>
        {
            var scroll = c.Remember(() => new ScrollState());
            var typography = c.Typography();
            var scheme = c.ColorScheme();
            var col = new Column
            {
                Modifier.FillMaxWidth().VerticalScroll(scroll),
            };
            foreach (var section in InterestsRepo.Topics)
            {
                col.Add(BuildSectionHeader(section.Key, typography, scheme));
                List<ComposableNode> rows = [];
                foreach (var topic in section.Value)
                {
                    var key = $"{section.Key}/{topic}";
                    rows.Add(BuildToggleRow(
                        topic,
                        selected.Contains(key),
                        () => Toggle(selected, key),
                        subscribed,
                        subscribe,
                        typography));
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
                                              MutableStateList<string> selected,
                                              string subscribed,
                                              string subscribe,
                                              AndroidX.Compose.Material3.Typography typography) =>
        new(items: items,
            itemContent: item =>
                BuildToggleRow(
                    item,
                    selected.Contains(item),
                    () => Toggle(selected, item),
                    subscribed,
                    subscribe,
                    typography))
        {
            Modifier = Modifier.FillMaxSize(),
        };

    static Box BuildSectionHeader(
        string label,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(horizontal: 16, vertical: 12),
            new Text(label)
            {
                Color = Color.FromPacked(scheme.OnSurfaceVariant),
            }.WithTypography(typography.TitleMedium),
        };

    static Row BuildToggleRow(
        string label,
        bool selected,
        Action onToggle,
        string subscribed,
        string subscribe,
        AndroidX.Compose.Material3.Typography typography) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 12)
                .Clickable(onToggle),
            new Text(label)
            {
                Modifier = Modifier.Weight(1f, fill: true),
            }.WithTypography(typography.TitleMedium),
            new Icon(
                selected ? Resource.Drawable.ic_check : Resource.Drawable.ic_add,
                selected ? subscribed : subscribe),
        };

    static void Toggle(MutableStateList<string> set, string key)
    {
        if (set.Contains(key))
            set.Remove(key);
        else
            set.Add(key);
    }
}
