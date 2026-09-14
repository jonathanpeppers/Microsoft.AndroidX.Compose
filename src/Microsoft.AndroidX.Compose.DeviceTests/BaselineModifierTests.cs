using Android.Graphics;
using Android.OS;
using Android.Views;
using AndroidX.Compose;
using AndroidX.Compose.UI.Layout;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;
using Color = AndroidX.Compose.Color;
using System.Runtime.Versioning;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies scope dispatch, immutable identity, placed baselines and native clipping.</summary>
[TestClass]
[DoNotParallelize]
public class BaselineModifierTests
{
    [TestMethod]
    public void StructuralKeys_PreserveArgumentsOrderAndOriginalChains()
    {
        var first = Baselines.FirstBaseline;
        var last = Baselines.LastBaseline;
        var original = Modifier.PaddingFrom(first, before: 32);
        var originalKey = original.StructuralKey;
        var extended = original.AlignBy(first).ClipToBounds();
        var equal = Modifier.PaddingFrom(first, before: new Dp(32)).AlignByBaseline().ClipToBounds();
        Assert.AreEqual(originalKey, original.StructuralKey);
        Assert.AreEqual(1, original.StructuralKey.Count);
        Assert.AreEqual(3, extended.StructuralKey.Count);
        Assert.AreEqual(extended.StructuralKey, equal.StructuralKey);
        Assert.AreEqual(extended.StructuralKey.GetHashCode(), equal.StructuralKey.GetHashCode());
        Assert.AreEqual(extended.StructuralKey, original.Then(Modifier.AlignByBaseline().ClipToBounds()).StructuralKey);
        Assert.AreNotEqual(extended.StructuralKey, original.ClipToBounds().AlignByBaseline().StructuralKey);
        Assert.AreNotEqual(Modifier.AlignBy(first).StructuralKey, Modifier.AlignBy(last).StructuralKey);
        Assert.AreNotEqual(Modifier.PaddingFrom(first).StructuralKey, Modifier.PaddingFrom(first, before: 0).StructuralKey);
        Assert.AreNotEqual(Modifier.PaddingFrom(first, before: 32).StructuralKey, Modifier.PaddingFrom(first, after: 32).StructuralKey);
        Assert.AreNotEqual(Modifier.PaddingFrom(first, before: 32).StructuralKey, Modifier.PaddingFrom(last, before: 32).StructuralKey);
        Assert.AreNotEqual(Modifier.PaddingFromBaseline(top: 32).StructuralKey, Modifier.PaddingFromBaseline(bottom: 32).StructuralKey);
        Assert.AreEqual(Modifier.PaddingFromBaseline(top: 32).StructuralKey, Modifier.Companion.PaddingFromBaseline(top: 32).StructuralKey);
        Assert.AreEqual(Modifier.ClipToBounds().StructuralKey, Modifier.Companion.ClipToBounds().StructuralKey);
#pragma warning disable CS8625, CS8604 // Exercise public runtime null guards.
        Assert.ThrowsExactly<ArgumentNullException>(() => Modifier.AlignBy((HorizontalAlignmentLine?)null));
        Assert.ThrowsExactly<ArgumentNullException>(() => Modifier.PaddingFrom(null));
#pragma warning restore CS8625, CS8604
    }

    [TestMethod]
    public void WrongScope_ThrowsAtMaterializationAndRestoresOuterScope()
    {
        var horizontal = Modifier.AlignByBaseline();
        using var verticalLine = new VerticalAlignmentLine(new BaselineLineMerger());
        var vertical = Modifier.AlignBy(verticalLine);
        ScopeKind[] kinds = [ScopeKind.None, ScopeKind.Box, ScopeKind.Column, ScopeKind.Row, ScopeKind.Other];
        foreach (ScopeKind kind in kinds)
        {
            using (RenderContext.PushScope(IntPtr.Zero, kind))
            {
                var error = Assert.ThrowsExactly<InvalidOperationException>(() => horizontal.Build());
                StringAssert.Contains(error.Message, "requires an active Row scope");
                StringAssert.Contains(error.Message, $"Current scope kind: {kind}");
                Assert.ThrowsExactly<InvalidOperationException>(() => vertical.Build());
                using (RenderContext.PushScope(IntPtr.Zero, ScopeKind.Box))
                    Assert.ThrowsExactly<InvalidOperationException>(() => horizontal.Build());
                Assert.AreEqual(kind, RenderContext.CurrentScopeKind);
            }
        }
        Assert.AreEqual(ScopeKind.None, RenderContext.CurrentScopeKind);
        using var modifierClass = Java.Lang.Class.ForName(
            "androidx.compose.ui.Modifier", true, global::Android.App.Application.Context.ClassLoader);
        using var companionField = modifierClass.GetField("Companion")
            ?? throw new InvalidOperationException("Native Modifier.Companion field missing.");
        Assert.IsNotNull(companionField.Get(null),
            "Cold rejected builds must not initialize the nested singleton before its outer interface.");
    }

    [TestMethod]
    [DataRow("alignment", 8)]
    [DataRow("direct", 4)]
    public async Task Baselines_AlignActualTextInTreeAndComposableScopes(string scenario, int count)
    {
        var activity = await Start(scenario, count);
        try
        {
            await Frame(activity);
            CheckBaselines(activity, "first", last: false);
            CheckBaselines(activity, "last", last: true);
            if (scenario == "alignment")
            {
                CheckBaselines(activity, "flow", last: false);
                Assert.AreEqual(AlignmentLine.Unspecified, activity.Measurements["missing"].First);
                Assert.AreEqual(activity.Measurements["lone"].Y, activity.Measurements["missing"].Y, 1);
            }
            var before = activity.Measurements["first-large"];
            int passes = Volatile.Read(ref activity.RenderPasses);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            await OnUi(activity, () => activity.Cycle.Value++);
            await WaitFor(() => Volatile.Read(ref activity.RenderPasses) > passes, "Baseline content did not recompose after GC.");
            await Frame(activity);
            Assert.AreEqual(before, activity.Measurements["first-large"]);
            CheckBaselines(activity, "first", last: false);
        }
        finally { await Finish(activity); }
    }

    [TestMethod]
    public async Task VerticalLines_UseColumnAndFlowColumnScopes()
    {
        var activity = await Start("column", 6);
        try
        {
            await Frame(activity);
            string[] prefixes = ["column", "flow-column"];
            foreach (string prefix in prefixes)
            {
                var small = activity.Measurements[$"{prefix}-small"];
                var large = activity.Measurements[$"{prefix}-large"];
                Assert.AreEqual(ScopeKind.Column, activity.Scopes[$"{prefix}-small"]);
                Assert.AreEqual(ScopeKind.Column, activity.Scopes[$"{prefix}-large"]);
                Assert.AreEqual(10 * Density(activity), small.Line, 1);
                Assert.AreEqual(30 * Density(activity), large.Line, 1);
                Assert.AreEqual(small.X + small.Line, large.X + large.Line, 1);
                Assert.AreNotEqual(small.X, large.X, "Published vertical lines must offset the children.");
                Console.WriteLine($"{prefix}: small={small}; large={large}; scope=Column");
            }
            Assert.AreEqual(activity.Measurements["column-missing-small"].X,
                activity.Measurements["column-missing-large"].X, 1, "Absent vertical lines retain start alignment.");
        }
        finally { await Finish(activity); }
    }

    [TestMethod]
    public async Task Padding_UsesNativeUnspecifiedAndMeasuresRequestedDistances()
    {
        var activity = await Start("padding", 12);
        try
        {
            var m = activity.Measurements;
            float density = Density(activity);
            string[] ids = ["null", "zero", "baseline-null", "baseline-zero"];
            foreach (string id in ids)
            {
                Assert.AreEqual(m["natural"].Height, m[id].Height, id);
                Assert.AreEqual(m["natural"].First, m[id].First, id);
                Assert.AreEqual(m["natural"].Last, m[id].Last, id);
            }
            Assert.AreEqual(32 * density, m["before"].First, 1);
            Assert.AreEqual(24 * density, m["after"].Height - m["after"].Last, 1);
            Assert.AreEqual(32 * density, m["both"].First, 1);
            Assert.AreEqual(24 * density, m["both"].Height - m["both"].Last, 1);
            Assert.AreEqual(40 * density, m["constrained"].Height, 1, "Padding must obey incoming max height.");
            Assert.AreEqual(40 * density, m["min-null"].First, 1, "Unspecified before must honor after under a minimum height.");
            Assert.IsTrue(m["min-zero"].First < m["min-null"].First - 10 * density, "Explicit before=0 must not be treated as omitted.");
            Assert.AreEqual(44 * density, m["absent-line"].Height, 1, "Missing line uses zero position plus natural child height.");
            Console.WriteLine($"Padding density={density}: before={m["before"]}; after={m["after"]}; both={m["both"]}; constrained={m["constrained"]}");
        }
        finally { await Finish(activity); }
    }

    [TestMethod]
    public async Task ClipToBounds_ClipsNativePixelsWithoutChangingMeasurement()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            Assert.Inconclusive("Window PixelCopy requires Android 26 or later.");
            return;
        }
        var activity = await Start("clipping", 2);
        try
        {
            await Frame(activity);
            var clipped = activity.Measurements["clipped"];
            var open = activity.Measurements["unclipped"];
            Assert.AreEqual(open.Width, clipped.Width);
            Assert.AreEqual(open.Height, clipped.Height);
            using var bitmap = await Capture(activity);
            float density = Density(activity);
            AssertPixel(bitmap, open, 70, Color.Red, density);
            AssertPixel(bitmap, clipped, 70, Color.Red, density);
            AssertPixel(bitmap, open, 120, Color.Red, density);
            AssertPixel(bitmap, clipped, 120, Color.White, density);
            Console.WriteLine($"Native clip: density={density}; unclipped={open}; clipped={clipped}; inside red, overflow red/white.");
        }
        finally { await Finish(activity); }
    }

    static void CheckBaselines(BaselineModifierTestActivity activity, string prefix, bool last)
    {
        var a = activity.Measurements[$"{prefix}-small"];
        var b = activity.Measurements[$"{prefix}-large"];
        Assert.AreNotEqual(AlignmentLine.Unspecified, a.First);
        Assert.AreNotEqual(AlignmentLine.Unspecified, b.First);
        Assert.AreEqual(a.Y + (last ? a.Last : a.First), b.Y + (last ? b.Last : b.First), 1, prefix);
        Assert.AreNotEqual(a.Y, b.Y, "The differently sized text must actually be offset.");
        Assert.AreEqual(ScopeKind.Row, activity.Scopes[$"{prefix}-small"]);
        Assert.AreEqual(ScopeKind.Row, activity.Scopes[$"{prefix}-large"]);
        Console.WriteLine($"{prefix}: small={a}; large={b}; scope=Row");
    }

    static float Density(BaselineModifierTestActivity activity) =>
        activity.Resources?.DisplayMetrics?.Density ?? throw new InvalidOperationException("Baseline density unavailable.");

    static void AssertPixel(Bitmap bitmap, BaselineModifierTestActivity.Bounds bounds, int xDp, Color expected, float density)
    {
        int x = (int)MathF.Round(bounds.X + xDp * density);
        int y = (int)MathF.Round(bounds.Y + bounds.Height / 2f);
        Assert.AreEqual(unchecked((int)(expected.ToPacked() >> 32)), bitmap.GetPixel(x, y), $"Pixel at {x},{y}");
    }

    static async Task<BaselineModifierTestActivity> Start(string scenario, int count)
    {
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(BaselineModifierTestActivity));
        intent.PutExtra("scenario", scenario);
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => BaselineModifierTestActivity.Current?.Measurements.Count == count, $"Scenario {scenario} did not place {count} nodes.");
        return BaselineModifierTestActivity.Current ?? throw new InvalidOperationException("Baseline activity missing.");
    }

    static async Task Finish(BaselineModifierTestActivity activity)
    {
        await OnUi(activity, activity.Finish);
        await WaitFor(() => BaselineModifierTestActivity.Current is null, "Baseline activity did not finish.");
    }

    static Task OnUi(BaselineModifierTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception e) { completion.SetException(e); }
        });
        return completion.Task;
    }

    static async Task Frame(BaselineModifierTestActivity activity)
    {
        await NextFrame(activity);
        await NextFrame(activity);
    }

    static async Task NextFrame(BaselineModifierTestActivity activity)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callback = new Java.Lang.Runnable(() => completion.SetResult());
        await OnUi(activity, () =>
        {
            var view = activity.Window?.DecorView ?? throw new InvalidOperationException("Baseline DecorView missing.");
            view.PostOnAnimation(callback);
        });
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [SupportedOSPlatform("android26.0")]
    static async Task<Bitmap> Capture(BaselineModifierTestActivity activity)
    {
        var window = activity.Window ?? throw new InvalidOperationException("Baseline window missing.");
        var view = window.DecorView;
        var bitmap = Bitmap.CreateBitmap(view.Width, view.Height,
            Bitmap.Config.Argb8888 ?? throw new InvalidOperationException("ARGB bitmap configuration missing."));
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new BaselinePixelCopyListener(completion);
        using var handler = new Handler(Looper.MainLooper ?? throw new InvalidOperationException("Main looper missing."));
        try
        {
            await OnUi(activity, () => PixelCopy.Request(window, bitmap, listener, handler));
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    static async Task WaitFor(Func<bool> condition, string message)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        Assert.IsTrue(condition(), message);
    }
}
