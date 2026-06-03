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
