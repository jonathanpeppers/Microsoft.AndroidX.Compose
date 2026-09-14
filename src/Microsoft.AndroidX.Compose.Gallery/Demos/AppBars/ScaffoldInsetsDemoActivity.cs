using Android.Views;
using AndroidX.Activity;

namespace AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>Runs outside the Gallery's own Scaffold to demonstrate native inset ownership.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar",
    Exported = true, WindowSoftInputMode = SoftInput.AdjustResize)]
[Android.Runtime.Register("net/compose/gallery/ScaffoldInsetsDemoActivity")]
public class ScaffoldInsetsDemoActivity : ComponentActivity
{
    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(c => new MaterialTheme
        {
            new Composed(composer =>
            {
                var mode = composer.RememberSaveable(() => new MutableState<int>(0));
                var selector = composer.RememberSaveable(() => new MutableState<bool>(false));
                var text = composer.RememberSaveable(() => new MutableState<string>(""));
                var selectorFocus = composer.Remember(() => new FocusRequester());
                var zero = composer.Remember(() => new WindowInsets());
                bool showSelector = selector.Value;
                composer.LaunchedEffect(showSelector, _ =>
                {
                    if (showSelector && selector.Value)
                        selectorFocus.RequestFocus();
                    return Task.CompletedTask;
                });
                var defaults = composer.ScaffoldContentWindowInsets();
                var excluded = defaults.Exclude(composer.NavigationBarsInsets()).Exclude(composer.ImeInsets());
                var insets = mode.Value switch
                {
                    1 => zero,
                    2 => excluded,
                    _ => defaults,
                };

                var scaffold = new Scaffold
                {
                    TopBar = new TopAppBar
                    {
                        Title = new Text("Scaffold insets"),
                        NavigationIcon = new TextButton(Finish) { new Text("Close") },
                    },
                    BodyContent = padding => new Composed(bodyComposer =>
                    {
                        var taps = bodyComposer.RememberSaveable(() => new MutableState<int>(0));
                        var identity = bodyComposer.Remember(() => Guid.NewGuid().ToString("N")[..6]);
                        return new Column
                        {
                            Modifier.FillMaxSize().Padding(padding).ConsumeWindowInsets(insets),
                            new Row
                            {
                                new TextButton(() => mode.Value = 0) { new Text("Default") },
                                new TextButton(() => mode.Value = 1) { new Text("Zero") },
                                new TextButton(() => mode.Value = 2) { new Text("Input-owned") },
                            },
                            new Text($"Mode {mode.Value}: body top {padding.Top.Value:F0}, bottom {padding.Bottom.Value:F0} dp"),
                            new TextButton(() => taps.Value++)
                            {
                                new Text($"Body {identity}, saved taps {taps.Value}"),
                            },
                            new Text("Focus the editor, open the selector, then focus again. No extra bottom strip."),
                            new TextButton(Recreate) { new Text("Recreate activity") },
                            new Spacer { Modifier = Modifier.Weight(1f) },
                            new Surface
                            {
                                Modifier.FillMaxWidth(),
                                new Column
                                {
                                    Modifier.FillMaxWidth().NavigationBarsPadding().ImePadding()
                                        .FocusRequester(selectorFocus).Focusable(),
                                    new TextField(text, singleLine: true)
                                    {
                                        Modifier = Modifier.FillMaxWidth().OnFocusChanged(focus =>
                                        {
                                            if (focus.IsFocused)
                                                selector.Value = false;
                                        }),
                                        Label = new Text("Message"),
                                    },
                                    new TextButton(() => selector.Value = !selector.Value)
                                    {
                                        new Text(selector.Value ? "Close selector" : "Open selector"),
                                    },
                                    selector.Value ? new Box
                                    {
                                        Modifier.FillMaxWidth().Height(180),
                                        new Text("Selector replaces the keyboard"),
                                    } : null,
                                },
                            },
                            new BackHandler(() => selector.Value = false, enabled: selector.Value),
                        };
                    }),
                };
                if (mode.Value != 0)
                    scaffold.ContentWindowInsets = insets;
                return scaffold;
            }),
        });
    }
}
