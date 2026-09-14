using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders custom fonts through Text and Typography while repeatedly collecting peers.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ResourceFontTestActivity")]
public class ResourceFontTestActivity : ComponentActivity
{
    internal static ResourceFontTestActivity? Current { get; private set; }
    internal MutableState<int> Cycle { get; } = new(0);
    internal MutableState<bool> Dark { get; } = new(false);
    internal int CompletedPasses;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent(c =>
        {
            var family = c.Remember(ResourceFontTests.CreateFamilyAndDisposeDescriptors);
            var typography = c.Remember(() => MaterialTheme.BuildTypography(
                bodyLarge: new TextStyle { FontFamily = family, FontSize = 24 }));
            return new Composed(composer =>
            {
                _ = Cycle.Value;
                bool dark = Dark.Value;
                Interlocked.Increment(ref CompletedPasses);
                var theme = new MaterialTheme
                {
                    Typography = typography,
                    ColorScheme = dark ? MaterialTheme.DarkColorScheme() : MaterialTheme.LightColorScheme(),
                };
                theme.Add(new Column
                {
                    // This friend assembly can see Padding(IntPtr); an integer would bind as a JNI handle.
                    Modifier.FillMaxSize().Background(dark ? Color.Black : Color.White).Padding(24.Dp()),
                    new Text("Karla regular: AVW 123") { Color = dark ? Color.White : Color.Black },
                    new Text("Karla bold: AVW 123") { FontFamily = family, FontWeight = FontWeight.Bold, FontSize = 24, Color = dark ? Color.White : Color.Black },
                    new Text("Karla italic: AVW 123") { FontFamily = family, FontStyle = FontStyle.Italic, FontSize = 24, Color = dark ? Color.White : Color.Black },
                });
                return theme;
            });
        });
        Current = this;
    }

    protected override void OnDestroy()
    {
        Current = null;
        base.OnDestroy();
    }
}
