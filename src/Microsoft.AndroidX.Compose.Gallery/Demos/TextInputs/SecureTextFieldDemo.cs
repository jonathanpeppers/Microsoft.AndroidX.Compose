using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>SecureTextField — filled-style password input.</summary>
public static class SecureTextFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-secure-textfield",
        CategoryId:  "text-inputs",
        Title:       "SecureTextField",
        Description: "Secure input with programmatic set, select-all, and clear operations.",
        Build:       c =>
        {
            var pwd = c.Remember(() => new SecureTextFieldState());
            return new Column
            {
                new SecureTextField(pwd)
                {
                    Label          = new Text("Password"),
                    LeadingIcon    = new Text("🔒"),
                    SupportingText = new Text("Masked input — length is the only thing the demo can see."),
                },
                new Text($"Length: {pwd.Text.Length}"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => pwd.SetText("secret")) { new Text("Set") },
                    new Button(() => pwd.SetTextAndSelectAll("replace me")) { new Text("Select all") },
                    new Button(pwd.ClearText) { new Text("Clear") },
                },
            };
        });
}
