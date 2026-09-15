using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Probes native scope validity independently of remember-observer callbacks.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateScopeMarkerTests
{
    [TestMethod]
    [DataRow("remove")]
    [DataRow("throw")]
    [DataRow("replace")]
    [DataRow("abandon")]
    [DataRow("dispose")]
    public void NativeMarkerAndToken_TrackAppliedOwnership(string operation)
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        using var firstKey = new Java.Lang.String("first");
        using var secondKey = new Java.Lang.String("second");
        var key = firstKey;
        IRecomposeScope? marker = null;
        SharedStateOwner? token = null;
        using var content = new ComposableLambda2(composer =>
        {
            token = SharedStateOwner.Remember(composer, key, () => { });
            composer.StartReusableGroup(354102, token);
            try
            {
                Assert.IsTrue(SharedStateOwnerLifetimeTests.Claim(token));
                long parentHash = composer.CompositeKeyHashCode;
                composer.StartMovableGroup(354103, token);
                try
                {
                    var inner = composer.StartRestartGroup(354104);
                    try
                    {
                        marker = inner.RecomposeScope
                            ?? throw new InvalidOperationException("Lifetime marker scope was unavailable.");
                        inner.RecordUsed(marker);
                    }
                    finally
                    {
                        inner.EndRestartGroup();
                    }
                }
                finally
                {
                    composer.EndMovableGroup();
                }
                Console.WriteLine($"Marker parent hash: before={parentHash}, after={composer.CompositeKeyHashCode}");
                Assert.AreEqual(parentHash, composer.CompositeKeyHashCode, "Marker leaked its identity into sibling save ancestry.");
            }
            finally
            {
                composer.EndReusableGroup();
            }
            if (operation == "throw")
                SharedStateOwnerLifetimeTests.ThrowingChildCleanup(composer, () => { });
        });
        using var empty = new ComposableLambda2(_ => { });
        try
        {
            composition.ComposeContent(content);
            var original = marker ?? throw new InvalidOperationException("Initial marker was unavailable.");
            var originalToken = token ?? throw new InvalidOperationException("Initial token was unavailable.");
            Assert.IsTrue(IsValid(original), "New marker must be valid before initial application.");
            Apply();
            Assert.IsTrue(IsValid(original), "Initial application invalidated the marker.");
            if (operation == "dispose")
            {
                composition.Dispose();
                Assert.IsFalse(IsValid(original), "Disposal retained a valid marker.");
                return;
            }
            if (operation is "replace" or "abandon")
            {
                key = secondKey;
                composition.ComposeContent(content);
                var replacement = marker ?? throw new InvalidOperationException("Replacement marker was unavailable.");
                var replacementToken = token ?? throw new InvalidOperationException("Replacement token was unavailable.");
                Assert.AreNotSame(original, replacement, "Reusable auxiliary replacement reused the scope.");
                Assert.IsTrue(IsValid(original), "Speculation invalidated the committed marker.");
                Assert.IsTrue(IsValid(replacement), "Speculative replacement marker was invalid.");
                if (operation == "abandon")
                {
                    composition.AbandonChanges();
                    Assert.IsTrue(IsValid(original), "Abandonment invalidated the committed marker.");
                    Assert.IsTrue(SharedStateOwnerLifetimeTests.Claim(originalToken), "Abandonment retired the committed owner.");
                    Console.WriteLine("Abandoned insertion anchor still valid=" + IsValid(replacement));
                    Assert.ThrowsExactly<InvalidOperationException>(() => SharedStateOwnerLifetimeTests.Claim(replacementToken));
                    key = firstKey;
                    composition.ComposeContent(content);
                    Assert.AreSame(original, marker, "Abandonment replaced the committed marker on reentry.");
                    Assert.AreSame(originalToken, token, "Abandonment replaced the committed token on reentry.");
                    Apply();
                }
                else
                {
                    Apply();
                    Assert.IsFalse(IsValid(original), "Applied replacement retained the previous marker.");
                    Assert.IsTrue(IsValid(replacement), "Applied replacement lost its marker.");
                    key = firstKey;
                    composition.ComposeContent(content);
                    var returned = marker ?? throw new InvalidOperationException("Returning marker was unavailable.");
                    Assert.AreNotSame(original, returned, "A-to-B-to-A resurrected a retired marker.");
                    Assert.IsTrue(IsValid(replacement), "Unapplied return invalidated the committed replacement.");
                    Apply();
                    Assert.IsFalse(IsValid(replacement), "Applied return retained the replacement marker.");
                    Assert.IsTrue(IsValid(returned), "Returning owner did not keep its new marker.");
                }
            }
            else
            {
                composition.ComposeContent(empty);
                Assert.IsTrue(IsValid(original), "Unapplied removal invalidated the committed marker.");
                if (operation == "throw")
                {
                    var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(composition.ApplyChanges);
                    StringAssert.Contains(error.Message, "Expected earlier cleanup failure.");
                }
                else
                    Apply();
                Assert.IsFalse(IsValid(original), "Applied removal retained the marker despite removed slots.");
            }
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }

        void Apply()
        {
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
        }
    }

    static bool IsValid(IRecomposeScope scope)
    {
        try
        {
            var type = JNIEnv.FindClass("androidx/compose/runtime/RecomposeScopeImpl");
            var method = JNIEnv.GetMethodID(type, "getValid", "()Z");
            bool valid = JNIEnv.CallBooleanMethod(((IJavaObject)scope).Handle, method);
            Console.WriteLine($"Scope {scope.GetHashCode()} valid={valid}");
            return valid;
        }
        finally
        {
            GC.KeepAlive(scope);
        }
    }
}
