using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Non-composable Kotlin <c>Function0&lt;Float&gt;</c> adapter that returns
/// a boxed <see cref="Java.Lang.Float"/>.
/// </summary>
[Register("net/compose/FloatFunction0")]
internal sealed class FloatFunction0 : Java.Lang.Object, IFunction0
{
    readonly Func<float> _body;

    public FloatFunction0(Func<float> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public Java.Lang.Object Invoke() => Java.Lang.Float.ValueOf(_body());
}
