using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.TextInputs;

/// <summary>TextField with every named slot — label, placeholder, leading/trailing icon, supporting text.</summary>
public static class TextFieldSlots
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-slots",
        CategoryId:  "text-inputs",
        Title:       "TextField slots",
        Description: "Label, Placeholder, LeadingIcon, TrailingIcon, SupportingText, SingleLine.",
        Build:       () =>
        {
            var name = Compose.Remember(() => new MutableState<string>(""));
            return new Column
            {
                new TextField(name)
                {
                    Label          = new Text("Your name"),
                    Placeholder    = new Text("Type something…"),
                    LeadingIcon    = new Text("👤"),
                    TrailingIcon   = new Text("✎"),
                    SupportingText = new Text("All five slots filled"),
                    SingleLine     = true,
                },
                new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
            };
        });
}
