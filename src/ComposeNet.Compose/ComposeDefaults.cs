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

using Androidx.Compose.Foundation.Layout;
using Androidx.Compose.Material3;
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
