using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Theming;

/// <summary>
/// Material Icons from
/// <c>Xamarin.AndroidX.Compose.Material.Icons.Core</c> exposed via the
/// <see cref="Icons"/> static surface, including the
/// <see cref="Icons.AutoMirrored"/> family that flips in RTL.
/// </summary>
public static class MaterialIconsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "theming-material-icons",
        CategoryId:  "theming",
        Title:       "Material Icons",
        Description: "Icons.Filled/Outlined/Rounded/Sharp/TwoTone + Icons.AutoMirrored.* — vector icons resolved through JNI (no drawable resources required).",
        Build:       c =>
        {
            var tapped = c.MutableStateOf("(none)");

            ComposableNode Tile(string name, AndroidX.Compose.UI.Graphics.Vector.ImageVector vec) =>
                new Column
                {
                    new IconButton(onClick: () => tapped.Value = name)
                        { new Icon(vec, name) },
                    new Text(name) { FontSize = new Sp(10) },
                };

            return new Column(verticalArrangement: Arrangement.SpacedBy(12.Dp()))
            {
                new Text($"Last tapped: {tapped}"),

                new Text("Icons.Filled (18):") { FontWeight = FontWeight.Bold },
                new FlowRow
                {
                    Modifier.Companion.FillMaxWidth(),
                    Tile("Search",        Icons.Filled.Search),
                    Tile("Menu",          Icons.Filled.Menu),
                    Tile("Add",           Icons.Filled.Add),
                    Tile("Delete",        Icons.Filled.Delete),
                    Tile("Edit",          Icons.Filled.Edit),
                    Tile("Settings",      Icons.Filled.Settings),
                    Tile("MoreVert",      Icons.Filled.MoreVert),
                    Tile("Close",         Icons.Filled.Close),
                    Tile("Check",         Icons.Filled.Check),
                    Tile("Star",          Icons.Filled.Star),
                    Tile("Favorite",      Icons.Filled.Favorite),
                    Tile("Share",         Icons.Filled.Share),
                    Tile("Home",          Icons.Filled.Home),
                    Tile("Person",        Icons.Filled.Person),
                    Tile("Notifications", Icons.Filled.Notifications),
                    Tile("Refresh",       Icons.Filled.Refresh),
                    Tile("Info",          Icons.Filled.Info),
                    Tile("Warning",       Icons.Filled.Warning),
                },

                new Text("Icons.AutoMirrored.Filled (flip in RTL):") { FontWeight = FontWeight.Bold },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    Tile("ArrowBack",    Icons.AutoMirrored.Filled.ArrowBack),
                    Tile("ArrowForward", Icons.AutoMirrored.Filled.ArrowForward),
                    Tile("Send",         Icons.AutoMirrored.Filled.Send),
                    Tile("List",         Icons.AutoMirrored.Filled.List),
                    Tile("ExitToApp",    Icons.AutoMirrored.Filled.ExitToApp),
                },

                new Text("Same glyph (Settings) across all 5 variants:") { FontWeight = FontWeight.Bold },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    Tile("Filled",   Icons.Filled.Settings),
                    Tile("Outlined", Icons.Outlined.Settings),
                    Tile("Rounded",  Icons.Rounded.Settings),
                    Tile("Sharp",    Icons.Sharp.Settings),
                    Tile("TwoTone",  Icons.TwoTone.Settings),
                },
            };
        });
}
