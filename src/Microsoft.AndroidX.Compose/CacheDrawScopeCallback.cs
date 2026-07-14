using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

[Register("net/compose/CacheDrawScopeCallback")]
internal sealed class CacheDrawScopeCallback : Java.Lang.Object, IFunction1
{
    readonly Action<CacheDrawScope> _body;

    public CacheDrawScopeCallback(Action<CacheDrawScope> body) => _body = body;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        var value = p0 ?? throw new InvalidOperationException("Compose supplied a null CacheDrawScope.");
        var scope = new CacheDrawScope(value);
        _body(scope);
        return scope.TakeResult();
    }
}
