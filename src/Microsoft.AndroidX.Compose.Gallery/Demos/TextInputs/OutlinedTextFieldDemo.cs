using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>OutlinedTextField — adds Prefix / Suffix that don't fit on the filled variant.</summary>
public static class OutlinedTextFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-outlined-textfield",
        CategoryId:  "text-inputs",
        Title:       "OutlinedTextField",
        Description: "Outlined variant with Prefix and Suffix slots.",
        Build:       c =>
        {
            var handle = c.Remember(() => new MutableState<string>(""));
            return new Column
            {
                new OutlinedTextField(handle)
                {
                    Label          = new Text("Handle"),
                    Prefix         = new Text("@"),
                    Suffix         = new Text(".dev"),
                    SupportingText = new Text($"len={handle.Value.Length}"),
                    SingleLine     = true,
                },
                new Text($"You'll be @{(string.IsNullOrEmpty(handle.Value) ? "?" : handle.Value)}.dev"),
            };
        });
}
