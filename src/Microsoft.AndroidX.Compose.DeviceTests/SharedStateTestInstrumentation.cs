using Android.OS;
using Android.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Runs shared-state regressions without reflection-based managed test discovery.</summary>
[Instrumentation(Name = "net.compose.devicetests.SharedStateTestInstrumentation")]
public class SharedStateTestInstrumentation : Instrumentation
{
    string? _backend;
    bool _aotCompatibility;

    /// <summary>Activates the instrumentation peer from Android.</summary>
    protected SharedStateTestInstrumentation(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership) { }

    /// <summary>Selects the Compose backend before creating any compositions.</summary>
    public override void OnCreate(Bundle? arguments)
    {
        base.OnCreate(arguments);
        _backend = arguments?.GetString("composeBackend");
        _aotCompatibility = arguments?.GetString("aotCompatibility") switch
        {
            null or "false" => false,
            "true" => true,
            _ => throw new ArgumentException("aotCompatibility must be 'true' or 'false'.", nameof(arguments))
        };
        Start();
    }

    /// <summary>Reports each direct test result and a nonzero failure count on error.</summary>
    public override void OnStart()
    {
        base.OnStart();
        using var result = new Bundle();
        result.PutInt("pid", Process.MyPid());
        result.PutString("backend", _backend);
        int passed = 0;
        try
        {
            global::AndroidX.Compose.Runtime.ComposeRuntimeFlags.IsLinkBufferComposerEnabled = _backend switch
            {
                "gap" => false,
                "link" => true,
                _ => throw new InvalidOperationException("Supply composeBackend=gap or link.")
            };
            Run("SharedStateLifetimeInitialization", () =>
            {
                using var type = Java.Lang.Class.ForName("composenet.compose.SharedStateLifetime")
                    ?? throw new InvalidOperationException("SharedStateLifetime class was unavailable.");
            });
            var transactions = new SharedStateTransactionTests();
            Run(nameof(transactions.AbandonedOwnerReplacement_DoesNotReleaseCommittedOwner),
                transactions.AbandonedOwnerReplacement_DoesNotReleaseCommittedOwner);
            Run(nameof(transactions.AbandonedRecomposition_DoesNotPublishConfirmCallback),
                transactions.AbandonedRecomposition_DoesNotPublishConfirmCallback);
            var siblings = new SharedStateSiblingTests();
            Run(nameof(siblings.SiblingsSharePendingAndInstalledOwner_ThenRetire),
                siblings.SiblingsSharePendingAndInstalledOwner_ThenRetire);
            if (_aotCompatibility)
            {
                var identity = new CompositionIdentityControlledTests();
                Run(nameof(identity.FailedSlotPublication_ReleasesTheUninstalledOwner),
                    identity.FailedSlotPublication_ReleasesTheUninstalledOwner);
                var serialization = new ProcessSnapshotSerializationTests();
                Run(nameof(serialization.CompositionIdentity_PreservesHostContract),
                    serialization.CompositionIdentity_PreservesHostContract);
                Run(nameof(serialization.Navigation_PreservesNullObservations),
                    serialization.Navigation_PreservesNullObservations);
                Run(nameof(serialization.Saveable_PreservesOrderedValues),
                    serialization.Saveable_PreservesOrderedValues);
            }
            result.PutInt("passed", passed);
            result.PutInt("failed", 0);
            Finish(Result.Ok, result);
        }
        catch (Exception error)
        {
            result.PutInt("passed", passed);
            result.PutInt("failed", 1);
            result.PutString("error", error.ToString());
            Finish(Result.Canceled, result);
        }

        void Run(string name, Action test)
        {
            test();
            passed++;
            using var status = new Bundle();
            status.PutString("test", name);
            status.PutString("outcome", "passed");
            SendStatus(0, status);
        }
    }
}
