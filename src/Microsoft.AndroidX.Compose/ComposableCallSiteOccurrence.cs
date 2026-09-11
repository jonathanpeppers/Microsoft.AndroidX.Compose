using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

// The registry retains this stateful peer until the slot's forget/abandon callback releases it.
[Register("net/compose/ComposableCallSiteOccurrence")]
internal sealed class ComposableCallSiteOccurrence : Java.Lang.Object, IRememberObserver
{
    readonly IControlledComposition _composition;
    readonly long _parent;
    readonly string _site;
    int _released;

    internal int Ordinal { get; }

    internal ComposableCallSiteOccurrence(IControlledComposition composition, long parent, string site)
    {
        _composition = composition;
        _parent = parent;
        _site = site;
        Ordinal = ComposableCallSite.Occurrences.Acquire(composition, parent, site, this);
    }

    internal ComposableCallSiteOccurrence(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) =>
        throw new InvalidOperationException("A retained composable occurrence peer cannot be reactivated without its owner.");

    public void OnRemembered() { }

    public void OnForgotten() => Release();

    public void OnAbandoned() => Release();

    internal void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
            ComposableCallSite.Occurrences.Release(_composition, _parent, _site, Ordinal);
    }
}
