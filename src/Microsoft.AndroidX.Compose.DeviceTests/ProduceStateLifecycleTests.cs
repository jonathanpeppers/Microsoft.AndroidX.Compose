using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies keyed ProduceState work follows committed composition lifetime.</summary>
[TestClass]
[DoNotParallelize]
public class ProduceStateLifecycleTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ChangedKey_UsesCurrentProducerAndFencesRetiredWrites(bool composerless)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = CreateRecomposer();
        var composition = CreateComposition(applier, recomposer);
        string userId = "A";
        MutableState<string>? state = null;
        MutableState<string>? writerA = null;
        MutableState<string>? writerB = null;
        CancellationToken tokenA = default;
        CancellationToken tokenB = default;
        bool aCanceledWhenBStarted = false;
        List<string> starts = [];
        var completionA = NewCompletion();
        var completionB = NewCompletion();
#pragma warning disable CN5009
        using var content = new ComposableLambda2(composer =>
        {
            string capturedUserId = userId;
            state = Produce(
                composer,
                composerless,
                initialValue: "initial",
                key: userId,
                (writer, token) =>
                {
                    starts.Add(capturedUserId);
                    if (capturedUserId == "A")
                    {
                        writerA = writer;
                        tokenA = token;
                        return completionA.Task;
                    }

                    aCanceledWhenBStarted = tokenA.IsCancellationRequested;
                    writerB = writer;
                    tokenB = token;
                    return completionB.Task;
                });
        });
#pragma warning restore CN5009

        try
        {
            composition.ComposeContent(content);
            Assert.AreEqual(0, starts.Count,
                "Initial producer started before its composition was applied.");
            Apply(composition);
            string[] expectedInitialStarts = ["A"];
            CollectionAssert.AreEqual(expectedInitialStarts, starts);
            var rememberedState = state
                ?? throw new InvalidOperationException("ProduceState did not return its state.");

            userId = "B";
            composition.ComposeContent(content);
            Assert.IsFalse(tokenA.IsCancellationRequested,
                "Speculative replacement canceled committed producer A.");
            CollectionAssert.AreEqual(expectedInitialStarts, starts,
                "Speculative replacement started producer B before apply.");

            Apply(composition);
            string[] expectedReplacementStarts = ["A", "B"];
            CollectionAssert.AreEqual(expectedReplacementStarts, starts);
            Assert.IsTrue(tokenA.IsCancellationRequested,
                "Committed replacement did not cancel producer A.");
            Assert.IsTrue(aCanceledWhenBStarted,
                "Producer B started before producer A was canceled.");
            Assert.IsFalse(tokenB.IsCancellationRequested);
            Assert.AreSame(rememberedState, state,
                "Key replacement must preserve the observable state wrapper.");

            var retiredWriter = writerA
                ?? throw new InvalidOperationException("Producer A did not receive a state writer.");
            retiredWriter.Value = "stale-A";
            Assert.AreEqual("initial", rememberedState.Value,
                "A retired producer published a stale result.");

            var currentWriter = writerB
                ?? throw new InvalidOperationException("Producer B did not receive a state writer.");
            currentWriter.Value = "fresh-B";
            Assert.AreEqual("fresh-B", rememberedState.Value);

            composition.Dispose();
            Assert.IsTrue(tokenB.IsCancellationRequested,
                "Disposing the composition did not cancel producer B.");
            currentWriter.Value = "after-dispose";
            Assert.AreEqual("fresh-B", rememberedState.Value,
                "A disposed producer published a stale result.");
        }
        finally
        {
            completionA.TrySetResult();
            completionB.TrySetResult();
            if (!composition.IsDisposed)
                composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SameKey_KeepsCommittedProducerLifetime(bool composerless)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = CreateRecomposer();
        var composition = CreateComposition(applier, recomposer);
        string capture = "A";
        MutableState<int>? state = null;
        CancellationToken tokenA = default;
        int startsA = 0;
        int startsB = 0;
        var completionA = NewCompletion();
        var completionB = NewCompletion();
#pragma warning disable CN5009
        using var content = new ComposableLambda2(composer =>
        {
            string currentCapture = capture;
            state = Produce(
                composer,
                composerless,
                initialValue: 7,
                key: "same-key",
                (_, token) =>
                {
                    if (currentCapture == "A")
                    {
                        startsA++;
                        tokenA = token;
                        return completionA.Task;
                    }

                    startsB++;
                    return completionB.Task;
                });
        });
#pragma warning restore CN5009

        try
        {
            composition.ComposeContent(content);
            Apply(composition);
            var rememberedState = state
                ?? throw new InvalidOperationException("ProduceState did not return its state.");

            capture = "B";
            composition.ComposeContent(content);
            Apply(composition);

            Assert.AreSame(rememberedState, state);
            Assert.AreEqual(1, startsA);
            Assert.AreEqual(0, startsB,
                "A same-key recomposition replaced the committed producer.");
            Assert.IsFalse(tokenA.IsCancellationRequested);

            composition.Dispose();
            Assert.IsTrue(tokenA.IsCancellationRequested);
        }
        finally
        {
            completionA.TrySetResult();
            completionB.TrySetResult();
            if (!composition.IsDisposed)
                composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ThrowingCancellationCallback_DoesNotBlockReplacement(bool composerless)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = CreateRecomposer();
        var composition = CreateComposition(applier, recomposer);
        string key = "A";
        int startsA = 0;
        int startsB = 0;
        var completionA = NewCompletion();
        var completionB = NewCompletion();
#pragma warning disable CN5009
        using var content = new ComposableLambda2(composer =>
        {
            string capturedKey = key;
            _ = Produce(
                composer,
                composerless,
                initialValue: 0,
                key,
                (_, token) =>
                {
                    if (capturedKey == "A")
                    {
                        startsA++;
                        token.Register(static () =>
                            throw new InvalidOperationException(
                                "Expected cancellation callback failure."));
                        return completionA.Task;
                    }

                    startsB++;
                    return completionB.Task;
                });
        });
#pragma warning restore CN5009

        try
        {
            composition.ComposeContent(content);
            Apply(composition);
            Assert.AreEqual(1, startsA);
            Assert.AreEqual(0, startsB);

            key = "B";
            composition.ComposeContent(content);
            Apply(composition);

            Assert.AreEqual(1, startsA);
            Assert.AreEqual(1, startsB,
                "A throwing cancellation callback blocked producer B startup.");
        }
        finally
        {
            completionA.TrySetResult();
            completionB.TrySetResult();
            composition.Dispose();
            recomposer.Cancel();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AbandonedAndFailedReplacement_LeaveCommittedProducerRunning(bool composerless)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = CreateRecomposer();
        var composition = CreateComposition(applier, recomposer);
        string key = "A";
        bool fail = false;
        CancellationToken tokenA = default;
        List<string> starts = [];
        var completion = NewCompletion();
#pragma warning disable CN5009
        using var content = new ComposableLambda2(composer =>
        {
            string capturedKey = key;
            _ = Produce(
                composer,
                composerless,
                initialValue: 0,
                key,
                (_, token) =>
                {
                    starts.Add(capturedKey);
                    if (capturedKey == "A")
                        tokenA = token;
                    return completion.Task;
                });
            if (fail)
                throw new Java.Lang.IllegalStateException("Expected ProduceState render failure.");
        });
#pragma warning restore CN5009

        try
        {
            composition.ComposeContent(content);
            Apply(composition);
            string[] expectedStarts = ["A"];
            CollectionAssert.AreEqual(expectedStarts, starts);

            key = "B";
            composition.ComposeContent(content);
            Assert.IsFalse(tokenA.IsCancellationRequested);
            CollectionAssert.AreEqual(expectedStarts, starts);
            composition.AbandonChanges();
            Assert.IsFalse(tokenA.IsCancellationRequested,
                "Abandoning producer B canceled committed producer A.");
            CollectionAssert.AreEqual(expectedStarts, starts,
                "Abandoning producer B started speculative work.");

            key = "C";
            fail = true;
            var error = Assert.ThrowsExactly<Java.Lang.IllegalStateException>(
                () => composition.ComposeContent(content));
            StringAssert.Contains(error.Message, "Expected ProduceState render failure.");
            Assert.IsFalse(tokenA.IsCancellationRequested,
                "A failed producer C render canceled committed producer A.");
            CollectionAssert.AreEqual(expectedStarts, starts,
                "A failed producer C render started speculative work.");

            fail = false;
            key = "A";
            composition.ComposeContent(content);
            Apply(composition);
            Assert.IsFalse(tokenA.IsCancellationRequested);
            CollectionAssert.AreEqual(expectedStarts, starts);
        }
        finally
        {
            composition.Dispose();
            Assert.IsTrue(tokenA.IsCancellationRequested,
                "Disposing the composition did not cancel producer A.");
            completion.TrySetResult();
            recomposer.Cancel();
        }
    }

    static MutableState<T> Produce<T>(
        IComposer composer,
        bool composerless,
        T initialValue,
        object? key,
        Func<MutableState<T>, CancellationToken, Task> producer)
    {
        if (!composerless)
        {
            return composer.ProduceState(
                initialValue,
                key,
                producer,
                line: 100,
                file: "ProduceStateLifecycleTests");
        }

        using var context = ComposableContext.Enter(composer);
        return Composables.ProduceState(
            initialValue,
            key,
            producer,
            line: 100,
            file: "ProduceStateLifecycleTests");
    }

    static Recomposer CreateRecomposer() =>
        new(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context was unavailable."));

    static IControlledComposition CreateComposition(
        IdentityTestApplier applier,
        Recomposer recomposer) =>
        CompositionKt.ControlledComposition(applier, recomposer)
            ?? throw new InvalidOperationException("Controlled composition was unavailable.");

    static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static void Apply(IControlledComposition composition)
    {
        composition.ApplyChanges();
        composition.ApplyLateChanges();
        composition.ChangesApplied();
    }
}
