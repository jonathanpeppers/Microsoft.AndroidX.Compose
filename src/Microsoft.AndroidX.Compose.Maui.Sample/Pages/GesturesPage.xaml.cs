namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Gestures demo — exercises the
/// <see cref="Microsoft.AndroidX.Compose.Maui.Platform.GestureBridge"/>
/// shipped in Phase 2 Slice 10. Every recognizer attaches to a
/// Compose-folded leaf so its fire callback rides Compose's pointer
/// input pipeline (<c>Modifier.PointerInput</c>) instead of MAUI's
/// detached-platform-view <c>Touch</c> event.
/// </summary>
/// <remarks>
/// <para>The echo labels are updated from each recognizer's event so
/// the fire side of the round-trip is visible on-device — no
/// dev-tools needed.</para>
///
/// <para><see cref="Microsoft.Maui.Controls.PanGestureRecognizer"/>'s
/// <c>PanUpdated</c> reports cumulative deltas from the start of the
/// gesture (matches MAUI's stock semantics — see
/// <c>IPanGestureController.SendPan</c>); writing back to
/// <see cref="Microsoft.Maui.Controls.VisualElement.TranslationX"/> /
/// <c>TranslationY</c> round-trips through Slice 8's
/// <see cref="Microsoft.AndroidX.Compose.Maui.Platform.ModifierBridge"/>
/// so the box visibly slides as the finger moves and stays where the
/// caller releases.</para>
/// </remarks>
public partial class GesturesPage : ContentPage
{
    int _tapCount;
    double _scale = 1d;
    // Pan totals at the start of the current gesture so we can layer
    // the live delta onto the box's persistent position. Without this
    // the box snaps back to (0, 0) at each gesture start.
    double _panBaseX;
    double _panBaseY;

    /// <summary>Build the page.</summary>
    public GesturesPage()
    {
        InitializeComponent();
    }

    void OnTapped(object? sender, TappedEventArgs e)
    {
        _tapCount++;
        TapEcho.Text = $"Taps: {_tapCount}";
    }

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panBaseX = PanTarget.TranslationX;
                _panBaseY = PanTarget.TranslationY;
                break;

            case GestureStatus.Running:
                PanTarget.TranslationX = _panBaseX + e.TotalX;
                PanTarget.TranslationY = _panBaseY + e.TotalY;
                PanEcho.Text = $"dx: {e.TotalX:F0} · dy: {e.TotalY:F0}";
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                PanEcho.Text = $"dx: {PanTarget.TranslationX:F0} · dy: {PanTarget.TranslationY:F0} (released)";
                break;
        }
    }

    void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                // Keep the in-flight scale; the per-frame scale is the
                // multiplier so we just chain it onto the persisted value.
                break;

            case GestureStatus.Running:
                _scale = Math.Clamp(_scale * e.Scale, 0.5d, 4d);
                PinchTarget.Scale = _scale;
                PinchEcho.Text = $"Scale: {_scale:F2}";
                break;
        }
    }

    void OnSwiped(object? sender, SwipedEventArgs e)
    {
        SwipeEcho.Text = $"Last swipe: {e.Direction}";
    }
}
