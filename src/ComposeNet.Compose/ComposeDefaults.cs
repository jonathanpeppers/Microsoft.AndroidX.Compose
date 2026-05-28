// `$default` is a Kotlin compiler convention: every @Composable function
// takes a trailing `int $default` bitmask where bit N == 1 means
// "parameter N was NOT provided; substitute the default". Bit positions
// are positional in the Kotlin source order — there's no runtime API
// to query them.
//
// `ColumnDefault` and `MaterialThemeDefault` are *generated* by
// ComposeNet.SourceGenerators from the two assembly-level attributes
// below. The generator reads the longest overload of the named static
// method, names each bit after its parameter, and emits an `All`
// constant that ORs every user-defaultable bit.
//
// `ButtonDefault` and `TextDefault` are still HAND-ROLLED because the
// dotnet/android-libraries binding strips `ButtonKt.Button` and
// `TextKt.Text--4IGK_g` (we call them via raw JNI in `ComposeBridges`),
// so there's no C# `IMethodSymbol` for the generator to introspect.

using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Material3;
using ComposeNet;

[assembly: ComposeDefaults<ColumnKt>("Column", "ColumnDefault")]
[assembly: ComposeDefaults<MaterialThemeKt>("MaterialTheme", "MaterialThemeDefault")]

namespace ComposeNet;

// androidx.compose.material3.ButtonKt.Button: 10 user params,
// bit 0 = onClick, bit 9 = content (both always provided).
[System.Flags]
internal enum ButtonDefault
{
    None              = 0,
    Modifier          = 1 << 1,
    Enabled           = 1 << 2,
    Shape             = 1 << 3,
    Colors            = 1 << 4,
    Elevation         = 1 << 5,
    Border            = 1 << 6,
    ContentPadding    = 1 << 7,
    InteractionSource = 1 << 8,
    All = Modifier | Enabled | Shape | Colors | Elevation | Border | ContentPadding | InteractionSource,
}

// androidx.compose.material3.TextKt.Text--4IGK_g: 17 user params,
// bit 0 = text (always provided).
[System.Flags]
internal enum TextDefault
{
    None          = 0,
    Modifier      = 1 << 1,
    Color         = 1 << 2,
    FontSize      = 1 << 3,
    FontStyle     = 1 << 4,
    FontWeight    = 1 << 5,
    FontFamily    = 1 << 6,
    LetterSpacing = 1 << 7,
    Decoration    = 1 << 8,
    Align         = 1 << 9,
    LineHeight    = 1 << 10,
    Overflow      = 1 << 11,
    SoftWrap      = 1 << 12,
    MaxLines      = 1 << 13,
    MinLines      = 1 << 14,
    OnTextLayout  = 1 << 15,
    Style         = 1 << 16,
    All = Modifier | Color | FontSize | FontStyle | FontWeight | FontFamily | LetterSpacing
        | Decoration | Align | LineHeight | Overflow | SoftWrap | MaxLines | MinLines
        | OnTextLayout | Style,
}

// androidx.compose.material3.IconButtonKt.IconButton: 6 user params,
// bit 0 = onClick, bit 5 = content (both provided).
[System.Flags]
internal enum IconButtonDefault
{
    None              = 0,
    Modifier          = 1 << 1,
    Enabled           = 1 << 2,
    Colors            = 1 << 3,
    InteractionSource = 1 << 4,
    All = Modifier | Enabled | Colors | InteractionSource,
}

// androidx.compose.material3.FloatingActionButtonKt.FloatingActionButton-X-z6DiA:
// 8 user params, bit 0 = onClick, bit 7 = content (both provided).
[System.Flags]
internal enum FloatingActionButtonDefault
{
    None              = 0,
    Modifier          = 1 << 1,
    Shape             = 1 << 2,
    ContainerColor    = 1 << 3,
    ContentColor      = 1 << 4,
    Elevation         = 1 << 5,
    InteractionSource = 1 << 6,
    All = Modifier | Shape | ContainerColor | ContentColor | Elevation | InteractionSource,
}

// androidx.compose.material3.SurfaceKt.Surface-T9BRK9s (non-interactive):
// 8 user params, only bit 7 = content provided.
[System.Flags]
internal enum SurfaceDefault
{
    None             = 0,
    Modifier         = 1 << 0,
    Shape            = 1 << 1,
    Color            = 1 << 2,
    ContentColor     = 1 << 3,
    TonalElevation   = 1 << 4,
    ShadowElevation  = 1 << 5,
    Border           = 1 << 6,
    All = Modifier | Shape | Color | ContentColor | TonalElevation | ShadowElevation | Border,
}

// androidx.compose.material3.AndroidAlertDialog_androidKt.AlertDialog-Oix01E0:
// 14 user params, bit 0 = onDismissRequest, bit 1 = confirmButton
// (both always provided). The four slot Function2 params (dismissButton,
// icon, title, text) are optional and toggled per-call by AlertDialog.Render.
[System.Flags]
internal enum AlertDialogDefault
{
    None              = 0,
    Modifier          = 1 << 2,
    DismissButton     = 1 << 3,
    Icon              = 1 << 4,
    Title             = 1 << 5,
    Text              = 1 << 6,
    Shape             = 1 << 7,
    ContainerColor    = 1 << 8,
    IconContentColor  = 1 << 9,
    TitleContentColor = 1 << 10,
    TextContentColor  = 1 << 11,
    TonalElevation    = 1 << 12,
    Properties        = 1 << 13,
    All = Modifier | DismissButton | Icon | Title | Text | Shape | ContainerColor
        | IconContentColor | TitleContentColor | TextContentColor | TonalElevation | Properties,
}

// androidx.compose.material3.TextFieldKt.TextField (String overload) AND
// OutlinedTextFieldKt.OutlinedTextField (String overload): 23 user params,
// bit 0 = value, bit 1 = onValueChange (both provided).
[System.Flags]
internal enum TextFieldDefault
{
    None                 = 0,
    Modifier             = 1 << 2,
    Enabled              = 1 << 3,
    ReadOnly             = 1 << 4,
    TextStyle            = 1 << 5,
    Label                = 1 << 6,
    Placeholder          = 1 << 7,
    LeadingIcon          = 1 << 8,
    TrailingIcon         = 1 << 9,
    Prefix               = 1 << 10,
    Suffix               = 1 << 11,
    SupportingText       = 1 << 12,
    IsError              = 1 << 13,
    VisualTransformation = 1 << 14,
    KeyboardOptions      = 1 << 15,
    KeyboardActions      = 1 << 16,
    SingleLine           = 1 << 17,
    MaxLines             = 1 << 18,
    MinLines             = 1 << 19,
    InteractionSource    = 1 << 20,
    Shape                = 1 << 21,
    Colors               = 1 << 22,
    All = Modifier | Enabled | ReadOnly | TextStyle | Label | Placeholder | LeadingIcon
        | TrailingIcon | Prefix | Suffix | SupportingText | IsError | VisualTransformation
        | KeyboardOptions | KeyboardActions | SingleLine | MaxLines | MinLines
        | InteractionSource | Shape | Colors,
}
