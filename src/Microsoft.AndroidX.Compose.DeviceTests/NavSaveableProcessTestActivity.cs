using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using System.Text.Json;
using Path = System.IO.Path;
using Process = Android.OS.Process;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Host-driven saved-task test of replaced static and factory navigation content.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar", Exported = true,
    LaunchMode = LaunchMode.SingleTask, TaskAffinity = "net.compose.devicetests.navsaveable")]
[Register("net/compose/devicetests/NavSaveableProcessTestActivity")]
public class NavSaveableProcessTestActivity : ComponentActivity
{
    readonly MutableNumberState<int> _generation = new(0);
    MutableNumberState<int>? _state;
    string _runId = "";
    string? _label;
    int? _value;
    int _previousProcessId;
    bool _factory, _restored, _saved;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _runId = savedInstanceState?.GetString("probeRunId") ?? Intent?.GetStringExtra("runId")
            ?? throw new InvalidOperationException("Navigation process probe requires runId.");
        _previousProcessId = savedInstanceState?.GetInt("probeProcessId") ?? 0;
        _factory = savedInstanceState?.GetBoolean("probeFactory") ?? Intent?.GetBooleanExtra("factory", false) ?? false;
        _generation.Value = savedInstanceState?.GetInt("probeGeneration") ?? 0;
        _restored = savedInstanceState is not null;
        var view = new ComposeView(this) { Id = 0x12346 };
        // No tree-container ancestors: this isolates the destination groups
        // and deferred navigation lambda rather than the other #353 paths.
        view.SetContent((IComposer composer) =>
        {
            string label = $"Account {_generation.Value}";
            var destination = _factory
                ? new NavDestination("home", _ => new NavSaveableProbeNode(this, label))
                : new NavDestination("home") { new NavSaveableProbeNode(this, label) };
            new NavHost("home") { destination }.Render(composer);
        });
        SetContentView(view);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        switch (intent?.GetStringExtra("command"))
        {
            case "update":
                var state = _state
                    ?? throw new InvalidOperationException("Navigation saveable content has not rendered.");
                state.Value = 101;
                _generation.Value = 1;
                break;
            case "finish":
                FinishAndRemoveTask();
                break;
            default:
                throw new InvalidOperationException("Unknown navigation process probe command.");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        _saved = false;
        WriteSnapshot();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutString("probeRunId", _runId);
        outState.PutInt("probeProcessId", Process.MyPid());
        outState.PutBoolean("probeFactory", _factory);
        outState.PutInt("probeGeneration", _generation.Value);
        base.OnSaveInstanceState(outState);
        _saved = true;
        WriteSnapshot();
    }

    internal void Record(MutableNumberState<int> state, int value, string label)
    {
        _state = state;
        _value = value;
        _label = label;
        WriteSnapshot();
    }

    void WriteSnapshot()
    {
        string directory = FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Navigation process probe has no files directory.");
        string path = Path.Combine(directory, "nav-saveable-process.json");
        var snapshot = new NavSaveableProcessSnapshot(
            _runId, Process.MyPid(), _previousProcessId, TaskId,
            _factory, _restored, _saved, _value, _label);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(snapshot,
            ProcessSnapshotJsonContext.Default.NavSaveableProcessSnapshot));
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
