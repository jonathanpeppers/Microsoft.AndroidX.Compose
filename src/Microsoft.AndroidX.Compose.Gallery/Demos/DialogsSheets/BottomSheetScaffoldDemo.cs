using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>
/// BottomSheetScaffold with a <see cref="SheetStateHolder"/> +
/// <see cref="BottomSheetScaffold.ConfirmValueChange"/> veto and an
/// imperative <see cref="SheetStateHolder.ExpandAsync"/> /
/// <see cref="SheetStateHolder.PartialExpandAsync"/> control set.
/// </summary>
public static class BottomSheetScaffoldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-bottom-sheet-scaffold",
        CategoryId:  "dialogs-sheets",
        Title:       "BottomSheetScaffold",
        Description: "Persistent sheet, ConfirmValueChange refuses Hidden, imperative expand / collapse.",
        Build:       c =>
        {
            var sheet = c.Remember(() => new SheetStateHolder(skipPartiallyExpanded: false));
            var scaffold = new BottomSheetScaffold(sheetState: sheet)
            {
                new Column
                {
                    new Text("Main content"),
                    new Row
                    {
                        new Button(onClick: async () => await sheet.ExpandAsync())
                            { new Text("Expand") },
                        new Button(onClick: async () => await sheet.PartialExpandAsync())
                            { new Text("Peek") },
                    },
                },
            };
            // SheetState.Hidden requires skipPartiallyExpanded=true, and our
            // holder didn't set it — so vetoing Hidden has no extra effect,
            // but veto still demonstrates the wiring.
            scaffold.ConfirmValueChange = v => v != SheetValue.Hidden;
            scaffold.SheetContent = new Column
            {
                new Text("Persistent bottom sheet"),
                new Text("Drag to peek / expand."),
            };
            return new Box
            {
                Modifier.Height(360),
                scaffold,
            };
        });
}

