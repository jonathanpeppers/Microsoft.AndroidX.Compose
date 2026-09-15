using AndroidX.Compose.Foundation.Interaction;
using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>Live theme roles, explicit color pairs, and native FAB elevation for all four sizes.</summary>
public static class FabStylingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "buttons-fab-styling",
        CategoryId: "buttons",
        Title: "FAB colors and elevation",
        Description: "All four FABs: theme inheritance, explicit colors, flat elevation, and expanded labels.",
        Build: c =>
        {
            var dark = c.MutableStateOf(false);
            var custom = c.MutableStateOf(false);
            var expanded = c.MutableStateOf(true);
            var count = c.MutableStateOf(0);
            var theme = new MaterialTheme { Dark = dark.Value, UseDynamicColor = false };
            theme.Add(new Composed(inner =>
            {
                var scheme = inner.ColorScheme();
                var source = inner.Remember(InteractionSourceKt.MutableInteractionSource);
                var flat = inner.Remember(() =>
                    FloatingActionButtonDefaults.Instance.BottomAppBarFabElevation(0, 0, 0, 0));
                Color container = custom.Value ? Color.FromHex("#FFE082") : Color.FromPacked(scheme.TertiaryContainer);
                Color? content = custom.Value ? Color.Black : null;
                var normal = new FloatingActionButton(() => count.Value++)
                {
                    ContainerColor = container, ContentColor = content, Elevation = flat, InteractionSource = source,
                };
                normal.Add(new Text("+"));
                var small = new SmallFloatingActionButton(() => count.Value++)
                {
                    ContainerColor = container, ContentColor = content, Elevation = flat,
                };
                small.Add(new Text("+"));
                var large = new LargeFloatingActionButton(() => count.Value++)
                {
                    ContainerColor = container, ContentColor = content, Elevation = flat,
                };
                large.Add(new Text("+"));
                return new Column(verticalArrangement: Arrangement.SpacedBy(12.Dp()))
                {
                    new Text($"Tapped: {count.Value}"),
                    new Text("Tree: tertiary role (inherited content) or amber / black; flat elevation"),
                    normal, small, large,
                    new ExtendedFloatingActionButton(() => count.Value++, expanded.Value)
                    {
                        ContainerColor = container, ContentColor = content, Elevation = flat,
                        Icon = new Text("+"), Text = new Text("Add item"),
                    },
                    new Text("Direct catalog: same theme role; Kotlin elevation and interactions"),
                    new Composed(direct =>
                    {
                        Direct(direct, () => count.Value++, expanded.Value);
                        return null;
                    }),
                };
            }));
            return new Column
            {
                new TextButton(() => dark.Value = !dark.Value) { new Text("Toggle light/dark") },
                new TextButton(() => custom.Value = !custom.Value) { new Text("Toggle explicit colors / theme role") },
                new TextButton(() => expanded.Value = !expanded.Value) { new Text("Toggle extended label") },
                theme,
            };
        });

    [global::AndroidX.Compose.Composable]
    internal static void Direct(IComposer c, Action onClick, bool expanded)
    {
        var container = Color.FromPacked(c.ColorScheme().TertiaryContainer);
        Composables.FloatingActionButton(onClick, () => Composables.Text("+"), containerColor: container);
        Composables.SmallFloatingActionButton(onClick, () => Composables.Text("+"), containerColor: container);
        Composables.LargeFloatingActionButton(onClick, () => Composables.Text("+"), containerColor: container);
        Composables.ExtendedFloatingActionButton(onClick, expanded,
            () => Composables.Text("Add item"), () => Composables.Text("+"), containerColor: container);
    }
}
