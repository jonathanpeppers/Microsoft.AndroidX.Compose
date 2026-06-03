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
            var count    = Remember(() => new MutableNumberState<int>(0));
            var name     = Remember(() => new MutableState<string>(""));
            var showDlg  = Remember(() => new MutableState<bool>(false));
            var liked    = Remember(() => new MutableState<bool>(false));
            var tab      = Remember(() => new MutableNumberState<int>(0));
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
                        new Card
                        {
                            new Text("Inside a Card"),
                            new Text($"Counter snapshot: {count}"),
                        },
                        new AssistChip(onClick: () => count++)
                        {
                            Label = new Text("Assist (+1)"),
                        },
                        new FilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                        {
                            Label = new Text(liked.Value ? "Liked" : "Like"),
                        },
                        new SuggestionChip(onClick: () => count.Value = 0)
                        {
                            Label = new Text("Reset"),
                        },
                        new NavigationBar
                        {
                            new NavigationBarItem(selected: tab.Value == 0, onClick: () => tab.Value = 0)
                            {
                                Icon  = new Text("🏠"),
                                Label = new Text("Home"),
                            },
                            new NavigationBarItem(selected: tab.Value == 1, onClick: () => tab.Value = 1)
                            {
                                Icon  = new Text("⚙"),
                                Label = new Text("Settings"),
                            },
                        },
                        new FloatingActionButton(onClick: () => showDlg.Value = true)
                        {
                            new Text("✕"),
                        },
                        showDlg.Value
                            ? new AlertDialog(onDismissRequest: () => showDlg.Value = false)
                            {
                                Title         = new Text("Reset counter?"),
                                Text          = new Text("This will set the counter back to zero."),
                                ConfirmButton = new Button(onClick: () => { count.Value = 0; showDlg.Value = false; })
                                {
                                    new Text("Reset"),
                                },
                                DismissButton = new Button(onClick: () => showDlg.Value = false)
                                {
                                    new Text("Cancel"),
                                },
                            }
                            : null,
                    },
                },
            };
        });
    }
}
