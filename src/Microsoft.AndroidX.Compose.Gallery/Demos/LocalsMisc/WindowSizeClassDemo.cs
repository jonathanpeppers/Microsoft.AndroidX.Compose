using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;
using AndroidX.Window.Core.Layout;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>
/// <c>composer.CurrentWindowAdaptiveInfo()</c> — live readout of the upstream
/// <see cref="WindowSizeClass"/> + <c>Posture</c>, plus a phone / tablet / desktop
/// indicator that flips at the Material 3 standard breakpoints. Rotate the
/// device or resize the split-screen window to see all values update.
/// </summary>
public static class WindowSizeClassDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-window-size-class",
        CategoryId:  "locals-misc",
        Title:       "WindowAdaptiveInfo & WindowSizeClass",
        Description: "composer.CurrentWindowAdaptiveInfo() — live size class predicates + posture for adaptive layouts. Rotate to flip the Phone/Tablet/Desktop label.",
        Build:       _ => new Column
        {
            new SizeClassReadout(),
        });

    sealed class SizeClassReadout : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var info = composer.CurrentWindowAdaptiveInfo();
            var size = info.WindowSizeClass;
            var posture = info.WindowPosture;

            new Column
            {
                new Text($"MinWidthDp  = {size.MinWidthDp}"),
                new Text($"MinHeightDp = {size.MinHeightDp}"),
                new Text(""),
                new Text("Width predicates at standard breakpoints:"),
                new Text($"  IsWidthAtLeastBreakpoint(600)  = {size.IsWidthAtLeastBreakpoint(WindowSizeClass.WidthDpMediumLowerBound)}"),
                new Text($"  IsWidthAtLeastBreakpoint(840)  = {size.IsWidthAtLeastBreakpoint(WindowSizeClass.WidthDpExpandedLowerBound)}"),
                new Text("Height predicates at standard breakpoints:"),
                new Text($"  IsHeightAtLeastBreakpoint(480) = {size.IsHeightAtLeastBreakpoint(WindowSizeClass.HeightDpMediumLowerBound)}"),
                new Text($"  IsHeightAtLeastBreakpoint(900) = {size.IsHeightAtLeastBreakpoint(WindowSizeClass.HeightDpExpandedLowerBound)}"),
                new Text(""),
                new Text($"Layout class: {DeviceLabel(size)}"),
                new Text(""),
                new Text("Posture (foldable / hinge state):"),
                new Text($"  IsTabletop  = {posture.IsTabletop}"),
                new Text($"  Hinge count = {posture.HingeList.Count}"),
            }.Render(composer);
        }

        // Process larger breakpoints first — IsWidthAtLeastBreakpoint is order-
        // dependent: a 1200dp window matches the 600dp breakpoint too. Upstream
        // docs explicitly call out this larger→smaller ordering.
        static string DeviceLabel(WindowSizeClass size) =>
            size.IsWidthAtLeastBreakpoint(WindowSizeClass.WidthDpExpandedLowerBound) ? "🖥️ Desktop (Expanded)" :
            size.IsWidthAtLeastBreakpoint(WindowSizeClass.WidthDpMediumLowerBound)   ? "📟 Tablet (Medium)"   :
                                                                                       "📱 Phone (Compact)";
    }
}
