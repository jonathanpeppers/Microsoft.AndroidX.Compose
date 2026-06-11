using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>
/// ModalBottomSheet with a <see cref="SheetStateHolder"/> and
/// <see cref="ModalBottomSheet.ConfirmValueChange"/> veto guarding
/// dismissal — the user has to confirm by tapping a button.
/// </summary>
public static class ModalBottomSheetVetoDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "dialogs-bottom-sheet-veto",
        CategoryId:  "dialogs-sheets",
        Title:       "ModalBottomSheet (veto)",
        Description: "ConfirmValueChange refuses dismissal until the user taps OK.",
        Build:       c =>
        {
            var open    = c.MutableStateOf(false);
            var confirm = c.MutableStateOf(false);
            var sheet   = c.Remember(() => new SheetStateHolder(skipPartiallyExpanded: true));

            ComposableNode? sheetNode = null;
            if (open.Value)
            {
                var modal = new ModalBottomSheet(
                    onDismissRequest: () => open.Value = false,
                    sheetState:       sheet)
                {
                    new Column
                    {
                        new Text("Tap OK to allow dismissal."),
                        new Text("Drag-to-dismiss is vetoed until then."),
                        new Button(onClick: () =>
                        {
                            confirm.Value = true;
                            open.Value    = false;
                        })
                        { new Text("OK") },
                    },
                };
                modal.ConfirmValueChange = v => v != SheetValue.Hidden || confirm.Value;
                sheetNode = modal;
            }

            return new Column
            {
                new Button(onClick: () => { confirm.Value = false; open.Value = true; })
                    { new Text("Show sheet") },
                sheetNode,
            };
        });
}

