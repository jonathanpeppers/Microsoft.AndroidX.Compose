using Microsoft.Maui.Controls.Shapes;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Modifiers demo — exercises every cross-cutting <see cref="IView"/>
/// property the <c>ModifierBridge</c> translates into Compose
/// (<c>IsVisible</c>, <c>Opacity</c>, <c>TranslationX</c>,
/// <c>Scale</c>, <c>Rotation</c>, <c>Clip</c>, <c>Shadow</c>) by
/// flipping each one on a single shared <see cref="Image"/>. Every
/// property change reaches Compose via <c>RemapForCompose</c>'s
/// version-counter bump on <c>ViewHandler.ViewMapper</c>.
/// </summary>
public partial class ModifiersPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public ModifiersPage() => InitializeComponent();

    void OnToggleOpacity(object? sender, EventArgs e)
    {
        Target.Opacity = Math.Abs(Target.Opacity - 1.0) < 0.01 ? 0.4 : 1.0;
        OpacityBtn.Text = $"Opacity: {Target.Opacity:F2}";
    }

    void OnToggleScale(object? sender, EventArgs e)
    {
        Target.Scale = Math.Abs(Target.Scale - 1.0) < 0.01 ? 1.5 : 1.0;
        ScaleBtn.Text = $"Scale: {Target.Scale:F2}";
    }

    void OnToggleRotation(object? sender, EventArgs e)
    {
        Target.Rotation = Target.Rotation == 0 ? 45 : 0;
        RotationBtn.Text = $"Rotation: {Target.Rotation:F0}°";
    }

    void OnToggleVisibility(object? sender, EventArgs e)
    {
        Target.IsVisible = !Target.IsVisible;
        VisibilityBtn.Text = $"IsVisible: {Target.IsVisible}";
    }

    void OnToggleTranslation(object? sender, EventArgs e)
    {
        Target.TranslationX = Target.TranslationX == 0 ? 60 : 0;
        TranslationBtn.Text = $"TranslationX: {Target.TranslationX:F0}";
    }

    void OnToggleClip(object? sender, EventArgs e)
    {
        // Cycle: none → rounded → ellipse → none.
        if (Target.Clip is null)
        {
            Target.Clip = new RoundRectangleGeometry
            {
                CornerRadius = new CornerRadius(40),
                Rect = new Rect(0, 0, 1, 1),
            };
            ClipBtn.Text = "Clip: rounded 40dp";
        }
        else if (Target.Clip is RoundRectangleGeometry)
        {
            Target.Clip = new EllipseGeometry
            {
                RadiusX = 0.5,
                RadiusY = 0.5,
                Center  = new Point(0.5, 0.5),
            };
            ClipBtn.Text = "Clip: circle";
        }
        else
        {
            Target.Clip = null;
            ClipBtn.Text = "Clip: none";
        }
    }

    void OnToggleShadow(object? sender, EventArgs e)
    {
        if (Target.Shadow is null)
        {
            Target.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Radius = 16,
                Opacity = 0.7f,
                Offset = new Point(0, 6),
            };
            ShadowBtn.Text = "Shadow: 16dp";
        }
        else
        {
            Target.Shadow = null!;
            ShadowBtn.Text = "Shadow: off";
        }
    }

    void OnResetAll(object? sender, EventArgs e)
    {
        Target.Opacity     = 1.0;
        Target.Scale       = 1.0;
        Target.Rotation    = 0;
        Target.IsVisible   = true;
        Target.TranslationX = 0;
        Target.Clip        = null!;
        Target.Shadow      = null!;

        OpacityBtn.Text     = "Opacity: 1.00";
        ScaleBtn.Text       = "Scale: 1.00";
        RotationBtn.Text    = "Rotation: 0°";
        VisibilityBtn.Text  = "IsVisible: True";
        TranslationBtn.Text = "TranslationX: 0";
        ClipBtn.Text        = "Clip: none";
        ShadowBtn.Text      = "Shadow: off";
    }
}
