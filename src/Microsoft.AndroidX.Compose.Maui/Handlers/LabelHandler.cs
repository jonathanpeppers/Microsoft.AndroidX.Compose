using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
using ComposeText = AndroidX.Compose.Text;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Label"/> handler that renders
/// through Jetpack Compose's <c>Text</c> composable. Replaces MAUI's stock
/// <c>AppCompatTextView</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// Each instance owns a <see cref="ComposeView"/> hosting one composition.
/// The composition reads two <see cref="MutableState{T}"/> slots
/// (text and optional packed color) so MAUI property changes propagate
/// through the standard mapper pipeline and trigger recomposition on the
/// next frame without rebuilding the view.
/// </remarks>
public partial class LabelHandler : ViewHandler<ILabel, ComposeView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ILabel"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ILabel, ILabelHandler> Mapper =
        new PropertyMapper<ILabel, ILabelHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]           = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ILabel, ILabelHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text  = new(string.Empty);
    readonly MutableState<long?>  _color = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public LabelHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public LabelHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(_ =>
        {
            var packed = _color.Value;
            return new ComposeText(_text.Value)
            {
                Color = packed.HasValue ? new ComposeColor(packed.Value) : null,
            };
        });
        return view;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose text slot.</summary>
    public static void MapText(ILabelHandler handler, ILabel label)
    {
        if (handler is LabelHandler self)
            self._text.Value = label.Text ?? string.Empty;
    }

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose color slot.</summary>
    public static void MapTextColor(ILabelHandler handler, ILabel label)
    {
        if (handler is LabelHandler self)
            self._color.Value = label.TextColor is { } c
                ? unchecked((long)ToPackedColor(c))
                : null;
    }

    static ulong ToPackedColor(Microsoft.Maui.Graphics.Color c)
    {
        byte a = (byte)(Math.Clamp(c.Alpha, 0f, 1f) * 255f);
        byte r = (byte)(Math.Clamp(c.Red,   0f, 1f) * 255f);
        byte g = (byte)(Math.Clamp(c.Green, 0f, 1f) * 255f);
        byte b = (byte)(Math.Clamp(c.Blue,  0f, 1f) * 255f);
        uint argb = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        return (ulong)argb << 32;
    }
}
