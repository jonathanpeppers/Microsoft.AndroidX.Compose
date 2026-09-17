using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks pending and installed shared ownership without retaining an unused backend.</summary>
[TestClass]
[DoNotParallelize]
public class SharedStateSiblingTests
{
    /// <summary>Retains a fixed owner location while its sibling leaves and returns.</summary>
    [TestMethod]
    public void SiblingsSharePendingAndInstalledOwner_ThenRetire()
    {
        using var applier = new StateOnlyApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");
        var state = new TimePickerState(7, 10);
        bool showBorrower = true;
        using var siblings = new ComposableLambda2(composer =>
        {
            composer.RememberTimePickerState(state);
            var peer = state.Jvm;
            Assert.IsNotNull(peer);
            composer.StartReplaceableGroup(739102);
            try
            {
                if (showBorrower)
                {
                    composer.RememberTimePickerState(state);
                    Assert.AreSame(peer, state.Jvm, "Pending siblings must borrow the same initialized peer.");
                }
            }
            finally
            {
                composer.EndReplaceableGroup();
            }
        });
        using var empty = new ComposableLambda2(_ => { });
        try
        {
            composition.ComposeContent(siblings);
            var original = state.Jvm;
            Assert.IsNotNull(original);
            Apply();
            state.Hour = 19;
            state.Minute = 42;

            showBorrower = false;
            composition.ComposeContent(siblings);
            Apply();
            Assert.AreSame(original, state.Jvm, "Removing a borrower must not replace the owner.");
            Assert.AreEqual(19, state.Hour);
            Assert.AreEqual(42, state.Minute);

            showBorrower = true;
            composition.ComposeContent(siblings);
            Apply();
            Assert.AreSame(original, state.Jvm, "Returning borrower must reuse the committed peer.");

            composition.ComposeContent(empty);
            Apply();
            Assert.IsNull(state.Jvm, "Removing both siblings must release ownership.");
            composition.ComposeContent(siblings);
            Apply();
            Assert.IsNotNull(state.Jvm);
            Assert.AreNotSame(original, state.Jvm, "Reentry must not resurrect a retired peer.");
            Assert.AreEqual(19, state.Hour);
            Assert.AreEqual(42, state.Minute);
        }
        finally
        {
            composition.Dispose();
            recomposer.Cancel();
        }
        Assert.IsNull(state.Jvm, "Disposal must release the replacement owner.");

        void Apply()
        {
            composition.ApplyChanges();
            composition.ApplyLateChanges();
            composition.ChangesApplied();
        }
    }
}
