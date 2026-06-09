using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Column and Row Arrangement — SpaceBetween, SpacedBy, etc.</summary>
public static class ColumnRowArrangementsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-arrangements",
        CategoryId:  "containers",
        Title:       "Column / Row arrangements",
        Description: "Arrangement.SpaceBetween, SpaceAround, SpacedBy applied to Row + Column.",
        Build:       () => new Column
        {
            new Text("Row(Arrangement.SpacedBy(12)):"),
            new Row(Arrangement.SpacedBy(12))
            {
                new Text("A"), new Text("B"), new Text("C"),
            },
            new Text("Row(Arrangement.SpaceBetween):"),
            new Row(Arrangement.SpaceBetween)
            {
                Modifier.Companion.FillMaxWidth(),
                new Text("A"), new Text("B"), new Text("C"),
            },
            new Text("Row(Arrangement.SpaceAround):"),
            new Row(Arrangement.SpaceAround)
            {
                Modifier.Companion.FillMaxWidth(),
                new Text("A"), new Text("B"), new Text("C"),
            },
        });
}
