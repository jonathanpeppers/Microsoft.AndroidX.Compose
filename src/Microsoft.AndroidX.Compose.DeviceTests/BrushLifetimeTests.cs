using System.Reflection;
using System.Runtime.CompilerServices;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Cursor-brush construction must remain valid across managed and Java collection.</summary>
[TestClass]
[DoNotParallelize]
public class BrushLifetimeTests
{
    /// <summary>Reuses the constructor after collection without retaining a temporary class peer.</summary>
    [TestMethod]
    public void SolidColorConstructor_SurvivesCollection()
    {
        PrimeConstructor();
        LogLegacyClassHandle("before GC");
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Java.Lang.JavaSystem.RunFinalization();
        }
        LogLegacyClassHandle("after GC, before constructor");
        using var brush = Brush.SolidColor(Color.Blue);
        Assert.IsInstanceOfType<AndroidX.Compose.UI.Graphics.SolidColor>(brush);
        Assert.IsFalse(string.IsNullOrEmpty(brush.ToString()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimeConstructor()
    {
        using var brush = Brush.SolidColor(Color.Red);
        Assert.IsFalse(string.IsNullOrEmpty(brush.ToString()));
    }

    static void LogLegacyClassHandle(string phase)
    {
        var field = typeof(ComposeBridges).GetField("s_solidColor_class",
            BindingFlags.Static | BindingFlags.NonPublic);
        string value = field?.GetValue(null) is IntPtr handle
            ? $"0x{handle.ToInt64():x}"
            : "no legacy raw class cache";
        Android.Util.Log.Info("BrushLifetime", $"{phase}: SolidColor constructor class={value}");
    }
}
