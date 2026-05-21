using System.Collections;
using System.Collections.Generic;
using Androidx.Compose.Foundation.Layout;
using Androidx.Compose.Material3;
using Androidx.Compose.Runtime;

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

    public void Add(ComposableNode child) => _children.Add(child);
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
