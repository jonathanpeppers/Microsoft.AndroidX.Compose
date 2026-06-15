using System.Runtime.CompilerServices;

// The MAUI handler assembly hand-writes a small handful of
// ComposableNode subclasses (e.g. RefreshViewHandler's
// PullToRefreshBox-with-indicator-color renderer, SliderHandler's
// thumb-bearing slider) that need to call the same internal
// ComposableLambdas.Wrap2/Wrap3 helpers facades use, plus a few of
// the [ComposeBridge]-decorated state-holder shims on the internal
// ComposeBridges static class. Limited to the Maui assembly so the
// surface stays "everything else is public C# facade or extension".
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.Maui")]
