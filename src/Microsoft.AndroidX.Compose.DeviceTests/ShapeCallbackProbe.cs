using AndroidX.Compose;
using AndroidX.Compose.UI.Unit;
using ComposePath = AndroidX.Compose.Path;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ShapeCallbackProbe
{
    internal int Calls;
    internal Size LastSize;
    internal LayoutDirection? LastDirection;
    internal ComposePath? Borrowed;
    internal ComposePath? Copy;
    internal bool RetainCopy;

    internal void Build(ComposePath path, Size size, LayoutDirection direction)
    {
        Assert.IsTrue(path.IsEmpty, "Each native outline must start with an empty path.");
        Interlocked.Increment(ref Calls);
        LastSize = size;
        LastDirection = direction;
        Borrowed = path;
        bool rtl = direction == LayoutDirection.Rtl;
        path.MoveTo(rtl ? size.Width : 0, 0)
            .LineTo(rtl ? 0 : size.Width, size.Height / 2)
            .LineTo(rtl ? size.Width : 0, size.Height);
        if (RetainCopy)
            Copy = new ComposePath(path);
        // Leave the contour open: GenericShape, not the adapter, must close it.
    }
}
