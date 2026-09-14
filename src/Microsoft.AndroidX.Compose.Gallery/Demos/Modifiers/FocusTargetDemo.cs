using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Native editor/selector handoff and captured-focus clearing.</summary>
public static class FocusTargetDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "modifiers-focus-target",
        CategoryId: "modifiers",
        Title: "Focus target and manager",
        Description: "Move input focus without adding an accessibility-focusable control; compare normal and forced clear.",
        Build: c =>
        {
            var editor = c.Remember(() => new FocusRequester());
            var selector = c.Remember(() => new FocusRequester());
            var manager = LocalFocusManager.Current(c);
            var value = c.MutableStateOf("");
            var expanded = c.MutableStateOf(false);
            var tick = c.MutableStateOf(0);
            var editorFocus = c.MutableStateOf(false);
            var selectorState = c.MutableStateOf(new FocusState(false, false, false).ToString());
            bool visible = expanded.Value;
            c.LaunchedEffect(visible, _ =>
            {
                if (visible && expanded.Value)
                    selector.RequestFocus();
                return Task.CompletedTask;
            });
            return new Column
            {
                new Text($"Editor focused: {editorFocus.Value}; selector: {selectorState.Value}"),
                new TextField(value, singleLine: true)
                {
                    Modifier = Modifier.FocusRequester(editor).OnFocusChanged(state =>
                    {
                        editorFocus.Value = state.IsFocused;
                        if (state.IsFocused)
                            expanded.Value = false;
                    }).Semantics("Focus demo editor"),
                },
                new Button(() => expanded.Value = true) { new Text("Open selector") },
                new Button(() => editor.RequestFocus()) { new Text("Return to editor") },
                new Button(() => manager.ClearFocus()) { new Text("Clear focus") },
                new Button(() => manager.ClearFocus(force: true)) { new Text("Force clear focus") },
                new Button(() => tick.Value++) { new Text($"Unrelated recomposition: {tick.Value}") },
                visible ? new Column
                {
                    Modifier.FocusRequester(selector)
                        .OnFocusChanged(state => selectorState.Value = state.ToString())
                        .FocusTarget().Semantics("Focus demo selector"),
                    new Text("Selector (not an extra accessibility focus stop)"),
                    new Button(() => selector.CaptureFocus()) { new Text("Capture selector focus") },
                    new Button(() => selector.FreeFocus()) { new Text("Release capture") },
                } : null,
                new BackHandler(() => expanded.Value = false, enabled: visible),
            };
        });
}
