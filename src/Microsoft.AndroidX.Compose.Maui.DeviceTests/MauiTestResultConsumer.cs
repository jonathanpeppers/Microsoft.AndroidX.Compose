using Android.OS;
using Android.App;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

internal sealed class MauiTestResultConsumer(Instrumentation instrumentation) : IDataConsumer
{
    int _passed;
    int _failed;
    int _skipped;

    internal int Passed => _passed;
    internal int Failed => _failed;
    internal int Skipped => _skipped;
    internal string? TrxReportPath { get; private set; }

    public string Uid => nameof(MauiTestResultConsumer);
    public string DisplayName => nameof(MauiTestResultConsumer);
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
                _ => null,
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
            var bundle = new Bundle();
            bundle.PutString("test", id is not null ? $"{id.Namespace}.{id.TypeName}.{id.MethodName}" : node.DisplayName);
            bundle.PutString("outcome", outcome);
            instrumentation.SendStatus(0, bundle);
        }
        return Task.CompletedTask;
    }
}
