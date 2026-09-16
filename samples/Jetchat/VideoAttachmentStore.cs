using Android.Content;

namespace AndroidX.Compose.Samples.Jetchat;

internal static class VideoAttachmentStore
{
    const long MaxVideoBytes = 25L * 1024 * 1024;
    const string DirectoryName = "jetchat-videos";

    internal static string SeedVideoUri =>
        $"android.resource://net.compose.samples.jetchat/{Resource.Raw.jetchat_video_fixture}";

    internal static async Task<string> ImportAsync(
        Context context,
        Android.Net.Uri source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(source);

        var cache = context.CacheDir
            ?? throw new InvalidOperationException("Jetchat cache directory is unavailable.");
        var directory = new DirectoryInfo(System.IO.Path.Combine(cache.AbsolutePath, DirectoryName));
        directory.Create();
        var destination = System.IO.Path.Combine(directory.FullName, $"{Guid.NewGuid():N}.mp4");

        try
        {
            await Task.Run(() => CopyBounded(context, source, destination, cancellationToken), cancellationToken);
            using var file = new Java.IO.File(destination);
            var uri = Android.Net.Uri.FromFile(file)
                ?? throw new InvalidOperationException("Could not create a URI for the imported video.");
            return uri.ToString() ?? throw new InvalidOperationException("Imported video URI was empty.");
        }
        catch
        {
            File.Delete(destination);
            throw;
        }
    }

    internal static void Prune(Java.IO.File? cacheDirectory)
    {
        if (cacheDirectory is null)
            return;
        var directory = new DirectoryInfo(System.IO.Path.Combine(cacheDirectory.AbsolutePath, DirectoryName));
        if (!directory.Exists)
            return;

        foreach (var file in directory.EnumerateFiles("*.mp4"))
            if (file.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1))
                file.Delete();
    }

    static void CopyBounded(
        Context context,
        Android.Net.Uri source,
        string destination,
        CancellationToken cancellationToken)
    {
        using var input = context.ContentResolver?.OpenInputStream(source)
            ?? throw new InvalidOperationException("The selected video could not be opened.");
        using var output = File.Create(destination);
        var buffer = new byte[64 * 1024];
        long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = input.Read(buffer);
            if (read <= 0)
                break;
            total += read;
            if (total > MaxVideoBytes)
                throw new InvalidOperationException("Choose a video smaller than 25 MiB.");
            output.Write(buffer, 0, read);
        }
        if (total == 0)
            throw new InvalidOperationException("The selected video was empty.");
    }
}
