using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>
/// <see cref="OutlinedTextField.VisualTransformation"/> — mask the
/// rendered text with bullets while the underlying buffer holds the
/// real characters. Exercises the bound
/// <see cref="PasswordVisualTransformation"/> via the bridge's
/// <c>visualTransformation</c> slot.
/// </summary>
public static class PasswordFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-password-visualtransformation",
        CategoryId:  "text-inputs",
        Title:       "Password VisualTransformation",
        Description: "PasswordVisualTransformation masks text; the buffer still holds the real characters.",
        Build:       c =>
        {
            var pwd = c.MutableStateOf("secret");
            return new Column
            {
                new OutlinedTextField(pwd)
                {
                    Label                = new Text("Password"),
                    VisualTransformation = new PasswordVisualTransformation('•'),
                    SingleLine           = true,
                },
                new Text($"Buffer length: {pwd.Value.Length}"),
                new Text($"Echo (proves real chars): \"{pwd.Value}\""),
            };
        });
}
