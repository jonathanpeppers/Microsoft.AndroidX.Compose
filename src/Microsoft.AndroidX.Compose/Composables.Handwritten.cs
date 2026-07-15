using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a Material theme with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void MaterialTheme(
        IComposer composer,
        [ComposableContent] Action<IComposer> content,
        ColorScheme? colorScheme = null,
        bool dynamicColor = true,
        bool? darkTheme = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        var theme = new global::AndroidX.Compose.MaterialTheme
        {
            ColorScheme = colorScheme,
            UseDynamicColor = dynamicColor,
            Dark = darkTheme,
        };
        theme.Add(new Tier2InlineContent(content));
        theme.Render(composer);
    }

    /// <summary>
    /// Renders a Material scaffold with padding-aware body content and an
    /// explicit composer.
    /// </summary>
    [Composable, GenerateImplicitComposable]
    public static void Scaffold(
        IComposer composer,
        [ComposableContent] Action<PaddingValues, IComposer> content,
        Modifier? modifier = null,
        [ComposableContent] Action<IComposer>? topBar = null,
        [ComposableContent] Action<IComposer>? bottomBar = null,
        [ComposableContent] Action<IComposer>? snackbarHost = null,
        [ComposableContent] Action<IComposer>? floatingActionButton = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(content);

        new global::AndroidX.Compose.Scaffold
        {
            Modifier = modifier,
            BodyContent = padding =>
                new Tier2InlineContent(c => content(padding, c)),
            TopBar = topBar is null ? null : new Tier2InlineContent(topBar),
            BottomBar = bottomBar is null
                ? null
                : new Tier2InlineContent(bottomBar),
            SnackbarHost = snackbarHost is null
                ? null
                : new Tier2InlineContent(snackbarHost),
            FloatingActionButton = floatingActionButton is null
                ? null
                : new Tier2InlineContent(floatingActionButton),
        }.Render(composer);
    }

    /// <summary>Renders a Material snackbar host with an explicit composer.</summary>
    [Composable, GenerateImplicitComposable]
    public static void SnackbarHost(
        IComposer composer,
        SnackbarHostState state,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(state);

        new global::AndroidX.Compose.SnackbarHost(state)
        {
            Modifier = modifier,
        }.Render(composer);
    }

    /// <summary>
    /// Renders a low-level custom layout with an explicit composer.
    /// </summary>
    [Composable, GenerateImplicitComposable]
    public static void Layout(
        IComposer composer,
        Func<MeasureScope, IReadOnlyList<Measurable>, Constraints, MeasureResult>
            measurePolicy,
        [ComposableContent] Action<IComposer> content,
        Modifier? modifier = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(measurePolicy);
        ArgumentNullException.ThrowIfNull(content);

        var layout = new global::AndroidX.Compose.Layout(measurePolicy)
        {
            Modifier = modifier,
        };
        layout.Add(new Tier2InlineContent(content));
        layout.Render(composer);
    }

    /// <summary>
    /// Renders a single-choice segmented button at
    /// <paramref name="index"/> within <paramref name="count"/> row items
    /// with an explicit composer.
    /// </summary>
    [Composable, GenerateImplicitComposable]
    public static void SegmentedButton(
        IComposer composer,
        int index,
        int count,
        bool selected,
        Action onClick,
        [ComposableContent] Action<IComposer> label,
        Modifier? modifier = null,
        [ComposableContent] Action<IComposer>? icon = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(onClick);
        ArgumentNullException.ThrowIfNull(label);
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                "Segmented button index must be within the row's child count.");

        var button = new global::AndroidX.Compose.SegmentedButton(
            selected,
            onClick)
        {
            Modifier = modifier,
            Icon = icon is null ? null : new Tier2InlineContent(icon),
        };
        button.Add(new Tier2InlineContent(label));
        using var row = RenderContext.PushRow(count);
        row.SetIndex(index);
        button.Render(composer);
    }

    /// <summary>
    /// Renders a multi-choice segmented button at
    /// <paramref name="index"/> within <paramref name="count"/> row items
    /// with an explicit composer.
    /// </summary>
    [Composable, GenerateImplicitComposable]
    public static void SegmentedButton(
        IComposer composer,
        int index,
        int count,
        bool @checked,
        Action<bool> onCheckedChange,
        [ComposableContent] Action<IComposer> label,
        Modifier? modifier = null,
        [ComposableContent] Action<IComposer>? icon = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(onCheckedChange);
        ArgumentNullException.ThrowIfNull(label);
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                "Segmented button index must be within the row's child count.");

        var button = new global::AndroidX.Compose.SegmentedButton(
            @checked,
            onCheckedChange)
        {
            Modifier = modifier,
            Icon = icon is null ? null : new Tier2InlineContent(icon),
        };
        button.Add(new Tier2InlineContent(label));
        using var row = RenderContext.PushRow(count);
        row.SetIndex(index);
        button.Render(composer);
    }
}
