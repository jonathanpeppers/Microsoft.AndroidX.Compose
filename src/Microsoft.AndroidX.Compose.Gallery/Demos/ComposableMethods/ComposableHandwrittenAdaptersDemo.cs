using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>
/// Exercises generated ambient overloads for handwritten composable facades.
/// </summary>
public static class ComposableHandwrittenAdaptersDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "composable-handwritten-holdouts",
        CategoryId:  "composable-methods",
        Title:       "Handwritten holdouts",
        Description: "Theme, scaffold, search, snackbar, and segmented controls through composable methods.",
        Build:       static _ => new ComposableDemoAdapter(() => HandwrittenHoldouts()));

    /// <summary>Renders the handwritten holdouts unlocked by ambient-overload generation.</summary>
    [Composable]
    public static void HandwrittenHoldouts()
    {
        var selected = Remember(() => new MutableNumberState<int>(0));
        var checkedState = Remember(() => new MutableState<bool>(false));
        var snackbarState = Remember(() => new SnackbarHostState());
        var text = Remember(() => new MutableState<string>("Editable"));
        var selection = Remember(() => new MutableState<AndroidX.Compose.UI.Text.Input.TextFieldValue>(
            ComposeExtensions.NewTextFieldValue("Selection-aware")));
        var search = Remember(() => new SearchBarState());
        var searchText = Remember(() => new SearchBarTextFieldState());
        var topSearch = Remember(() => new SearchBarState());
        var topSearchText = Remember(() => new SearchBarTextFieldState());
        var sheet = Remember(() =>
            new SheetStateHolder(skipPartiallyExpanded: false));
        IReadOnlyList<int> body = [0];

        MaterialTheme(() =>
            Scaffold(
                padding => LazyColumn(
                    body,
                    _ =>
                    {
                        Text("Single choice");
                        SingleChoiceSegmentedButtonRow(() =>
                        {
                            SegmentedButton(
                                index: 0,
                                count: 2,
                                selected: selected.Value == 0,
                                onClick: () => selected.Value = 0,
                                label: () => Text("Day"));
                            SegmentedButton(
                                index: 1,
                                count: 2,
                                selected: selected.Value == 1,
                                onClick: () => selected.Value = 1,
                                label: () => Text("Week"));
                        });

                        Text("Multi choice");
                        MultiChoiceSegmentedButtonRow(() =>
                            SegmentedButton(
                                index: 0,
                                count: 1,
                                @checked: checkedState.Value,
                                onCheckedChange: value =>
                                    checkedState.Value = value,
                                label: () => Text("Pinned"),
                                icon: () => Text("P")));

                        SnackbarHost(snackbarState);

                        TextField(
                            text,
                            singleLine: true,
                            label: () => Text("Filled text field"));
                        OutlinedTextField(
                            selection,
                            singleLine: true,
                            label: () => Text("Outlined TextFieldValue"));

                        SearchBar(
                            search,
                            inputField: () => SearchBarInputField(
                                searchText,
                                search,
                                placeholder: () => Text("Docked search")));
                        ExpandedDockedSearchBar(
                            search,
                            inputField: () => SearchBarInputField(
                                searchText,
                                search),
                            content: () => Text("Docked search results"));

                        TopSearchBar(
                            topSearch,
                            inputField: () => SearchBarInputField(
                                topSearchText,
                                topSearch,
                                placeholder: () => Text("Top search")));
                        ExpandedFullScreenSearchBar(
                            topSearch,
                            inputField: () => SearchBarInputField(
                                topSearchText,
                                topSearch),
                            content: () => Text("Full-screen search results"));

                        BottomSheetScaffold(
                            sheetContent: () => Text("Persistent sheet"),
                            content: () => Text("Bottom-sheet body"),
                            sheetState: sheet,
                            modifier: Modifier.Height(240),
                            topBar: () => Text("Bottom sheet scaffold"),
                            confirmValueChange: value =>
                                value != AndroidX.Compose.Material3.SheetValue.Hidden);

                        Layout(
                            (scope, measurables, constraints) =>
                            {
                                var placeables = measurables
                                    .Select(item => item.Measure(constraints))
                                    .ToArray();
                                int width = placeables.Max(item => item.Width);
                                int height = placeables.Sum(item => item.Height);
                                return scope.Layout(width, height, placement =>
                                {
                                    int y = 0;
                                    foreach (var placeable in placeables)
                                    {
                                        placement.PlaceRelative(placeable, 0, y);
                                        y += placeable.Height;
                                    }
                                });
                            },
                            () =>
                            {
                                Text("Custom Layout child 1");
                                Text("Custom Layout child 2");
                            },
                            modifier: Modifier.FillMaxWidth());
                    },
                    contentPadding: padding),
                topBar: () => Text("Composable adapters")));
    }
}
