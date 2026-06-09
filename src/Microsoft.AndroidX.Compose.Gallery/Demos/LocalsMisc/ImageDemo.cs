using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>Image — resource-id overload and explicit Painter handle overload.</summary>
public static class ImageDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-image",
        CategoryId:  "locals-misc",
        Title:       "Image",
        Description: "Resource-id ctor renders a drawable; uses StringResource to pull the caption.",
        Build:       c => new Column
        {
            new Text("Resource-id ctor:"),
            new Image(Resource.Drawable.ic_star, "Star icon"),
            new Text(c.StringResource(Resource.String.counter_caption)),
        });
}
