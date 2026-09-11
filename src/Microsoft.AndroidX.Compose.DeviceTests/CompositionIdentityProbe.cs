using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class CompositionIdentityProbe
{
    internal MutableNumberState<int> Ordinary { get; } = new(0);
    internal MutableNumberState<int>? Saved { get; set; }
    internal int Observed;
    internal int ObservedSaved;
    internal int Renders;
    internal int Setups;
    internal int Disposals;
}
