using AndroidX.Compose;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
using ComposeFontWeight = AndroidX.Compose.FontWeight;
using ComposeImage = AndroidX.Compose.Image;
using ComposeText = AndroidX.Compose.Text;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Compose-backed logical handler for a MAUI
/// <see cref="ISwipeItemMenuItem"/>. The owning
/// <see cref="SwipeViewHandler"/> asks this handler for its composable
/// text/icon surface instead of creating an AppCompat button.
/// </summary>
public partial class SwipeItemMenuItemHandler :
    ElementHandler<ISwipeItemMenuItem, Java.Lang.Object>,
    ISwipeMenuItemNodeProvider
{
    /// <summary>Property mapper for the menu item's visual properties.</summary>
    public static IPropertyMapper<ISwipeItemMenuItem, SwipeItemMenuItemHandler> Mapper =
        new PropertyMapper<ISwipeItemMenuItem, SwipeItemMenuItemHandler>(ElementHandler.ElementMapper)
        {
            ["Visibility"]       = MapVisibility,
            ["Background"]       = MapBackground,
            ["Text"]             = MapText,
            ["TextColor"]        = MapTextColor,
            ["CharacterSpacing"] = MapCharacterSpacing,
            ["Font"]             = MapFont,
            ["Source"]           = MapSource,
        };

    /// <summary>Command mapper inheriting standard element commands.</summary>
    public static CommandMapper<ISwipeItemMenuItem, SwipeItemMenuItemHandler> CommandMapper =
        new(ElementCommandMapper);

    readonly MutableState<string> _text = new(string.Empty);
    readonly MutableState<long?> _background = new((long?)null);
    readonly MutableState<long?> _textColor = new((long?)null);
    readonly MutableState<int?> _fontSize = new((int?)null);
    readonly MutableState<bool> _bold = new(false);
    readonly MutableState<float?> _characterSpacing = new((float?)null);
    readonly MutableState<int> _visibilityVersion = new(0);
    ImageSourceLoader? _loader;

    /// <summary>Construct a handler with the default mappers.</summary>
    public SwipeItemMenuItemHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SwipeItemMenuItemHandler(
        IPropertyMapper? mapper,
        CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    ImageSourceLoader Loader =>
        _loader ??= new ImageSourceLoader(this, () => VirtualView);

    /// <inheritdoc/>
    protected override Java.Lang.Object CreatePlatformElement()
    {
        var context = MauiContext?.Context
            ?? throw new InvalidOperationException(
                "MauiContext not set on SwipeItemMenuItemHandler.");
        return new global::Android.Views.View(context);
    }

    ComposableNode ISwipeMenuItemNodeProvider.BuildSwipeItemNode()
    {
        _ = _visibilityVersion.Value;

        var column = new Column(
            verticalArrangement: Arrangement.Center,
            horizontalAlignment: Alignment.Horizontal.CenterHorizontally);

        if (_loader is { } loader)
        {
            if (loader.Painter.Value is { } painter)
                column.Add(new ComposeImage(painter) { Modifier = Modifier.Size(24) });
            else if (loader.DrawableResourceId.Value is int id)
                column.Add(new ComposeImage(id) { Modifier = Modifier.Size(24) });
        }

        if (!string.IsNullOrEmpty(_text.Value))
        {
            column.Add(new ComposeText(_text.Value)
            {
                Color = _textColor.Value is long packed
                    ? ComposeColor.FromPacked(packed)
                    : null,
                FontSize = _fontSize.Value is int size ? new Sp(size) : null,
                FontWeight = _bold.Value ? ComposeFontWeight.Bold : null,
                LetterSpacing = _characterSpacing.Value is float spacing
                    ? new Sp(1) * spacing
                    : null,
            });
        }

        Modifier modifier = Modifier.Companion.FillMaxSize();
        if (_background.Value is long background)
            modifier = modifier.Background(ComposeColor.FromPacked(background));
        column.Modifier = modifier;
        return column;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(Java.Lang.Object platformView)
    {
        _loader?.Reset();
        base.DisconnectHandler(platformView);
    }

    /// <summary>Map item visibility.</summary>
    public static void MapVisibility(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem _) =>
        handler._visibilityVersion.Value++;

    /// <summary>Map the item's background paint.</summary>
    public static void MapBackground(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item) =>
        SetColors(handler, item);

    /// <summary>Map the item label.</summary>
    public static void MapText(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item) =>
        handler._text.Value = item.Text ?? string.Empty;

    /// <summary>Map the item text color.</summary>
    public static void MapTextColor(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item) =>
        SetColors(handler, item);

    static void SetColors(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item)
    {
        var background = (item.Background as SolidPaint)?.Color;
        handler._background.Value = ColorMapping.ToPackedLong(background);
        var textColor = item.TextColor;
        if (textColor is null && background is not null)
        {
            float luminance =
                background.Red * 0.299f +
                background.Green * 0.587f +
                background.Blue * 0.114f;
            textColor = luminance > 0.5f
                ? Microsoft.Maui.Graphics.Colors.Black
                : Microsoft.Maui.Graphics.Colors.White;
        }
        handler._textColor.Value =
            ColorMapping.ToPackedLong(textColor);
    }

    /// <summary>Map the item character spacing.</summary>
    public static void MapCharacterSpacing(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item) =>
        handler._characterSpacing.Value = item.CharacterSpacing == 0
            ? null
            : (float)item.CharacterSpacing;

    /// <summary>Map the item font size and weight.</summary>
    public static void MapFont(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item)
    {
        var font = item.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value =
            (font.Weight & Microsoft.Maui.FontWeight.Bold) == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>Resolve the item icon through MAUI's image-source pipeline.</summary>
    public static async void MapSource(
        SwipeItemMenuItemHandler handler,
        ISwipeItemMenuItem item) =>
        await handler.Loader.LoadAsync(item.Source).ConfigureAwait(false);
}
