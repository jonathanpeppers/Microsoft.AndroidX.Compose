using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class FabPixelCopyListener : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
{
    internal TaskCompletionSource Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void OnPixelCopyFinished(int copyResult)
    {
        if (copyResult == (int)PixelCopyResult.Success)
            Completion.TrySetResult();
        else
            Completion.TrySetException(new InvalidOperationException($"Native FAB PixelCopy failed with status {copyResult}."));
    }
}
