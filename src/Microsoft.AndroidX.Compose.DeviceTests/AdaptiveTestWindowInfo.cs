using Android.Runtime;
using AndroidX.Compose.Material3.Adaptive;
using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Window.Core.Layout;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal static class AdaptiveTestWindowInfo
{
    internal static WindowAdaptiveInfo Compact() =>
        Create(
            minWidthDp: 0,
            minHeightDp: WindowSizeClass.HeightDpMediumLowerBound,
            isTabletop: false,
            hinge: null);

    internal static WindowAdaptiveInfo Expanded() =>
        Create(
            minWidthDp: WindowSizeClass.WidthDpExpandedLowerBound,
            minHeightDp: WindowSizeClass.HeightDpMediumLowerBound,
            isTabletop: false,
            hinge: null);

    internal static WindowAdaptiveInfo ExpandedWithHinge(
        float left,
        float top,
        float right,
        float bottom,
        bool isVertical,
        bool isSeparating,
        bool isOccluding,
        bool isTabletop = false) =>
        Create(
            minWidthDp: WindowSizeClass.WidthDpExpandedLowerBound,
            minHeightDp: WindowSizeClass.HeightDpMediumLowerBound,
            isTabletop,
            new HingeSpecification(
                left,
                top,
                right,
                bottom,
                isVertical,
                isSeparating,
                isOccluding));

    internal static unsafe int ExcludedBoundsCount(
        PaneScaffoldDirective directive)
    {
        ArgumentNullException.ThrowIfNull(directive);
        IntPtr bounds = IntPtr.Zero;
        try
        {
            var directiveClass = JNIEnv.FindClass(
                "androidx/compose/material3/adaptive/layout/PaneScaffoldDirective");
            var getter = JNIEnv.GetMethodID(
                directiveClass,
                "getExcludedBounds",
                "()Ljava/util/List;");
            bounds = JNIEnv.CallObjectMethod(
                ((Java.Lang.Object)directive).Handle,
                getter);
            var listClass = JNIEnv.FindClass("java/util/List");
            var size = JNIEnv.GetMethodID(listClass, "size", "()I");
            return JNIEnv.CallIntMethod(bounds, size);
        }
        finally
        {
            if (bounds != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(bounds);
            GC.KeepAlive(directive);
        }
    }

    static unsafe WindowAdaptiveInfo Create(
        int minWidthDp,
        int minHeightDp,
        bool isTabletop,
        HingeSpecification? hinge)
    {
        if (hinge is null)
        {
            return new WindowAdaptiveInfo(
                new WindowSizeClass(minWidthDp, minHeightDp),
                new Posture(isTabletop, []));
        }

        var specification = hinge.Value;
        IntPtr rect = IntPtr.Zero;
        IntPtr hingeHandle = IntPtr.Zero;
        try
        {
            // HingeInfo is public, but its constructor is absent from the
            // binding; test-only JNI creates deterministic simulated posture.
            var rectClass = JNIEnv.FindClass(
                "androidx/compose/ui/geometry/Rect");
            var rectConstructor = JNIEnv.GetMethodID(
                rectClass,
                "<init>",
                "(FFFF)V");
            var rectArgs = stackalloc JValue[4];
            rectArgs[0] = new JValue(specification.Left);
            rectArgs[1] = new JValue(specification.Top);
            rectArgs[2] = new JValue(specification.Right);
            rectArgs[3] = new JValue(specification.Bottom);
            rect = JNIEnv.NewObject(
                rectClass,
                rectConstructor,
                rectArgs);

            var hingeClass = JNIEnv.FindClass(
                "androidx/compose/material3/adaptive/HingeInfo");
            var hingeConstructor = JNIEnv.GetMethodID(
                hingeClass,
                "<init>",
                "(Landroidx/compose/ui/geometry/Rect;ZZZZ)V");
            var hingeArgs = stackalloc JValue[5];
            hingeArgs[0] = new JValue(rect);
            hingeArgs[1] = new JValue(false);
            hingeArgs[2] = new JValue(specification.IsVertical);
            hingeArgs[3] = new JValue(specification.IsSeparating);
            hingeArgs[4] = new JValue(specification.IsOccluding);
            hingeHandle = JNIEnv.NewObject(
                hingeClass,
                hingeConstructor,
                hingeArgs);
            var hingeInfo = Java.Lang.Object.GetObject<HingeInfo>(
                hingeHandle,
                JniHandleOwnership.TransferLocalRef)
                ?? throw new InvalidOperationException(
                    "Synthetic adaptive HingeInfo could not be created.");
            hingeHandle = IntPtr.Zero;
            using (hingeInfo)
            {
                var posture = new Posture(
                    isTabletop,
                    hingeList: [hingeInfo]);
                return new WindowAdaptiveInfo(
                    new WindowSizeClass(minWidthDp, minHeightDp),
                    posture);
            }
        }
        finally
        {
            if (hingeHandle != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(hingeHandle);
            if (rect != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(rect);
        }
    }

    readonly record struct HingeSpecification(
        float Left,
        float Top,
        float Right,
        float Bottom,
        bool IsVertical,
        bool IsSeparating,
        bool IsOccluding);
}
