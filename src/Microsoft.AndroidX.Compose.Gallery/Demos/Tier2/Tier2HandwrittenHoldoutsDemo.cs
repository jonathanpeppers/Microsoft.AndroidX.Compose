using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Exercises generated ambient overloads for handwritten composable facades.
/// </summary>
public static class Tier2HandwrittenHoldoutsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "tier2-handwritten-holdouts",
        CategoryId:  "tier2",
        Title:       "Handwritten holdouts",
        Description: "MaterialTheme, Scaffold, SnackbarHost, and segmented buttons through Tier 2 APIs.",
        Build:       static _ => new Tier2Adapter(() => HandwrittenHoldouts()));

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
                topBar: () => Text("Tier 2 holdouts")));
    }
}
