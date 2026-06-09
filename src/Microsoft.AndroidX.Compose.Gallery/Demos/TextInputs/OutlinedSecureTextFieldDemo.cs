using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>OutlinedSecureTextField — same as SecureTextField but with an outlined chrome.</summary>
public static class OutlinedSecureTextFieldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-outlined-secure-textfield",
        CategoryId:  "text-inputs",
        Title:       "OutlinedSecureTextField",
        Description: "Outlined-style secure input plus a Sign-in button that compares two fields.",
        Build:       c =>
        {
            var pwd     = c.Remember(() => new SecureTextFieldState());
            var confirm = c.Remember(() => new SecureTextFieldState());
            var status  = c.Remember(() => new MutableState<string>("Tap Sign in to compare"));
            return new Column
            {
                new SecureTextField(pwd)
                {
                    Label       = new Text("Password"),
                    LeadingIcon = new Text("🔒"),
                },
                new OutlinedSecureTextField(confirm)
                {
                    Label       = new Text("Confirm password"),
                    LeadingIcon = new Text("🔒"),
                },
                new Button(onClick: () =>
                    status.Value =
                        $"len={pwd.Text.Length}/{confirm.Text.Length}, " +
                        $"match={(pwd.Text == confirm.Text)}")
                {
                    new Text("Sign in"),
                },
                new Text(status.Value),
            };
        });
}
