using Android.Runtime;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Android <c>Instrumentation</c> entry point. Drives the MSTest runner
/// on-device and reports pass/fail/skip counts back via
/// <c>am instrument -w</c>'s <c>INSTRUMENTATION_RESULT</c> stream so a
/// host PC can surface results without launching an activity.
/// </summary>
[Instrumentation(Name = "net.compose.devicetests.TestInstrumentation")]
public class TestInstrumentation : Instrumentation
{
    protected TestInstrumentation(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership) { }

    public override void OnCreate(Bundle? arguments)
    {
        base.OnCreate(arguments);
        Start();
    }

    public override async void OnStart()
    {
        base.OnStart();

        var consumer = new ResultConsumer(this);
        var bundle = new Bundle();
        try
        {
            var ctx = Application.Context
                ?? throw new InvalidOperationException("Application.Context not set on TestInstrumentation.");
            var writeablePath = ctx.GetExternalFilesDir(null)?.AbsolutePath ?? Path.GetTempPath();
            var resultsPath = Path.Combine(writeablePath, "TestResults");
            var builder = await TestApplication.CreateBuilderAsync([
                "--results-directory", resultsPath,
                "--report-trx"
            ]);
            builder.AddMSTest(() => [GetType().Assembly]);
            builder.AddTrxReportProvider();
            builder.TestHost.AddDataConsumer(_ => consumer);

            using ITestApplication app = await builder.BuildAsync();
            await app.RunAsync();

            bundle.PutInt("passed", consumer.Passed);
            bundle.PutInt("failed", consumer.Failed);
            bundle.PutInt("skipped", consumer.Skipped);
            bundle.PutString("resultsPath", consumer.TrxReportPath);
            Finish(Result.Ok, bundle);
        }
        catch (Exception ex)
        {
            bundle.PutString("error", ex.ToString());
            Finish(Result.Canceled, bundle);
        }
    }

    class ResultConsumer(Instrumentation instrumentation) : IDataConsumer
    {
        int _passed, _failed, _skipped;
        public int Passed => _passed;
        public int Failed => _failed;
        public int Skipped => _skipped;
        public string? TrxReportPath;

        public string Uid => nameof(ResultConsumer);
        public string DisplayName => nameof(ResultConsumer);
        public string Description => "";
        public string Version => "1.0";
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact)];

        public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
        {
            if (value is SessionFileArtifact artifact)
            {
                TrxReportPath = artifact.FileInfo.FullName;
            }
            else if (value is TestNodeUpdateMessage { TestNode: var node })
            {
                var state = node.Properties.SingleOrDefault<TestNodeStateProperty>();
                string? outcome = state switch
                {
                    PassedTestNodeStateProperty => "passed",
                    FailedTestNodeStateProperty or ErrorTestNodeStateProperty
                        or TimeoutTestNodeStateProperty => "failed",
                    SkippedTestNodeStateProperty => "skipped",
                    _ => null
                };
                if (outcome is null)
                    return Task.CompletedTask;

                _ = outcome switch
                {
                    "passed" => Interlocked.Increment(ref _passed),
                    "failed" => Interlocked.Increment(ref _failed),
                    _ => Interlocked.Increment(ref _skipped),
                };

                var id = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
                var b = new Bundle();
                b.PutString("test", id is not null ? $"{id.Namespace}.{id.TypeName}.{id.MethodName}" : node.DisplayName);
                b.PutString("outcome", outcome);
                instrumentation.SendStatus(0, b);
            }
            return Task.CompletedTask;
        }
    }
}
