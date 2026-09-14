using Android.Views;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Completes a test capture only after native PixelCopy has copied the rendered window.</summary>
internal sealed class BaselinePixelCopyListener(TaskCompletionSource completion) : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
{
    public void OnPixelCopyFinished(int copyResult)
    {
        if (copyResult == (int)PixelCopyResult.Success)
            completion.SetResult();
        else
            completion.SetException(new InvalidOperationException($"Baseline PixelCopy failed: {copyResult}."));
    }
}
