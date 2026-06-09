using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>SecureTextField — filled-style password input.</summary>
public static class SecureTextFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-secure-textfield",
        CategoryId:  "text-inputs",
        Title:       "SecureTextField",
        Description: "Filled-style secure (password) input with mask + clear button.",
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
            };
        });
}
