namespace ComposeNet;

// Compose @Composable functions take a trailing `int $default` parameter:
// a bitmask where bit N == 1 means "param N was NOT provided by the
// caller; substitute the default". (See the Kotlin compiler plugin's
// `DefaultParameterTransformer`.) The binding generator surfaces this
// parameter with the unhelpful name `_changed` for some Kotlin-mangled
// signatures and `_default` for others — either way, it's a raw int
// without any type to tell us which bit means which parameter.
//
// These [Flags] enums name each bit, one enum per composable call we
// make from C#, so call sites read like
//
//     _changed: (int)(ColumnDefault.Modifier
//                   | ColumnDefault.VerticalArrangement
//                   | ColumnDefault.HorizontalAlignment)
//
// instead of `_changed: 0b0111`.

[System.Flags]
internal enum ColumnDefault
{
    None                = 0,
    Modifier            = 1 << 0,
    VerticalArrangement = 1 << 1,
    HorizontalAlignment = 1 << 2,
    // bit 3 = content, but we always provide content so never set it.
}

[System.Flags]
internal enum MaterialThemeDefault
{
    None        = 0,
    ColorScheme = 1 << 0,
    Shapes      = 1 << 1,
    Typography  = 1 << 2,
    // bit 3 = content, always provided.
}

// androidx.compose.material3.ButtonKt.Button: 10 user params,
// bit 0 = onClick, bit 9 = content (both always provided).
[System.Flags]
internal enum ButtonDefault
{
    None               = 0,
    Modifier           = 1 << 1,
    Enabled            = 1 << 2,
    Shape              = 1 << 3,
    Colors             = 1 << 4,
    Elevation          = 1 << 5,
    Border             = 1 << 6,
    ContentPadding     = 1 << 7,
    InteractionSource  = 1 << 8,
}

// androidx.compose.material3.TextKt.Text--4IGK_g: 17 user params,
// bit 0 = text (always provided). Spread across two `$changed`-style
// ints in the bytecode but the $default bitmask is one 17-bit int.
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
}
