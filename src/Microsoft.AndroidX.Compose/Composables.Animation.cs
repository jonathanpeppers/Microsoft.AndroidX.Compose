using AndroidX.Compose.Runtime;
using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Remembers a native typed transition and updates its target in the implicit composition.</summary>
    public static Transition<T> UpdateTransition<T>(
        T targetState,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") where T : notnull =>
        ComposeExtensions.UpdateTransition(ComposableContext.Current, targetState, label, line, file);

    /// <summary>
    /// Animates visibility with an explicit composer. Children may use
    /// <see cref="Modifier.AnimateEnterExit"/> for independent transitions.
    /// Null enter/exit values preserve the parent's Kotlin defaults.
    /// </summary>
    [Composable, GenerateImplicitComposable]
    public static void AnimatedVisibility(
        IComposer composer,
        bool visible,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null,
        Animation.EnterTransition? enter = null,
        Animation.ExitTransition? exit = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);
        var node = new global::AndroidX.Compose.AnimatedVisibility(visible, enter, exit)
        {
            Modifier = modifier,
        };
        node.Add(new ComposableContentNode(content));
        node.Render(composer);
    }

    /// <summary>Animates between typed content states in the implicit composition.</summary>
    [Composable]
    public static void AnimatedContent<T>(
        T targetState,
        [ComposableContent] Action<T> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.AnimatedContent<T>(
            targetState,
            value => new ComposableContentNode(_ => content(value)))
        {
            Modifier = modifier,
        }.Render();
    }

    /// <summary>Animates between typed content states with an explicit composer.</summary>
    [Composable]
    internal static void AnimatedContent<T>(
        IComposer composer,
        T targetState,
        [ComposableContent] Action<T, IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.AnimatedContent<T>(
            targetState,
            value => new ComposableContentNode(c => content(value, c)))
        {
            Modifier = modifier,
        }.Render(composer);
    }

    /// <summary>Crossfades between typed content states in the implicit composition.</summary>
    [Composable]
    public static void Crossfade<T>(
        T targetState,
        [ComposableContent] Action<T> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Crossfade<T>(
            targetState,
            value => new ComposableContentNode(_ => content(value)))
        {
            Modifier = modifier,
        }.Render();
    }

    /// <summary>Crossfades between typed content states with an explicit composer.</summary>
    [Composable]
    internal static void Crossfade<T>(
        IComposer composer,
        T targetState,
        [ComposableContent] Action<T, IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Crossfade<T>(
            targetState,
            value => new ComposableContentNode(c => content(value, c)))
        {
            Modifier = modifier,
        }.Render(composer);
    }
}
