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
// The other enums are *also* generated, but from the declarative
// `[ComposeDefaults]` overload. Their Kotlin overloads with the trailing
// $default param are stripped from the binding (mangled JVM names like
// `Text--4IGK_g` from inline classes such as `Color`/`TextUnit`/`Dp`),
// so there is no IMethodSymbol for the generator to introspect — we
// hand it the Kotlin parameter names instead. Names prefixed with `!`
// consume a bit position but don't emit an enum member (e.g. params
// the caller always provides). For extension-receiver functions
// (NavigationBarItem takes a RowScope receiver), the receiver is NOT
// part of the $default bitmask — start the name list at the first
// user-facing parameter.
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

// androidx.compose.material3.ModalBottomSheet_androidKt.ModalBottomSheet-dYc4hso:
// 13 user params; bits 0 (onDismissRequest), 2 (sheetState), 12 (content) always provided.
[assembly: ComposeDefaults("ModalBottomSheetDefault",
    "!onDismissRequest", "modifier", "!sheetState", "sheetMaxWidth", "shape",
    "containerColor", "contentColor", "tonalElevation", "scrimColor", "dragHandle",
    "windowInsets", "properties", "!content")]

// androidx.compose.material3.BottomSheetScaffoldKt.BottomSheetScaffold-sdMYb0k:
// 17 user params; bits 0 (sheetContent), 2 (scaffoldState), 16 (content) always provided.
[assembly: ComposeDefaults("BottomSheetScaffoldDefault",
    "!sheetContent", "modifier", "!scaffoldState", "sheetPeekHeight", "sheetMaxWidth",
    "sheetShape", "sheetContainerColor", "sheetContentColor", "sheetTonalElevation",
    "sheetShadowElevation", "sheetDragHandle", "sheetSwipeEnabled", "topBar",
    "snackbarHost", "containerColor", "contentColor", "!content")]

// androidx.compose.material3.DatePickerDialog_androidKt.DatePickerDialog-GmEhDVc:
// 9 user params; bits 0 (onDismissRequest), 1 (confirmButton), 8 (content) always provided.
[assembly: ComposeDefaults("DatePickerDialogDefault",
    "!onDismissRequest", "!confirmButton", "modifier", "dismissButton", "shape",
    "tonalElevation", "colors", "properties", "!content")]

// androidx.compose.material3.TimePickerKt.TimePicker-mT9BvqQ:
// 4 user params; bit 0 (state) always provided.
[assembly: ComposeDefaults("TimePickerDefault",
    "!state", "modifier", "colors", "layoutType")]

// androidx.compose.material3.TimePickerDialogKt.TimePickerDialog-FItCLgY:
// 10 user params; bits 0 (onDismissRequest), 1 (confirmButton),
// 2 (dismissButton), 9 (content) always provided.
[assembly: ComposeDefaults("TimePickerDialogDefault",
    "!onDismissRequest", "!confirmButton", "!dismissButton", "modifier", "properties",
    "title", "modeToggleButton", "shape", "containerColor", "!content")]

// androidx.compose.material3.TooltipKt.TooltipBox (7-param overload):
// 7 user params; bits 0 (positionProvider), 1 (tooltip), 2 (state),
// 6 (content) always provided.
[assembly: ComposeDefaults("TooltipBoxDefault",
    "!positionProvider", "!tooltip", "!state", "modifier", "focusable",
    "enableUserInput", "!content")]

// androidx.compose.material3.TextFieldKt.TextField (String overload) AND
// OutlinedTextFieldKt.OutlinedTextField (String overload): 23 user params,
// bit 0 = value, bit 1 = onValueChange (both provided).
[assembly: ComposeDefaults("TextFieldDefault",
    "!value", "!onValueChange", "modifier", "enabled", "readOnly",
    "textStyle", "label", "placeholder", "leadingIcon", "trailingIcon",
    "prefix", "suffix", "supportingText", "isError", "visualTransformation",
    "keyboardOptions", "keyboardActions", "singleLine", "maxLines", "minLines",
    "interactionSource", "shape", "colors")]

// androidx.compose.material3.CardKt.Card (non-clickable): 6 user params,
// bit 5 = content provided.
[assembly: ComposeDefaults("CardDefault",
    "modifier", "shape", "colors", "elevation", "border", "!content")]

// androidx.compose.material3.ChipKt.AssistChip: 11 user params,
// bit 0 = onClick, bit 1 = label (both always provided).
// Optional slot bits 4 (LeadingIcon) and 5 (TrailingIcon) are toggled
// per-call by AssistChip.Render.
[assembly: ComposeDefaults("AssistChipDefault",
    "!onClick", "!label", "modifier", "enabled", "leadingIcon", "trailingIcon",
    "shape", "colors", "elevation", "border", "interactionSource")]

// androidx.compose.material3.ChipKt.FilterChip: 12 user params,
// bits 0 = selected, 1 = onClick, 2 = label (all always provided).
[assembly: ComposeDefaults("FilterChipDefault",
    "!selected", "!onClick", "!label", "modifier", "enabled",
    "leadingIcon", "trailingIcon", "shape", "colors", "elevation",
    "border", "interactionSource")]

// androidx.compose.material3.ChipKt.InputChip: 13 user params,
// bits 0 = selected, 1 = onClick, 2 = label (all always provided).
[assembly: ComposeDefaults("InputChipDefault",
    "!selected", "!onClick", "!label", "modifier", "enabled",
    "leadingIcon", "avatar", "trailingIcon", "shape", "colors",
    "elevation", "border", "interactionSource")]

// androidx.compose.material3.ChipKt.SuggestionChip: 10 user params,
// bit 0 = onClick, bit 1 = label (both always provided).
[assembly: ComposeDefaults("SuggestionChipDefault",
    "!onClick", "!label", "modifier", "enabled", "icon",
    "shape", "colors", "elevation", "border", "interactionSource")]

// androidx.compose.material3.NavigationBarKt.NavigationBar-HsRjFd4:
// 6 user params, bit 5 = content provided.
[assembly: ComposeDefaults("NavigationBarDefault",
    "modifier", "containerColor", "contentColor", "tonalElevation",
    "windowInsets", "!content")]

// androidx.compose.material3.NavigationBarKt.NavigationBarItem: 9 user
// params after the RowScope receiver (the receiver is not part of the
// $default bitmask). Bits 0 = selected, 1 = onClick, 2 = icon (all
// always provided). The optional Label slot is toggled by
// NavigationBarItem.Render.
[assembly: ComposeDefaults("NavigationBarItemDefault",
    "!selected", "!onClick", "!icon", "modifier", "enabled", "label",
    "alwaysShowLabel", "colors", "interactionSource")]

// androidx.compose.material3.NavigationRailKt.NavigationRail-qi6gXK8:
// 6 user params, bit 5 = content provided.
[assembly: ComposeDefaults("NavigationRailDefault",
    "modifier", "containerColor", "contentColor", "header",
    "windowInsets", "!content")]

// androidx.compose.material3.NavigationRailKt.NavigationRailItem:
// 9 user params; bits 0 = selected, 1 = onClick, 2 = icon
// (all always provided).
[assembly: ComposeDefaults("NavigationRailItemDefault",
    "!selected", "!onClick", "!icon", "modifier", "enabled", "label",
    "alwaysShowLabel", "colors", "interactionSource")]
