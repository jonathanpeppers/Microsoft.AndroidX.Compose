using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

internal sealed class MovableStateProbeHandler(
    global::Android.Content.Context context,
    string id,
    IDictionary<string, object> observed,
    IList<string> order,
    IDictionary<string, int> disposals) : IViewHandler, IComposeHandler
{
    readonly global::Android.Views.View _platformView = new(context);
    bool _disconnected;

    public object PlatformView => _platformView;

    public IView? VirtualView { get; private set; }

    IElement? IElementHandler.VirtualView => VirtualView;

    public IMauiContext? MauiContext { get; private set; }

    public bool HasContainer { get; set; }

    public object? ContainerView => null;

    public ComposableNode BuildNode(IComposer composer) =>
        new MovableStateProbeNode(id, observed, order, disposals);

    public void SetMauiContext(IMauiContext mauiContext)
    {
        ArgumentNullException.ThrowIfNull(mauiContext);
        MauiContext = mauiContext;
    }

    public void SetVirtualView(IElement view)
    {
        ArgumentNullException.ThrowIfNull(view);
        VirtualView = view as IView
            ?? throw new InvalidOperationException("MovableStateProbeHandler requires an IView.");
    }

    public Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint) =>
        Microsoft.Maui.Graphics.Size.Zero;

    public void PlatformArrange(Microsoft.Maui.Graphics.Rect frame) { }

    public void UpdateValue(string property) { }

    public void Invoke(string command, object? args = null) { }

    public void DisconnectHandler()
    {
        if (_disconnected)
            return;
        _disconnected = true;
        VirtualView = null;
        MauiContext = null;
        _platformView.Dispose();
    }

    public void BumpViewPropertiesVersion() { }
}
