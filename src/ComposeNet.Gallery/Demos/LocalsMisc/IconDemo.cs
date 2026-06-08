using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.LocalsMisc;

/// <summary>Icon — Material 3 tinted leaf for action surfaces.</summary>
public static class IconDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-icon",
        CategoryId:  "locals-misc",
        Title:       "Icon",
        Description: "Material 3 Icon takes a drawable resource id and a content description; it's the tinted counterpart to Image.",
        Build:       () => new Column
        {
            new Text("Settings:"),
            new Icon(Resource.Drawable.ic_settings, "Settings"),
            new Text("Star:"),
            new Icon(Resource.Drawable.ic_star, "Star"),
        });
}
