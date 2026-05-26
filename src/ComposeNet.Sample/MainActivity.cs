using Android.OS;
using ComposeNet;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light")]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var count = Remember(() => new MutableIntState(0));
            var name  = Remember(() => new MutableState<string>(""));
            return new MaterialTheme
            {
                new Surface
                {
                    new Column
                    {
                        new Text("Hello from .NET"),
                        new Text($"Count: {count}"),
                        new Button(onClick: () => count++)
                        {
                            new Text("Tap to increment"),
                        },
                        new IconButton(onClick: () => count--)
                        {
                            new Text("−"),
                        },
                        new OutlinedTextField(name),
                        new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
                        new FloatingActionButton(onClick: () => count.Value = 0)
                        {
                            new Text("✕"),
                        },
                    },
                },
            };
        });
    }
}
