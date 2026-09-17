using AndroidX.Activity.Result;

namespace AndroidX.Compose.Samples.Jetchat;

[Android.Runtime.Register("net/compose/samples/jetchat/VideoActivityResultCallback")]
internal sealed class VideoActivityResultCallback : Java.Lang.Object, IActivityResultCallback
{
    readonly Action<Android.Net.Uri?> _completed;

    internal VideoActivityResultCallback(Action<Android.Net.Uri?> completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        _completed = completed;
    }

    public void OnActivityResult(Java.Lang.Object? result) =>
        _completed(result as Android.Net.Uri);
}
