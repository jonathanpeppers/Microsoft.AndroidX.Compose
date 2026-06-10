using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose.UI.Platform;

namespace AndroidX.Compose.Gallery;

/// <summary>
/// Demonstrates the <see cref="ComposeView"/> interop entry point —
/// hosting Compose content inside an existing Android <c>View</c>
/// hierarchy. Builds a <see cref="LinearLayout"/> programmatically
/// with a native <see cref="TextView"/> on top and a
/// <see cref="ComposeView"/> underneath, then attaches a Compose
/// composition via
/// <see cref="ComposeExtensions.SetContent(ComposeView, Func{AndroidX.Compose.Runtime.IComposer, ComposableNode})"/>.
/// Mirrors the Kotlin pattern used when adding Compose to a legacy
/// <c>View</c>-based screen.
/// </summary>
[Activity(Label = "ComposeView interop", Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Android.Runtime.Register("net/compose/gallery/ComposeViewActivity")]
public class ComposeViewActivity : ComponentActivity
{
    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();

        var root = new LinearLayout(this)
        {
            Orientation     = Android.Widget.Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent),
        };
        root.SetPadding(48, 96, 48, 48);

        root.AddView(new TextView(this)
        {
            Text     = "Native Android TextView (View hierarchy)",
            TextSize = 18,
        });

        var compose = new ComposeView(this);
        compose.SetContent(c =>
        {
            var taps = c.MutableStateOf(0);
            return new MaterialTheme
            {
                new Column
                {
                    Modifier.Companion.FillMaxWidth(),
                    new Text("Compose content hosted by the ComposeView above:"),
                    new Text($"Tapped: {taps}"),
                    new Button(onClick: () => taps++) { new Text("Increment from Compose") },
                },
            };
        });
        root.AddView(compose);

        SetContentView(root);
    }
}
