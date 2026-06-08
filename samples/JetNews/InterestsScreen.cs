using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// JetNews interests screen — a <see cref="PrimaryTabRow"/> with three
/// tabs (Topics / People / Publications) and a per-tab toggleable
/// list. The upstream sample renders Topics in an adaptive two-column
/// custom <c>Layout</c> on wide screens; we render a single column.
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
            Modifier.Companion.FillMaxSize(),
            new PrimaryTabRow(selectedTabIndex: selectedTab.Value)
            {
                BuildTab(selectedTab, 0, "Topics"),
                BuildTab(selectedTab, 1, "People"),
                BuildTab(selectedTab, 2, "Publications"),
            },
            selectedTab.Value switch
            {
                0 => (ComposableNode) BuildTopics(selectedTopics),
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

    static Column BuildTopics(MutableStateList<string> selected)
    {
        var col = new Column { Modifier.Companion.FillMaxWidth() };
        foreach (var section in InterestsRepo.Topics)
        {
            col.Add(BuildSectionHeader(section.Key));
            foreach (var topic in section.Value)
            {
                var key = $"{section.Key}/{topic}";
                col.Add(BuildToggleRow(topic, selected.Contains(key), () => Toggle(selected, key)));
            }
            col.Add(new HorizontalDivider
            {
                Modifier = Modifier.Companion.Padding(horizontal: 16, vertical: 8),
            });
        }
        return col;
    }

    static LazyColumn<string> BuildSimpleList(IReadOnlyList<string> items,
                                              MutableStateList<string> selected) =>
        new(items: items,
            itemContent: item =>
                BuildToggleRow(item, selected.Contains(item), () => Toggle(selected, item)))
        {
            Modifier = Modifier.Companion.FillMaxSize(),
        };

    static Box BuildSectionHeader(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 16, vertical: 12),
            new Text(label)
            {
                FontSize   = 14,
                FontWeight = FontWeight.SemiBold,
                Color      = Color.FromHex("#666666"),
            },
        };

    static Row BuildToggleRow(string label, bool selected, System.Action onToggle) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 12)
                .Clickable(onToggle),
            new Text(label)
            {
                FontSize = 16,
                Modifier = Modifier.Companion.Weight(1f, fill: true),
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
