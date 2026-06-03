// `$default` is a Kotlin compiler convention: every @Composable function
// takes a trailing `int $default` bitmask where bit N == 1 means
// "parameter N was NOT provided; substitute the default". Bit positions
// are positional in the Kotlin source order — there's no runtime API
// to query them.
//
// `ColumnDefault` and `MaterialThemeDefault` are *generated* by
// ComposeNet.SourceGenerators from the generic `[ComposeDefaults<T>]`
// attribute below — the binder exposes those Kt classes, so the
// generator can read parameter names off the longest overload.
//
// The other seven (Button, Text, IconButton, FloatingActionButton,
// Surface, AlertDialog, TextField/OutlinedTextField) are *also*
// generated, but from the declarative `[ComposeDefaults]` overload.
// Their Kotlin overloads with the trailing $default param are stripped
// from the binding (mangled JVM names like `Text--4IGK_g` from inline
// classes such as `Color`/`TextUnit`/`Dp`), so there is no IMethodSymbol
// for the generator to introspect — we hand it the Kotlin parameter
// names instead. Names prefixed with `!` consume a bit position but
// don't emit an enum member (e.g. params the caller always provides).
//
// When dotnet/java-interop#1440 lands and exposes the inline-class
// overloads, the declarative attributes can be replaced with
// `[ComposeDefaults<ButtonKt>("Button", "ButtonDefault")]` etc. and
// this comment can be deleted.

using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Material3;
using ComposeNet;

[assembly: ComposeDefaults<ColumnKt>("Column", "ColumnDefault")]
[assembly: ComposeDefaults<MaterialThemeKt>("MaterialTheme", "MaterialThemeDefault")]

// androidx.compose.material3.ButtonKt.Button: 10 user params,
// bit 0 = onClick, bit 9 = content (both always provided).
[assembly: ComposeDefaults("ButtonDefault",
    "!onClick", "modifier", "enabled", "shape", "colors",
    "elevation", "border", "contentPadding", "interactionSource", "!content")]

// androidx.compose.material3.TextKt.Text--4IGK_g: 17 user params,
// bit 0 = text (always provided).
[assembly: ComposeDefaults("TextDefault",
    "!text", "modifier", "color", "fontSize", "fontStyle",
    "fontWeight", "fontFamily", "letterSpacing", "decoration", "align",
    "lineHeight", "overflow", "softWrap", "maxLines", "minLines",
    "onTextLayout", "style")]

// androidx.compose.material3.IconButtonKt.IconButton: 6 user params,
// bit 0 = onClick, bit 5 = content (both provided).
[assembly: ComposeDefaults("IconButtonDefault",
    "!onClick", "modifier", "enabled", "colors", "interactionSource", "!content")]

// androidx.compose.material3.FloatingActionButtonKt.FloatingActionButton-X-z6DiA:
// 8 user params, bit 0 = onClick, bit 7 = content (both provided).
[assembly: ComposeDefaults("FloatingActionButtonDefault",
    "!onClick", "modifier", "shape", "containerColor", "contentColor",
    "elevation", "interactionSource", "!content")]

// androidx.compose.material3.SurfaceKt.Surface-T9BRK9s (non-interactive):
// 8 user params, only bit 7 = content provided.
[assembly: ComposeDefaults("SurfaceDefault",
    "modifier", "shape", "color", "contentColor", "tonalElevation",
    "shadowElevation", "border", "!content")]

// androidx.compose.material3.AndroidAlertDialog_androidKt.AlertDialog-Oix01E0:
// 14 user params, bit 0 = onDismissRequest, bit 1 = confirmButton
// (both always provided). The four slot Function2 params (dismissButton,
// icon, title, text) are toggled per-call by AlertDialog.Render — they
// stay as enum members so callers can OR them in conditionally.
[assembly: ComposeDefaults("AlertDialogDefault",
    "!onDismissRequest", "!confirmButton", "modifier", "dismissButton",
    "icon", "title", "text", "shape", "containerColor", "iconContentColor",
    "titleContentColor", "textContentColor", "tonalElevation", "properties")]

// androidx.compose.material3.TextFieldKt.TextField (String overload) AND
// OutlinedTextFieldKt.OutlinedTextField (String overload): 23 user params,
// bit 0 = value, bit 1 = onValueChange (both provided).
[assembly: ComposeDefaults("TextFieldDefault",
    "!value", "!onValueChange", "modifier", "enabled", "readOnly",
    "textStyle", "label", "placeholder", "leadingIcon", "trailingIcon",
    "prefix", "suffix", "supportingText", "isError", "visualTransformation",
    "keyboardOptions", "keyboardActions", "singleLine", "maxLines", "minLines",
    "interactionSource", "shape", "colors")]

