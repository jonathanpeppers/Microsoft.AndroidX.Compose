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
            return new MaterialTheme
            {
                new Column
                {
                    new Text("Hello from .NET"),
                    new Text($"Count: {count}"),
                    new Button(onClick: () => count++)
                    {
                        new Text("Tap to increment"),
                    },
                },
            };
        });
    }
}
