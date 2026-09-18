using Android.App;
using Android.OS;
using Android.Runtime;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

/// <summary>Android instrumentation entry point for the MAUI Compose device regressions.</summary>
[Instrumentation(Name = "net.compose.maui.devicetests.TestInstrumentation")]
public class TestInstrumentation : Instrumentation
{
    internal static TestInstrumentation? Current { get; private set; }

    string? _filter;

    protected TestInstrumentation(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership) { }

    public override void OnCreate(Bundle? arguments)
    {
        base.OnCreate(arguments);
        Current = this;
        _filter = arguments?.GetString("filter");
        Start();
    }

    /// <summary>Releases the current test-host reference when Android destroys the instrumentation.</summary>
    public override void OnDestroy()
    {
        Current = null;
        base.OnDestroy();
    }

    public override async void OnStart()
    {
        base.OnStart();

        var consumer = new MauiTestResultConsumer(this);
        var bundle = new Bundle();
        try
        {
            var context = global::Android.App.Application.Context
                ?? throw new InvalidOperationException("Application.Context not set on TestInstrumentation.");
            var resultsPath = Path.Combine(
                context.GetExternalFilesDir(null)?.AbsolutePath ?? Path.GetTempPath(),
                "TestResults");
            List<string> runnerArguments = [
                "--results-directory", resultsPath,
                "--report-trx",
            ];
            if (!string.IsNullOrWhiteSpace(_filter))
            {
                runnerArguments.Add("--filter");
                runnerArguments.Add(_filter);
            }

            var builder = await TestApplication.CreateBuilderAsync([.. runnerArguments]);
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
        finally
        {
            Current = null;
        }
    }
}
