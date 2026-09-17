using Android.Util;
using AndroidX.Media3.Common;

namespace AndroidX.Compose.Samples.Jetchat;

[Android.Runtime.Register("net/compose/samples/jetchat/VideoErrorMessageProvider")]
internal sealed class VideoErrorMessageProvider : Java.Lang.Object, IErrorMessageProvider
{
    public Pair GetErrorMessage(Java.Lang.Object? error)
    {
        using var code = Java.Lang.Integer.ValueOf(0)
            ?? throw new InvalidOperationException("Could not create the Media3 error code.");
        using var message = new Java.Lang.String("Unable to play this video.");
        return new Pair(code, message);
    }
}
