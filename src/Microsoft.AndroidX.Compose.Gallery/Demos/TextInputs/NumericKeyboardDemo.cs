using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.TextInputs;

/// <summary>
/// <see cref="TextField.KeyboardOptions"/> — surface a numeric IME
/// (vs the default text keyboard) by building a
/// <see cref="AndroidX.Compose.Foundation.Text.KeyboardOptions"/> from
/// the singleton <see cref="KeyboardOptionsCompanion.Default"/> and
/// flipping the <c>keyboardType</c> slot via the bound
/// <c>KeyboardOptions.Copy(...)</c>.
///
/// <c>KeyboardOptions.Companion</c> is bound but Mono's binder skips
/// the static <c>Companion</c> field accessor on the outer class — see
/// <see cref="KeyboardOptionsCompanion"/> for the JNI bootstrap.
/// </summary>
public static class NumericKeyboardDemo
{
    // Compose Foundation's `KeyboardType` is a @JvmInline value class
    // wrapping `Int`; the constants below match the stable lowering
    // emitted by Compose 1.x:
    //   Unspecified = 0  Text = 1   Ascii = 2  Number = 3
    //   Phone = 4        Uri  = 5   Email = 6  Password = 7
    //   NumberPassword = 8           Decimal = 9
    const int KeyboardTypeNumber = 3;

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-textfield-numeric-keyboardoptions",
        CategoryId:  "text-inputs",
        Title:       "Numeric KeyboardOptions",
        Description: "KeyboardOptions.Copy flips keyboardType=Number so the OS shows the digit IME.",
        Build:       c =>
        {
            var phone   = c.MutableStateOf("");
            var generic = c.MutableStateOf("");
            var d       = KeyboardOptionsCompanion.Default;
            return new Column
            {
                new TextField(phone, singleLine: true)
                {
                    Label           = new Text("Phone (numeric IME)"),
                    KeyboardOptions = d.Copy(
                        d.Capitalization, d.AutoCorrectEnabled,
                        KeyboardTypeNumber, d.ImeAction,
                        d.PlatformImeOptions, d.ShowKeyboardOnFocus,
                        d.HintLocales),
                },
                new TextField(generic, singleLine: true)
                {
                    Label = new Text("Generic (text IME)"),
                },
                new Text($"Digits typed: {phone.Value.Length}"),
            };
        });
}
