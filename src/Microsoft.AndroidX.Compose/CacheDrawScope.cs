using Android.Runtime;
using AndroidX.Compose.UI.Draw;

namespace AndroidX.Compose;

/// <summary>
/// Scope used by <see cref="ModifierExtensions.DrawWithCache"/> to build a
/// draw callback only when size or observed state changes.
/// </summary>
public sealed class CacheDrawScope
{
    readonly Java.Lang.Object _jvm;
    DrawResult? _result;

    internal CacheDrawScope(Java.Lang.Object jvm) => _jvm = jvm;

    /// <summary>Current drawing bounds in pixels.</summary>
    public Size Size => Size.FromPacked(ComposeBridges.CacheDrawScopeSize(_jvm.Handle));

    /// <summary>Sets a callback that draws before the modified content.</summary>
    public void OnDrawBehind(Action<DrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _result = WrapDrawResult(ComposeBridges.CacheDrawScopeOnDrawBehind(
            _jvm.Handle, new DrawScopeCallback(draw)));
    }

    /// <summary>
    /// Sets a callback that controls when the modified content is drawn.
    /// Call <see cref="ContentDrawScope.DrawContent"/> from the callback.
    /// </summary>
    public void OnDrawWithContent(Action<ContentDrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _result = WrapDrawResult(ComposeBridges.CacheDrawScopeOnDrawWithContent(
            _jvm.Handle, new ContentDrawScopeCallback(draw)));
    }

    internal DrawResult TakeResult() =>
        _result ?? throw new InvalidOperationException(
            "DrawWithCache must call OnDrawBehind or OnDrawWithContent.");

    static DrawResult WrapDrawResult(IntPtr handle)
    {
        var local = handle;
        try
        {
            var result = Java.Lang.Object.GetObject<DrawResult>(
                local, JniHandleOwnership.TransferLocalRef);
            local = IntPtr.Zero;
            return result ?? throw new InvalidOperationException(
                "CacheDrawScope returned a null DrawResult.");
        }
        finally
        {
            if (local != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(local);
        }
    }
}
