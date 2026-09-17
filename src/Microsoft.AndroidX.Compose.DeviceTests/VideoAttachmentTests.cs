using AndroidX.Compose.Samples.Jetchat;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native-host regressions for Jetchat video attachment storage and view reuse.</summary>
[TestClass]
[DoNotParallelize]
public class VideoAttachmentTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Video attachment tests require native instrumentation.");

    /// <summary>The host authority is correct and deletion cannot escape the import cache root.</summary>
    [TestMethod]
    public void Storage_UsesHostAuthorityAndDeletesOnlyDiscardedImports()
    {
        var context = global::Android.App.Application.Context;
        StringAssert.StartsWith(
            VideoAttachmentStore.SeedVideoUri(context),
            "android.resource://net.compose.devicetests/");
        var cache = context.CacheDir
            ?? throw new InvalidOperationException("Test cache directory is unavailable.");
        var files = context.FilesDir
            ?? throw new InvalidOperationException("Test files directory is unavailable.");
        var importRoot = Path.Combine(cache.AbsolutePath, "jetchat-videos");
        var outsideRoot = Path.Combine(files.AbsolutePath, "jetchat-videos");
        Directory.CreateDirectory(importRoot);
        Directory.CreateDirectory(outsideRoot);
        var discarded = Path.Combine(importRoot, "discarded.mp4");
        var sent = Path.Combine(importRoot, "sent.mp4");
        var outside = Path.Combine(outsideRoot, "outside.mp4");
        try
        {
            File.WriteAllText(discarded, "discarded");
            File.WriteAllText(sent, "sent");
            File.WriteAllText(outside, "outside");
            using var discardedFile = new Java.IO.File(discarded);
            using var outsideFile = new Java.IO.File(outside);
            VideoAttachmentStore.DeleteImported(
                context,
                global::Android.Net.Uri.FromFile(discardedFile)?.ToString());
            VideoAttachmentStore.DeleteImported(
                context,
                global::Android.Net.Uri.FromFile(outsideFile)?.ToString());

            Assert.IsFalse(File.Exists(discarded));
            Assert.IsTrue(File.Exists(sent), "Sent imports remain until normal cache pruning.");
            Assert.IsTrue(File.Exists(outside), "A same-named directory outside CacheDir must be untouched.");
        }
        finally
        {
            File.Delete(discarded);
            File.Delete(sent);
            File.Delete(outside);
        }
    }

    /// <summary>A reused AndroidView receives and publishes the replacement video URI.</summary>
    [TestMethod]
    public void ThumbnailView_UpdatesUriOnPositionalReuse()
    {
        TestVideoThumbnailView? view = null;
        Runner.RunOnMainSync(() =>
        {
            view = new TestVideoThumbnailView(
                global::Android.App.Application.Context,
                "file:///first.mp4");
            view.Attach();
            Assert.AreEqual(1, view.LoadGeneration);
            view.Detach();
            Assert.IsFalse(view.HasActiveLoad);
            view.Attach();
            Assert.AreEqual(2, view.LoadGeneration);
            view.SetVideoUri("file:///second.mp4");
            Assert.AreEqual(3, view.LoadGeneration);
        });
        try
        {
            Assert.AreEqual("file:///second.mp4", view?.CurrentVideoUri);
        }
        finally
        {
            Runner.RunOnMainSync(() => view?.Dispose());
        }
    }

    /// <summary>Bounded import rejects invalid streams and removes every partial destination.</summary>
    [TestMethod]
    public async Task ImportAsync_RejectsInvalidStreamsAndCleansPartialFiles()
    {
        const long maxVideoBytes = 25L * 1024 * 1024;
        var context = global::Android.App.Application.Context;
        var cache = context.CacheDir
            ?? throw new InvalidOperationException("Test cache directory is unavailable.");
        var importRoot = Path.Combine(cache.AbsolutePath, "jetchat-videos");
        Directory.CreateDirectory(importRoot);
        int initialCount = Directory.EnumerateFiles(importRoot).Count();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            VideoAttachmentStore.ImportAsync(
                context,
                () => new ControlledVideoStream(0)));
        Assert.AreEqual(initialCount, Directory.EnumerateFiles(importRoot).Count());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            VideoAttachmentStore.ImportAsync(
                context,
                () => new ControlledVideoStream(maxVideoBytes + 1)));
        Assert.AreEqual(initialCount, Directory.EnumerateFiles(importRoot).Count());

        await Assert.ThrowsExactlyAsync<IOException>(() =>
            VideoAttachmentStore.ImportAsync(
                context,
                () => new ControlledVideoStream(1024 * 1024, throwAt: 64 * 1024)));
        Assert.AreEqual(initialCount, Directory.EnumerateFiles(importRoot).Count());

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
            VideoAttachmentStore.ImportAsync(
                context,
                () => new ControlledVideoStream(1024 * 1024),
                cancellation.Token));
        Assert.AreEqual(initialCount, Directory.EnumerateFiles(importRoot).Count());
    }
}
