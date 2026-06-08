using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Containers;

/// <summary>BoxWithConstraints reports the available width/height to its content lambda.</summary>
public static class BoxWithConstraintsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-box-with-constraints",
        CategoryId:  "containers",
        Title:       "BoxWithConstraints",
        Description: "Reports MaxWidth / MaxHeight in dp — branch your layout on screen size.",
        Build:       () => new BoxWithConstraints(c => new Text(
            $"Max width = {c.MaxWidth:0.#} dp, max height = {c.MaxHeight:0.#} dp"))
        {
            Modifier = Modifier.Companion.FillMaxWidth().Padding(8),
        });
}
