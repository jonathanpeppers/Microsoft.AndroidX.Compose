using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using System.Text.Json;
using Path = System.IO.Path;
using Process = Android.OS.Process;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Host-driven saved-task test. The host kills this background process and
/// restores its task; instrumentation cannot survive the process under test.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar", Exported = true,
    LaunchMode = LaunchMode.SingleTask, TaskAffinity = "net.compose.devicetests.saveable")]
[Register("net/compose/devicetests/SaveableProcessTestActivity")]
public class SaveableProcessTestActivity : ComponentActivity
{
    readonly Dictionary<string, MutableNumberState<int>> _states = new();
    readonly SortedDictionary<string, int> _values = new(StringComparer.Ordinal);
    string _runId = "";
    int _previousProcessId;
    bool _restored;
    bool _saved;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _runId = savedInstanceState?.GetString("probeRunId") ?? Intent?.GetStringExtra("runId")
            ?? throw new InvalidOperationException("Saveable process probe requires runId.");
        _previousProcessId = savedInstanceState?.GetInt("probeProcessId") ?? 0;
        _restored = savedInstanceState is not null;
        // A fixed view ID is required for Android's saved view-state registry.
        var view = new ComposeView(this) { Id = 0x12345 };
        view.SetContent(_ => BuildTree());
        SetContentView(view);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        switch (intent?.GetStringExtra("command"))
        {
            case "mutate":
                if (_states.Count != 10)
                    throw new InvalidOperationException("All ten saveable probes must render before mutation.");
                int value = 101;
                foreach (string name in _values.Keys)
                    _states[name].Value = value++;
                break;
            case "finish":
                FinishAndRemoveTask();
                break;
            default:
                throw new InvalidOperationException("Unknown saveable process probe command.");
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
        base.OnSaveInstanceState(outState);
        _saved = true;
        WriteSnapshot();
    }

    internal void Record(string name, MutableNumberState<int> state, int value)
    {
        _states[name] = state;
        _values[name] = value;
        WriteSnapshot();
    }

    void WriteSnapshot()
    {
        string directory = FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Saveable process probe has no files directory.");
        string path = Path.Combine(directory, "saveable-process.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new
        {
            RunId = _runId,
            ProcessId = Process.MyPid(),
            PreviousProcessId = _previousProcessId,
            TaskId,
            Restored = _restored,
            Saved = _saved,
            Values = _values,
        }));
        File.Move(path + ".tmp", path, overwrite: true);
    }

    ComposableNode BuildTree() => new MaterialTheme
    {
        new Column
        {
            new Box
            {
                new Column
                {
                    new SaveableProbeNode(this, "nested-first"),
                    new SaveableProbeNode(this, "nested-second"),
                },
            },
            new ComposableContentNode(c => new SaveableProbeNode(this, "content-node").Render(c)),
            new SaveableProbeNode(this, "explicit-content", wrapper: 1),
            new SaveableProbeNode(this, "explicit-indexed-content", wrapper: 2),
            new SaveableProbeNode(this, "implicit-content", wrapper: 3),
            new SaveableProbeNode(this, "implicit-indexed-content", wrapper: 4),
            new SingleChoiceSegmentedButtonRow
            {
                new SegmentedButton(selected: true, onClick: static () => { })
                {
                    new SaveableProbeNode(this, "segmented-first"),
                    new SaveableProbeNode(this, "segmented-second"),
                },
            },
            new NavHost("home")
            {
                Modifier.Height(100),
                new NavDestination("home")
                {
                    new Column { new SaveableProbeNode(this, "navigation") },
                },
            },
        },
    };
}
