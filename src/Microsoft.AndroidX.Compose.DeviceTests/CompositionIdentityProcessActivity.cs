using System.Text.Json;
using Android.Content;
using Android.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Persists real activity state for the host-driven fresh-process identity regression.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar",
    Exported = true, LaunchMode = global::Android.Content.PM.LaunchMode.SingleTask,
    TaskAffinity = "net.compose.devicetests.identity")]
[Register("net/compose/devicetests/CompositionIdentityProcessActivity")]
public class CompositionIdentityProcessActivity : CompositionIdentityTestActivity
{
    string _runId = "";
    int _previousProcessId;
    bool _restored;
    bool _saved;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Isolate interceptor ancestry from tree-container key behavior covered separately by #353.
        Reset(savedInstanceState?.GetString("identity.scenario")
            ?? Intent?.GetStringExtra("scenario") ?? "loop", directContent: true);
        _restored = savedInstanceState is not null;
        _runId = savedInstanceState?.GetString("identity.run")
            ?? Intent?.GetStringExtra("runId")
            ?? throw new InvalidOperationException("Identity process probe requires a runId.");
        _previousProcessId = savedInstanceState?.GetInt("identity.pid") ?? 0;
        Phase.Value = savedInstanceState?.GetInt("identity.phase") ?? (Scenario == "loop" ? 0 : 10);
        Committed = WriteSnapshot;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        switch (intent?.GetStringExtra("command"))
        {
            case "mutate":
                foreach (var (id, probe) in Probes)
                {
                    var saved = probe.Saved
                        ?? throw new InvalidOperationException($"Saveable state for '{id}' is unavailable.");
                    saved.Value = 1000 + NodeCode(id);
                    probe.Ordinary.Value = 2000 + NodeCode(id);
                }
                Phase.Value = Scenario == "loop" ? 1 : 15;
                break;
            case "seed-added":
                foreach (var (id, probe) in Probes.Where(pair => pair.Value.Disposals == 0))
                {
                    var saved = probe.Saved
                        ?? throw new InvalidOperationException($"Saveable state for '{id}' is unavailable.");
                    saved.Value = 1000 + NodeCode(id);
                    probe.Ordinary.Value = 2000 + NodeCode(id);
                }
                // Drive a parent side effect after the leaf-only writes have been applied.
                Count.Value++;
                break;
            case "finish":
                FinishAndRemoveTask();
                break;
        }
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);
        outState.PutString("identity.run", _runId);
        outState.PutInt("identity.pid", global::Android.OS.Process.MyPid());
        outState.PutInt("identity.phase", Phase.Value);
        outState.PutString("identity.scenario", Scenario);
        _saved = true;
        WriteSnapshot();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _saved = false;
        WriteSnapshot();
    }

    void WriteSnapshot()
    {
        var active = Probes.Where(pair => pair.Value.Disposals == 0).ToArray();
        var snapshot = new CompositionIdentityProcessSnapshot(
            _runId, global::Android.OS.Process.MyPid(), _previousProcessId, TaskId,
            _restored, _saved, Phase.Value,
            active.ToDictionary(pair => pair.Key, pair => pair.Value.ObservedSaved),
            active.ToDictionary(pair => pair.Key, pair => pair.Value.Observed));
        string directory = FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Identity process probe FilesDir is unavailable.");
        string path = Path.Combine(directory, "composition-identity-process.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(snapshot));
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
