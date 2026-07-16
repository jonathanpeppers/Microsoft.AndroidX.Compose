using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
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
            value => new Tier2InlineContent(_ => content(value)))
        {
            Modifier = modifier,
        }.Render();
    }

    /// <summary>Animates between typed content states with an explicit composer.</summary>
    [Composable]
    public static void AnimatedContent<T>(
        IComposer composer,
        T targetState,
        [ComposableContent] Action<T, IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.AnimatedContent<T>(
            targetState,
            value => new Tier2InlineContent(c => content(value, c)))
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
            value => new Tier2InlineContent(_ => content(value)))
        {
            Modifier = modifier,
        }.Render();
    }

    /// <summary>Crossfades between typed content states with an explicit composer.</summary>
    [Composable]
    public static void Crossfade<T>(
        IComposer composer,
        T targetState,
        [ComposableContent] Action<T, IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Crossfade<T>(
            targetState,
            value => new Tier2InlineContent(c => content(value, c)))
        {
            Modifier = modifier,
        }.Render(composer);
    }
}
