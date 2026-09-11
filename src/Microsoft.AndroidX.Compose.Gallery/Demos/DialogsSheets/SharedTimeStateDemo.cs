using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.DialogsSheets;

/// <summary>Hoists shared time ownership above independently removable picker consumers.</summary>
public static class SharedTimeStateDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "dialogs-shared-time-state",
        CategoryId: "dialogs-sheets",
        Title: "Shared time ownership",
        Description: "Hide either picker without removing the shared native save-state owner.",
        Build: c =>
        {
            var showClock = c.MutableStateOf(true);
            var showInput = c.MutableStateOf(true);
            var state = c.RememberTimePickerState();
            return new Column
            {
                new Text($"Shared selection: {state.Hour:D2}:{state.Minute:D2}"),
                new Row
                {
                    new Button(() => showClock.Value = !showClock.Value) { new Text("Toggle clock") },
                    new Button(() => showInput.Value = !showInput.Value) { new Text("Toggle keyboard") },
                },
                new Box { showClock.Value ? new TimePicker(state) : null },
                new Box { showInput.Value ? new TimeInput(state) : null },
            };
        });
}
