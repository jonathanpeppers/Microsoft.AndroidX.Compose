namespace AndroidX.Compose.SourceGenerators.Tests;

internal sealed class ViewportListenerOwner
{
    public ViewportListenerOwner() => Callback = () => Calls++;

    public System.Action Callback { get; }

    public int Calls { get; private set; }
}
