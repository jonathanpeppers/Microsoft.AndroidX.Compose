using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using Microsoft.Maui.Handlers;
using ComposeButton = AndroidX.Compose.Button;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Button"/> handler that renders
/// through Jetpack Compose's Material 3 <c>Button</c> composable. Replaces
/// MAUI's stock <c>MaterialButton</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// The Compose <c>onClick</c> lambda forwards to <see cref="IButton.Clicked"/>
/// so MAUI's standard <c>Clicked</c> event and bound <c>Command</c> fire
/// as expected.
/// </remarks>
public partial class ButtonHandler : ViewHandler<IButton, ComposeView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IButton"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    /// <remarks>
    /// <para>Typed against this concrete handler (not <c>IButtonHandler</c>)
    /// because <see cref="PropertyMapper{TVirtualView, TViewHandler}"/> casts
    /// the handler arg of every mapper callback to <c>TViewHandler</c>, and
    /// this class doesn't implement the stock MAUI <c>IButtonHandler</c>
    /// interface.</para>
    ///
    /// <para>Overrides <c>IView.Background</c> with a no-op: Material 3's
    /// <c>Button</c> paints its own pill-shaped surface, so letting MAUI
    /// also paint a <c>SolidColorBrush</c> on the outer <c>ComposeView</c>
    /// produces a double-background stack (a wide rectangle behind the
    /// pill). Compose owns the surface for buttons.</para>
    /// </remarks>
    public static IPropertyMapper<IButton, ButtonHandler> Mapper =
        new PropertyMapper<IButton, ButtonHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)]                      = MapText,
            [nameof(IView.Background)]                = SuppressBackground,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IButton, ButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<string> _text      = new(string.Empty);
    readonly MutableState<bool>   _fillWidth = new(false);

    /// <summary>Construct a handler with the default mappers.</summary>
    public ButtonHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ButtonHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(_ =>
        {
            var button = new ComposeButton(onClick: OnClicked)
            {
                new Text(_text.Value),
            };
            if (_fillWidth.Value)
                button.PrependModifier(Modifier.FillMaxWidth());
            return button;
        });
        return view;
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    void OnClicked()
    {
        // Stock MAUI ButtonHandler raises Pressed → Clicked → Released in
        // touch-down/-up order; Compose only surfaces a logical click here.
        // Fire all three so any caller subscribing to Pressed/Released
        // (e.g. behaviors, gesture recognizers) still observes them.
        var virtualView = VirtualView;
        if (virtualView is null) return;
        virtualView.Pressed();
        virtualView.Clicked();
        virtualView.Released();
    }

    /// <summary>Map <see cref="IText.Text"/> to the Compose text slot.</summary>
    public static void MapText(ButtonHandler handler, IButton button)
    {
        if (button is IText text)
            handler._text.Value = text.Text ?? string.Empty;
    }

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the button asks to fill its
    /// slot. Compose Material 3 <c>Button</c> hugs its content by
    /// default, so without this a button with <c>HorizontalOptions="Fill"</c>
    /// would render as a small pill on the left edge.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(ButtonHandler handler, IButton button) =>
        handler._fillWidth.Value = button.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

    static void SuppressBackground(ButtonHandler handler, IButton button)
    {
        // Compose Material 3 Button paints its own surface; do not let
        // MAUI paint a SolidColorBrush on the outer ComposeView too.
    }
}
