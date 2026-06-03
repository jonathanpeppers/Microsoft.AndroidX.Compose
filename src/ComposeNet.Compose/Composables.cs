using System.Collections;
using System.Collections.Generic;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
namespace ComposeNet;

// ---- Leaf: Text ----

/// <summary>Material 3 <c>Text</c> composable.</summary>
public sealed class Text : ComposableNode
{
    readonly string _text;
    public Text(string text) => _text = text;
    internal override void Render(IComposer composer) =>
        ComposeBridges.Text(_text, composer);
}

// ---- Container base: provides Add + IEnumerable for collection-init ----

/// <summary>
/// Base class for container composables that take a single content
/// lambda holding zero or more children. Implements
/// <see cref="IEnumerable"/> + <see cref="Add(ComposableNode)"/> so
/// C# collection-initializer syntax
/// (<c>new Column { new Text("Hi"), new Text("There") }</c>) compiles.
/// </summary>
public abstract class ComposableContainer : ComposableNode, IEnumerable
{
    readonly List<ComposableNode> _children = new();

    public void Add(ComposableNode? child)
    {
        if (child is not null)
            _children.Add(child);
    }
    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    /// <summary>Internal accessor for <see cref="Render"/> impls.</summary>
    private protected IReadOnlyList<ComposableNode> Children => _children;

    /// <summary>
    /// Renders this container's children sequentially into
    /// <paramref name="composer"/>. Containers call this from inside
    /// their content lambda.
    /// </summary>
    private protected void RenderChildren(IComposer composer)
    {
        for (int i = 0; i < _children.Count; i++)
            _children[i].Render(composer);
    }
}

// ---- Column ----

/// <summary>Foundation <c>Column</c> composable.</summary>
public sealed class Column : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ColumnKt.Column(
            modifier:            null,
            verticalArrangement: null,
            horizontalAlignment: null,
            content:             content,
            _composer:           composer,
            p5:                  0,
            _changed:            (int)ColumnDefault.All);
    }
}

// ---- MaterialTheme ----

/// <summary>
/// Material 3 <c>MaterialTheme</c>. Uses the Android 12+ dynamic
/// light color scheme derived from the system wallpaper (Material You)
/// — gives Google's blue/teal baseline on stock emulators rather than
/// Compose's default purple.
/// </summary>
public sealed class MaterialTheme : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var scheme  = DynamicTonalPaletteKt.DynamicLightColorScheme(Android.App.Application.Context);
        var content = new ComposableLambda2(c => RenderChildren(c));
        MaterialThemeKt.MaterialTheme(
            colorScheme: scheme,
            shapes:      null,
            typography:  null,
            content:     content,
            _composer:   composer,
            p5:          0,
            _changed:    (int)(MaterialThemeDefault.All & ~MaterialThemeDefault.ColorScheme));
    }
}

// ---- Button ----

/// <summary>
/// Material 3 filled <c>Button</c>. Takes an <c>onClick</c> in its
/// constructor and uses collection-initializer syntax for content:
/// <code>new Button(onClick: () => count++) { new Text("Tap") }</code>
/// </summary>
public sealed class Button : ComposableContainer
{
    readonly System.Action _onClick;
    public Button(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.Button(click, content, composer);
    }
}

// ---- IconButton ----

/// <summary>
/// Material 3 <c>IconButton</c>. Children render into a Function2 content
/// slot (no RowScope). Typical use: <c>new IconButton(...) { new Text("☆") }</c>.
/// </summary>
public sealed class IconButton : ComposableContainer
{
    readonly System.Action _onClick;
    public IconButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.IconButton(click, content, composer);
    }
}

// ---- FloatingActionButton ----

/// <summary>Material 3 <c>FloatingActionButton</c>.</summary>
public sealed class FloatingActionButton : ComposableContainer
{
    readonly System.Action _onClick;
    public FloatingActionButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.FloatingActionButton(click, content, composer);
    }
}

// ---- Surface ----

/// <summary>
/// Material 3 non-interactive <c>Surface</c> — applies background color,
/// elevation, and clipping to its content.
/// </summary>
public sealed class Surface : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.Surface(content, composer);
    }
}

// ---- TextField / OutlinedTextField ----

/// <summary>
/// Material 3 <c>TextField</c> (filled variant). Bound to a
/// <see cref="MutableState{T}"/> of <see cref="string"/> so user edits
/// trigger recomposition.
/// </summary>
public sealed class TextField : ComposableNode
{
    readonly MutableState<string> _state;
    public TextField(MutableState<string> state) => _state = state;

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v => _state.Value = v?.ToString() ?? string.Empty);
        ComposeBridges.TextField(_state.Value ?? string.Empty, onChange, composer);
    }
}

/// <summary>
/// Material 3 <c>OutlinedTextField</c>. Same binding contract as
/// <see cref="TextField"/>.
/// </summary>
public sealed class OutlinedTextField : ComposableNode
{
    readonly MutableState<string> _state;
    public OutlinedTextField(MutableState<string> state) => _state = state;

    internal override void Render(IComposer composer)
    {
        var onChange = new ComposableLambda1(v => _state.Value = v?.ToString() ?? string.Empty);
        ComposeBridges.OutlinedTextField(_state.Value ?? string.Empty, onChange, composer);
    }
}

// ---- AlertDialog ----

/// <summary>
/// Material 3 <c>AlertDialog</c>. The first composable in the facade to
/// use <em>named slot properties</em> instead of a single collection
/// initializer: <see cref="ConfirmButton"/> is required, and any of
/// <see cref="DismissButton"/>, <see cref="Icon"/>, <see cref="Title"/>,
/// and <see cref="Text"/> may be supplied via C# object-initializer
/// syntax. Slots left <c>null</c> are reported as defaulted through the
/// <c>$default</c> bitmask so Compose substitutes the Kotlin defaults.
///
/// <code>
/// new AlertDialog(onDismissRequest: () => show.Value = false)
/// {
///     Title         = new Text("Confirm?"),
///     Text          = new Text("This cannot be undone."),
///     ConfirmButton = new Button(onClick: ...) { new Text("OK") },
///     DismissButton = new Button(onClick: ...) { new Text("Cancel") },
/// }
/// </code>
///
/// This shape is the template that <c>ModalBottomSheet</c>,
/// <c>DatePickerDialog</c>, <c>TimePickerDialog</c>, and the tooltip
/// composables will follow once their <c>*State</c> bridges land.
/// </summary>
public sealed class AlertDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public AlertDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Required: the affirmative-action button (typically <see cref="ComposeNet.Button"/>).</summary>
    public ComposableNode? ConfirmButton { get; set; }

    /// <summary>Optional: secondary button rendered alongside <see cref="ConfirmButton"/>.</summary>
    public ComposableNode? DismissButton { get; set; }

    /// <summary>Optional: leading icon shown above the title.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: dialog title.</summary>
    public ComposableNode? Title { get; set; }

    /// <summary>Optional: dialog body / supporting text.</summary>
    public ComposableNode? Text { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null)
            throw new System.InvalidOperationException(
                "AlertDialog.ConfirmButton is required (the Kotlin parameter has no default).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));

        ComposableLambda2? dismissBtn = DismissButton is null ? null
            : new ComposableLambda2(c => DismissButton.Render(c));
        ComposableLambda2? icon = Icon is null ? null
            : new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? title = Title is null ? null
            : new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? text = Text is null ? null
            : new ComposableLambda2(c => Text.Render(c));

        // Start from "default everything" and clear the bit for each
        // optional slot the user actually supplied.
        int defaults = (int)AlertDialogDefault.All;
        if (dismissBtn is not null) defaults &= ~(int)AlertDialogDefault.DismissButton;
        if (icon       is not null) defaults &= ~(int)AlertDialogDefault.Icon;
        if (title      is not null) defaults &= ~(int)AlertDialogDefault.Title;
        if (text       is not null) defaults &= ~(int)AlertDialogDefault.Text;

        ComposeBridges.AlertDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            dismissButton:    dismissBtn,
            icon:             icon,
            title:            title,
            text:             text,
            defaults:         defaults,
            composer:         composer);
    }
}

// ---- Card ----

/// <summary>
/// Material 3 non-clickable <c>Card</c> — a tonal surface with rounded
/// corners that lays its children out as a Column. Children are added
/// via collection-initializer syntax:
/// <code>
/// new Card { new Text("Title"), new Text("Subtitle") }
/// </code>
/// </summary>
public sealed class Card : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.Card(content, composer);
    }
}

// ---- Chip family ----

/// <summary>
/// Material 3 <c>AssistChip</c>. <see cref="Label"/> is required;
/// <see cref="LeadingIcon"/> and <see cref="TrailingIcon"/> are optional
/// slots:
/// <code>
/// new AssistChip(onClick: ...) { Label = new Text("Filter") }
/// </code>
/// </summary>
public sealed class AssistChip : ComposableNode
{
    readonly System.Action _onClick;
    public AssistChip(System.Action onClick) => _onClick = onClick;

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot (e.g. icon).</summary>
    public ComposableNode? LeadingIcon { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "AssistChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)AssistChipDefault.All;
        if (leading  is not null) defaults &= ~(int)AssistChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)AssistChipDefault.TrailingIcon;

        ComposeBridges.AssistChip(click, label, leading, trailing, defaults, composer);
    }
}

/// <summary>
/// Material 3 <c>FilterChip</c>. Renders as either selected or unselected;
/// the <c>onClick</c> handler typically toggles the bound boolean state.
/// </summary>
public sealed class FilterChip : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public FilterChip(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot (typically the check / unselected icon).</summary>
    public ComposableNode? LeadingIcon { get; set; }

    /// <summary>Optional: trailing slot.</summary>
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "FilterChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)FilterChipDefault.All;
        if (leading  is not null) defaults &= ~(int)FilterChipDefault.LeadingIcon;
        if (trailing is not null) defaults &= ~(int)FilterChipDefault.TrailingIcon;

        ComposeBridges.FilterChip(_selected, click, label, leading, trailing, defaults, composer);
    }
}

/// <summary>
/// Material 3 <c>InputChip</c>. Adds an <see cref="Avatar"/> slot in
/// addition to the leading/trailing slots common to the chip family.
/// </summary>
public sealed class InputChip : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public InputChip(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    public ComposableNode? LeadingIcon  { get; set; }
    public ComposableNode? Avatar       { get; set; }
    public ComposableNode? TrailingIcon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "InputChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? leading  = LeadingIcon  is null ? null : new ComposableLambda2(c => LeadingIcon.Render(c));
        ComposableLambda2? avatar   = Avatar       is null ? null : new ComposableLambda2(c => Avatar.Render(c));
        ComposableLambda2? trailing = TrailingIcon is null ? null : new ComposableLambda2(c => TrailingIcon.Render(c));

        int defaults = (int)InputChipDefault.All;
        if (leading  is not null) defaults &= ~(int)InputChipDefault.LeadingIcon;
        if (avatar   is not null) defaults &= ~(int)InputChipDefault.Avatar;
        if (trailing is not null) defaults &= ~(int)InputChipDefault.TrailingIcon;

        ComposeBridges.InputChip(_selected, click, label, leading, avatar, trailing, defaults, composer);
    }
}

/// <summary>
/// Material 3 <c>SuggestionChip</c>. Single-icon variant — only an
/// <see cref="Icon"/> slot is exposed (vs. AssistChip's leading + trailing).
/// </summary>
public sealed class SuggestionChip : ComposableNode
{
    readonly System.Action _onClick;
    public SuggestionChip(System.Action onClick) => _onClick = onClick;

    /// <summary>Required: chip text.</summary>
    public ComposableNode? Label { get; set; }

    /// <summary>Optional: leading slot.</summary>
    public ComposableNode? Icon { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Label is null)
            throw new System.InvalidOperationException(
                "SuggestionChip.Label is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var label = new ComposableLambda2(c => Label.Render(c));
        ComposableLambda2? icon = Icon is null ? null : new ComposableLambda2(c => Icon.Render(c));

        int defaults = (int)SuggestionChipDefault.All;
        if (icon is not null) defaults &= ~(int)SuggestionChipDefault.Icon;

        ComposeBridges.SuggestionChip(click, label, icon, defaults, composer);
    }
}

// ---- NavigationBar / NavigationBarItem ----

/// <summary>
/// Material 3 <c>NavigationBar</c>. Container for
/// <see cref="NavigationBarItem"/> children laid out horizontally:
/// <code>
/// new NavigationBar
/// {
///     new NavigationBarItem(selected: tab == 0, onClick: () =&gt; tab.Value = 0)
///     {
///         Icon = new Text("🏠"), Label = new Text("Home"),
///     },
///     new NavigationBarItem(selected: tab == 1, onClick: () =&gt; tab.Value = 1)
///     {
///         Icon = new Text("⚙"), Label = new Text("Settings"),
///     },
/// }
/// </code>
/// </summary>
public sealed class NavigationBar : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        // Capture the RowScope receiver (p0 of the Function3) and publish
        // it so child NavigationBarItems can pass it to their underlying
        // RowScope-extension static.
        var content = new ComposableLambda3((scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope);
            RenderChildren(c);
        });
        ComposeBridges.NavigationBar(content, composer);
    }
}

/// <summary>
/// Material 3 <c>NavigationBarItem</c>. Must be a child of
/// <see cref="NavigationBar"/> — the Kotlin static method takes a
/// <c>RowScope</c> extension receiver, which the parent
/// <see cref="NavigationBar"/> publishes via <c>RenderContext</c>.
/// <see cref="Icon"/> is required; <see cref="Label"/> is optional.
/// </summary>
public sealed class NavigationBarItem : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public NavigationBarItem(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: item icon.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: item label.</summary>
    public ComposableNode? Label { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Icon is null)
            throw new System.InvalidOperationException(
                "NavigationBarItem.Icon is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var icon  = new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? label = Label is null ? null : new ComposableLambda2(c => Label.Render(c));

        int defaults = (int)NavigationBarItemDefault.All;
        if (label is not null) defaults &= ~(int)NavigationBarItemDefault.Label;

        ComposeBridges.NavigationBarItem(
            rowScope: RenderContext.CurrentScope,
            selected: _selected,
            onClick:  click,
            icon:     icon,
            label:    label,
            defaults: defaults,
            composer: composer);
    }
}

// ---- NavigationRail / NavigationRailItem ----

/// <summary>
/// Material 3 <c>NavigationRail</c>. Vertical analog of
/// <see cref="NavigationBar"/>. Children are <see cref="NavigationRailItem"/>s:
/// <code>
/// new NavigationRail
/// {
///     new NavigationRailItem(selected: tab == 0, onClick: ...) { Icon = ..., Label = ... },
///     new NavigationRailItem(selected: tab == 1, onClick: ...) { Icon = ..., Label = ... },
/// }
/// </code>
/// </summary>
public sealed class NavigationRail : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        // NavigationRailItem (unlike NavigationBarItem) is a top-level
        // static, not a ColumnScope extension — so we don't need to
        // publish the scope. Children can render directly.
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.NavigationRail(content, composer);
    }
}

/// <summary>
/// Material 3 <c>NavigationRailItem</c>. Used inside
/// <see cref="NavigationRail"/>.
/// </summary>
public sealed class NavigationRailItem : ComposableNode
{
    readonly bool _selected;
    readonly System.Action _onClick;

    public NavigationRailItem(bool selected, System.Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Required: item icon.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: item label.</summary>
    public ComposableNode? Label { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Icon is null)
            throw new System.InvalidOperationException(
                "NavigationRailItem.Icon is required (the Kotlin parameter has no default).");

        var click = new ComposableLambda0(_onClick);
        var icon  = new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? label = Label is null ? null : new ComposableLambda2(c => Label.Render(c));

        int defaults = (int)NavigationRailItemDefault.All;
        if (label is not null) defaults &= ~(int)NavigationRailItemDefault.Label;

        ComposeBridges.NavigationRailItem(_selected, click, icon, label, defaults, composer);
    }
}
// ---- ModalBottomSheet ----

/// <summary>
/// Material 3 <c>ModalBottomSheet</c>. Opens a modal sheet anchored to
/// the bottom of the screen.
///
/// The <c>SheetState</c> is created inside <see cref="Render"/> via
/// <see cref="ModalBottomSheetKt.RememberModalBottomSheetState"/> — that
/// builder is NOT stripped from the binding, so we call it directly on
/// the bound C# method instead of going through JNI. Use the visibility
/// pattern from <see cref="AlertDialog"/>: gate the entire instance on
/// a <see cref="MutableState{T}"/> of <see cref="bool"/>.
///
/// <code>
/// var show = Remember(() => new MutableState&lt;bool&gt;(false));
/// show.Value
///     ? new ModalBottomSheet(onDismissRequest: () =&gt; show.Value = false)
///       {
///           new Column { new Text("Sheet contents") },
///       }
///     : null
/// </code>
/// </summary>
public sealed class ModalBottomSheet : ComposableContainer
{
    readonly System.Action _onDismissRequest;
    public ModalBottomSheet(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Optional drag handle drawn at the top of the sheet.</summary>
    public ComposableNode? DragHandle { get; set; }

    internal override void Render(IComposer composer)
    {
        // Bound C# call — RememberModalBottomSheetState is NOT stripped.
        // p3 is the (renamed) Composer parameter; _changed = 3 means bits
        // 0 and 1 (skipPartiallyExpanded, confirmValueChange) are
        // defaulted by Compose.
        var sheetState = ModalBottomSheetKt.RememberModalBottomSheetState(
            skipPartiallyExpanded: false,
            confirmValueChange:    null,
            _composer:             composer,
            p3:                    0,
            _changed:              3);

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var content   = new ComposableLambda3(c => RenderChildren(c));
        ComposableLambda2? dragHandle = DragHandle is null ? null
            : new ComposableLambda2(c => DragHandle.Render(c));

        int defaults = (int)ModalBottomSheetDefault.All;
        if (dragHandle is not null) defaults &= ~(int)ModalBottomSheetDefault.DragHandle;

        ComposeBridges.ModalBottomSheet(
            onDismissRequest: onDismiss,
            sheetState:       ((Java.Lang.Object)sheetState).Handle,
            dragHandle:       dragHandle,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}

// ---- BottomSheetScaffold ----

/// <summary>
/// Material 3 <c>BottomSheetScaffold</c>. Hosts a persistent bottom
/// sheet alongside a primary content area, plus optional top bar and
/// snackbar slots.
///
/// <see cref="BottomSheetScaffoldKt.RememberBottomSheetScaffoldState"/>
/// is called directly on the C# binding (NOT stripped), threading the
/// composer through.
/// </summary>
public sealed class BottomSheetScaffold : ComposableContainer
{
    /// <summary>Required: the persistent bottom-sheet content.</summary>
    public ComposableNode? SheetContent { get; set; }

    /// <summary>Optional: the sheet's drag handle.</summary>
    public ComposableNode? SheetDragHandle { get; set; }

    /// <summary>Optional: persistent top bar above the main content.</summary>
    public ComposableNode? TopBar { get; set; }

    internal override void Render(IComposer composer)
    {
        if (SheetContent is null)
            throw new System.InvalidOperationException(
                "BottomSheetScaffold.SheetContent is required.");

        // Bound C# call — RememberBottomSheetScaffoldState is NOT stripped.
        var scaffoldState = BottomSheetScaffoldKt.RememberBottomSheetScaffoldState(
            bottomSheetState:   null,
            snackbarHostState:  null,
            _composer:          composer,
            p3:                 0,
            _changed:           3);

        var sheet   = new ComposableLambda3(c => SheetContent.Render(c));
        var content = new ComposableLambda3(c => RenderChildren(c));

        ComposableLambda2? dragHandle = SheetDragHandle is null ? null
            : new ComposableLambda2(c => SheetDragHandle.Render(c));
        ComposableLambda2? topBar = TopBar is null ? null
            : new ComposableLambda2(c => TopBar.Render(c));

        int defaults = (int)BottomSheetScaffoldDefault.All;
        if (dragHandle is not null) defaults &= ~(int)BottomSheetScaffoldDefault.SheetDragHandle;
        if (topBar     is not null) defaults &= ~(int)BottomSheetScaffoldDefault.TopBar;

        ComposeBridges.BottomSheetScaffold(
            sheetContent:    sheet,
            scaffoldState:   ((Java.Lang.Object)scaffoldState).Handle,
            sheetDragHandle: dragHandle,
            topBar:          topBar,
            snackbarHost:    null,
            content:         content,
            defaults:        defaults,
            composer:        composer);
    }
}

// ---- DatePickerDialog ----

/// <summary>
/// Material 3 <c>DatePickerDialog</c>. The <c>DatePickerState</c>
/// builder (<c>rememberDatePickerState-EU0dCGE</c>) is mangled and
/// stripped from the binding, so we resolve it through JNI inside the
/// <see cref="DatePicker"/> facade. Place a <see cref="DatePicker"/>
/// (or any composable) inside <see cref="Body"/>.
/// </summary>
public sealed class DatePickerDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public DatePickerDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Required: the affirmative button (no Kotlin default).</summary>
    public ComposableNode? ConfirmButton { get; set; }

    /// <summary>Optional: secondary button.</summary>
    public ComposableNode? DismissButton { get; set; }

    /// <summary>Required: dialog body — typically a <see cref="DatePicker"/>.</summary>
    public ComposableNode? Body { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null)
            throw new System.InvalidOperationException(
                "DatePickerDialog.ConfirmButton is required.");
        if (Body is null)
            throw new System.InvalidOperationException(
                "DatePickerDialog.Body is required (the dialog's content slot).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));
        var content   = new ComposableLambda3(c => Body.Render(c));
        ComposableLambda2? dismiss = DismissButton is null ? null
            : new ComposableLambda2(c => DismissButton.Render(c));

        int defaults = (int)DatePickerDialogDefault.All;
        if (dismiss is not null) defaults &= ~(int)DatePickerDialogDefault.DismissButton;

        ComposeBridges.DatePickerDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            dismissButton:    dismiss,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}

// ---- DatePicker (the inline picker control used inside DatePickerDialog) ----

/// <summary>
/// Material 3 <c>DatePicker</c>. The Kotlin <c>DatePickerKt.DatePicker</c>
/// composable IS exposed by the binding, but its <c>$default</c> bitmask
/// param isn't user-visible (the binding drops the trailing
/// <c>$default</c> parameter on @Composable functions), so we go through
/// raw JNI to set it. Place inside <see cref="DatePickerDialog"/>'s body.
/// </summary>
public sealed class DatePicker : ComposableNode
{
    internal override void Render(IComposer composer)
    {
        var stateHandle = ComposeBridges.RememberDatePickerState(composer);
        ComposeBridges.DatePicker(stateHandle, composer);
    }
}

// ---- TimePicker ----

/// <summary>
/// Material 3 <c>TimePicker</c>. Resolves <c>TimePickerState</c> via
/// raw JNI (<c>rememberTimePickerState</c> takes a <see cref="IComposer"/>
/// so it requires the composer-aware bridge).
/// </summary>
public sealed class TimePicker : ComposableNode
{
    readonly int  _initialHour;
    readonly int  _initialMinute;
    readonly bool _is24Hour;
    public TimePicker(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
    {
        _initialHour   = initialHour;
        _initialMinute = initialMinute;
        _is24Hour      = is24Hour;
    }

    internal override void Render(IComposer composer)
    {
        var stateHandle = ComposeBridges.RememberTimePickerState(_initialHour, _initialMinute, _is24Hour, composer);
        ComposeBridges.TimePicker(stateHandle, (int)TimePickerDefault.All, composer);
    }
}

// ---- TimePickerDialog ----

/// <summary>
/// Material 3 <c>TimePickerDialog</c>. Both <c>ConfirmButton</c> and
/// <c>DismissButton</c> are required by Compose; <see cref="Title"/>
/// and <see cref="ModeToggleButton"/> are optional. Place a
/// <see cref="TimePicker"/> in the body.
/// </summary>
public sealed class TimePickerDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public TimePickerDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    public ComposableNode? ConfirmButton    { get; set; }
    public ComposableNode? DismissButton    { get; set; }
    public ComposableNode? Title            { get; set; }
    public ComposableNode? ModeToggleButton { get; set; }
    /// <summary>Required: dialog body — typically a <see cref="TimePicker"/>.</summary>
    public ComposableNode? Body             { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null || DismissButton is null)
            throw new System.InvalidOperationException(
                "TimePickerDialog.ConfirmButton and DismissButton are both required.");
        if (Body is null)
            throw new System.InvalidOperationException(
                "TimePickerDialog.Body is required (the dialog's content slot).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));
        var dismiss   = new ComposableLambda2(c => DismissButton.Render(c));
        var content   = new ComposableLambda3(c => Body.Render(c));
        ComposableLambda2? title = Title is null ? null
            : new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? toggle = ModeToggleButton is null ? null
            : new ComposableLambda2(c => ModeToggleButton.Render(c));

        int defaults = (int)TimePickerDialogDefault.All;
        if (title  is not null) defaults &= ~(int)TimePickerDialogDefault.Title;
        if (toggle is not null) defaults &= ~(int)TimePickerDialogDefault.ModeToggleButton;

        ComposeBridges.TimePickerDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            dismissButton:    dismiss,
            title:            title,
            modeToggleButton: toggle,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}

// ---- Tooltip (TooltipBox + plain styled tooltip) ----

/// <summary>
/// Material 3 <c>TooltipBox</c> with a plain tooltip popup. The popup
/// position provider comes from <c>TooltipDefaults.rememberPlainTooltipPositionProvider</c>;
/// <c>TooltipState</c> from <c>rememberTooltipState</c>. Both are
/// resolved via JNI inside <see cref="ComposeBridges"/>.
///
/// Use named-property syntax: <see cref="Tip"/> is the popup body shown
/// on long-press / hover, <see cref="Anchor"/> is the always-visible
/// content the popup attaches to.
/// </summary>
public sealed class Tooltip : ComposableNode
{
    readonly bool _isPersistent;
    public Tooltip(bool isPersistent = false) => _isPersistent = isPersistent;

    /// <summary>Required: the popup body shown on long-press / hover.</summary>
    public ComposableNode? Tip { get; set; }

    /// <summary>Required: the always-visible anchor the tooltip attaches to.</summary>
    public ComposableNode? Anchor { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Tip is null || Anchor is null)
            throw new System.InvalidOperationException(
                "Tooltip requires both Tip (popup body) and Anchor (visible content).");

        var positionProvider = ComposeBridges.RememberPlainTooltipPositionProvider(composer);
        var stateHandle      = ComposeBridges.RememberTooltipState(_isPersistent, composer);

        var tooltip = new ComposableLambda3(c => Tip.Render(c));
        var anchor  = new ComposableLambda2(c => Anchor.Render(c));

        ComposeBridges.TooltipBox(
            positionProvider: positionProvider,
            tooltip:          tooltip,
            state:            stateHandle,
            content:          anchor,
            defaults:         (int)TooltipBoxDefault.All,
            composer:         composer);
    }
}

// ---- (BasicTooltip intentionally not bound) ----
//
// `androidx.compose.material3.internal.BasicTooltipKt.BasicTooltipBox`
// is the foundation primitive that the public `TooltipBox` is built on
// top of. There's no user-facing scenario where it's preferable to
// `Tooltip` (the public wrapper we already bind), so it's skipped on
// purpose for the same reason as `BasicEdgeToEdgeDialog`.

// ---- (BasicEdgeToEdgeDialog intentionally not bound) ----
//
// `androidx.compose.material3.internal.BasicEdgeToEdgeDialog` is an
// internal implementation primitive that upstream `Dialog`,
// `AlertDialog`, `DatePickerDialog`, and `TimePickerDialog` are built
// on top of. It draws content edge-to-edge with no scrim, no insets,
// and no built-in dismiss handling — there is no user-facing scenario
// where it's preferable to one of the public dialog wrappers we
// already bind. Skipped on purpose.