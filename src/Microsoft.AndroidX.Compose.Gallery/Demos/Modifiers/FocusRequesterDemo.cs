using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Programmatic focus via FocusRequester + Focusable + OnFocusChanged.</summary>
public static class FocusRequesterDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-focus-requester",
        CategoryId:  "modifiers",
        Title:       "FocusRequester",
        Description: "Tap the button to programmatically focus the bordered Text; OnFocusChanged updates a status line.",
        Build:       c =>
        {
            var focusReq    = c.Remember(() => new FocusRequester());
            var focusStatus = c.Remember(() => new MutableState<string>("not focused"));

            return new Column
            {
                new Text($"Focus status: {focusStatus}"),
                new Text("Focus target")
                {
                    Modifier = Modifier.Companion
                        .FocusRequester(focusReq)
                        .OnFocusChanged(fs => focusStatus.Value =
                            fs.IsFocused ? "focused" : (fs.HasFocus ? "child has focus" : "not focused"))
                        .Focusable()
                        .Padding(8)
                        .Border(1, Color.FromRgb(0x55, 0x55, 0xAA))
                        .Padding(4),
                },
                new Button(onClick: () => focusReq.RequestFocus()) { new Text("Request focus") },
            };
        });
}
