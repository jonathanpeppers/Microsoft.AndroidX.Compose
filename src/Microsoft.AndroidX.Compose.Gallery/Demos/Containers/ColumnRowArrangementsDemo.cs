using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Column and Row Arrangement — SpaceBetween, SpacedBy, etc.</summary>
public static class ColumnRowArrangementsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-arrangements",
        CategoryId:  "containers",
        Title:       "Column / Row arrangements",
        Description: "Arrangement.SpaceBetween, SpaceAround, SpacedBy applied to Row + Column.",
        Build:       _ => new Column
        {
            new Text("Row(Arrangement.SpacedBy(12.Dp())):"),
            new Row(Arrangement.SpacedBy(12.Dp()))
            {
                new Text("A"), new Text("B"), new Text("C"),
            },
            new Text("Row(Arrangement.SpaceBetween):"),
            new Row(Arrangement.SpaceBetween)
            {
                Modifier.FillMaxWidth(),
                new Text("A"), new Text("B"), new Text("C"),
            },
            new Text("Row(Arrangement.SpaceAround):"),
            new Row(Arrangement.SpaceAround)
            {
                Modifier.FillMaxWidth(),
                new Text("A"), new Text("B"), new Text("C"),
            },
        });
}
