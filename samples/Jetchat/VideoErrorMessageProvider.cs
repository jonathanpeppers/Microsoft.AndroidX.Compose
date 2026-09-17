using Android.Util;
using AndroidX.Media3.Common;

namespace AndroidX.Compose.Samples.Jetchat;

[Android.Runtime.Register("net/compose/samples/jetchat/VideoErrorMessageProvider")]
internal sealed class VideoErrorMessageProvider : Java.Lang.Object, IErrorMessageProvider
{
    public Pair GetErrorMessage(Java.Lang.Object? error)
    {
#pragma warning disable CS0618, CA1422 // A fresh peer avoids Integer.valueOf()'s shared cache.
        using var code = new Java.Lang.Integer(0);
#pragma warning restore CS0618, CA1422
        using var message = new Java.Lang.String("Unable to play this video.");
        return new Pair(code, message);
    }
}
