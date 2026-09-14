using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks fresh managed wrappers, restored boxed peers, and key resets after recreation.</summary>
[TestClass]
[DoNotParallelize]
public class RememberSaveableRestoreTests
{
    [TestMethod]
    [DataRow("string", false)]
    [DataRow("int", false)]
    [DataRow("long", false)]
    [DataRow("float", false)]
    [DataRow("double", false)]
    [DataRow("int", true)]
    public Task KeyedWrappers_RestoreAndReset(string kind, bool changeInputsDuringRecreation) => kind switch
    {
        "string" => VerifyRestore(i => new MutableState<string>(i.ToString()), int.Parse,
            state => state.Value = "73", changeInputsDuringRecreation),
        "int" => VerifyRestore(i => new MutableNumberState<int>(i), value => value,
            state => state.Value = 73, changeInputsDuringRecreation),
        "long" => VerifyRestore(i => new MutableNumberState<long>(i), value => (int)value,
            state => state.Value = 73, changeInputsDuringRecreation),
        "float" => VerifyRestore(i => new MutableNumberState<float>(i), value => (int)value,
            state => state.Value = 73, changeInputsDuringRecreation),
        "double" => VerifyRestore(i => new MutableNumberState<double>(i), value => (int)value,
            state => state.Value = 73, changeInputsDuringRecreation),
        _ => throw new InvalidOperationException($"Unknown saveable state kind '{kind}'."),
    };

    static async Task VerifyRestore<T>(Func<int, MutableState<T>> create, Func<T, int> read,
        Action<MutableState<T>> seed, bool changeInputsDuringRecreation)
    {
        object?[] keys = ["A", null];
        int initial = 10, calls = 0, scalarCalls = 0;
        MutableState<T>? observed = null;
        int observedValue = 0, observedScalar = 0;
        RememberSaveableTestActivity.Reset(c =>
        {
            var state = c.RememberSaveableKeyed(() =>
            {
                calls++;
                return create(initial);
            }, keys);
            int scalar = c.RememberSaveableKeyed(() =>
            {
                scalarCalls++;
                return initial;
            }, keys);
            int value = read(state.Value);
            c.SideEffect(() =>
            {
                observed = state;
                observedValue = value;
                observedScalar = scalar;
            });
        });
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(RememberSaveableTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        await WaitFor(() => RememberSaveableTestActivity.Current is not null &&
                RememberSaveableTestActivity.Passes > 0, "Saveable activity did not compose.");
        var activity = RememberSaveableTestActivity.Current
            ?? throw new InvalidOperationException("Saveable activity was unavailable.");
        try
        {
            var first = State();
            Assert.AreEqual(10, observedValue);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, scalarCalls);
            await Change(() =>
            {
                keys[0] = "B";
                initial = 20;
            });
            var beforeRestore = State();
            Assert.AreNotSame(first, beforeRestore);
            Assert.AreEqual(20, observedValue);
            Assert.AreEqual(20, observedScalar);
            Assert.AreEqual(2, calls);
            Assert.AreEqual(2, scalarCalls);
            await Change(() => seed(beforeRestore));
            Assert.AreEqual(73, observedValue);

            int pass = RememberSaveableTestActivity.Passes;
            await OnUi(activity, () =>
            {
                initial = 999;
                if (changeInputsDuringRecreation)
                    keys[0] = "C";
                activity.Recreate();
            });
            await WaitFor(() => RememberSaveableTestActivity.Current is { } current &&
                    !ReferenceEquals(activity, current) && RememberSaveableTestActivity.Passes > pass,
                "Saveable activity did not recreate.");
            activity = RememberSaveableTestActivity.Current
                ?? throw new InvalidOperationException("Recreated saveable activity was unavailable.");
            Assert.IsTrue(activity.Restored, "Recreation must deliver a saved instance Bundle.");
            var restored = State();
            Assert.AreNotSame(beforeRestore, restored, "Recreation must construct a fresh managed wrapper.");
            Assert.AreEqual(73, observedValue, "The saved JVM value must override the new wrapper's initial value.");
            Assert.AreEqual(20, observedScalar);
            Assert.AreEqual(3, calls, "Restore creates one managed wrapper.");
            Assert.AreEqual(2, scalarCalls, "A restored scalar must not execute its initializer.");
            Assert.IsFalse(restored._state is IMutableIntState or IMutableLongState or IMutableFloatState,
                "The default mutable-state saver restores a boxed peer, not a primitive-specialized peer.");
            await Change(() => seed(restored));
            Assert.AreSame(restored, State());
            Assert.AreEqual(73, observedValue);
            Assert.AreEqual(3, calls);

            await Change(() =>
            {
                restored.Value = create(74).Value;
            });
            Assert.AreEqual(74, observedValue, "Writes through a restored boxed numeric peer must work.");
            await Change(() =>
            {
                keys[0] = "D";
                initial = 40;
            });
            Assert.AreNotSame(restored, State());
            Assert.AreEqual(40, observedValue);
            Assert.AreEqual(40, observedScalar);
            Assert.AreEqual(4, calls);
            Assert.AreEqual(3, scalarCalls);
            Assert.AreEqual(74, read(restored.Value), "Reset must leave the previous restored wrapper untouched.");
        }
        finally
        {
            await OnUi(activity, activity.Finish);
            await WaitFor(() => RememberSaveableTestActivity.Current is null, "Saveable activity did not finish.");
            RememberSaveableTestActivity.Content = null;
        }

        MutableState<T> State() => observed
            ?? throw new InvalidOperationException("Saveable wrapper was not observed.");

        async Task Change(Action update)
        {
            int pass = RememberSaveableTestActivity.Passes;
            await OnUi(activity, () =>
            {
                update();
                RememberSaveableTestActivity.Revision.Value++;
            });
            await WaitFor(() => RememberSaveableTestActivity.Passes > pass, "Saveable value did not recompose.");
        }
    }

    static Task OnUi(RememberSaveableTestActivity activity, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail(message);
            await Task.Delay(20);
        }
    }
}
