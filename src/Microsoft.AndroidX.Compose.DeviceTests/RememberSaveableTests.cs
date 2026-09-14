using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Checks input invalidation against the real Kotlin saveable holder and managed cache.</summary>
[TestClass]
[DoNotParallelize]
public class RememberSaveableTests
{
    public TestContext? TestContext { get; set; }

    [TestMethod]
    [DataRow("single")]
    [DataRow("two")]
    [DataRow("three")]
    [DataRow("array")]
    [DataRow("ambient-array")]
    public void MutableState_KeysResetWrapperAndValue(string mode) =>
        VerifyKeys(i => new MutableState<string>(i.ToString()),
            state => state.Value = "900", state => int.Parse(state.Value), mode);

    [TestMethod]
    [DataRow("single")]
    [DataRow("two")]
    [DataRow("three")]
    [DataRow("array")]
    [DataRow("ambient-array")]
    public void MutableNumberState_KeysResetWrapperAndValue(string mode) =>
        VerifyKeys(i => new MutableNumberState<int>(i),
            state => state.Value = 900, state => state.Value, mode);

    [TestMethod]
    [DataRow("single")]
    [DataRow("two")]
    [DataRow("three")]
    [DataRow("array")]
    [DataRow("ambient-array")]
    public void Scalar_KeysResetValue(string mode) =>
        VerifyKeys(i => i, null, value => value, mode);

    [TestMethod]
    public void UnsupportedArrayValuedKey_DocumentsLegacyMarshallingConstraint()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        int calls = 0, scalarCalls = 0, initial = 10;
        int[] key = [1];
        MutableNumberState<int>? observed = null;
        int scalar = 0;
        try
        {
            MutableNumberState<int> Compose()
            {
                composition.ComposeContent(new ComposableLambda2(c =>
                {
                    observed = c.RememberSaveable(() =>
                    {
                        calls++;
                        return new MutableNumberState<int>(initial);
                    }, key1: key);
                    scalar = c.RememberSaveable(() =>
                    {
                        scalarCalls++;
                        return initial;
                    }, key1: key);
                }));
                composition.ApplyChanges();
                return observed ?? throw new InvalidOperationException("Fallback-key state was not composed.");
            }

            var first = Compose();
            first.Value = 900;
            initial = 20;
            int[] replacementKey = [2];
            Assert.IsFalse(Equals(key, replacementKey));
            Assert.AreEqual(key.ToString(), replacementKey.ToString());
            key = replacementKey;
            var replacement = Compose();
            var context = TestContext ?? throw new InvalidOperationException("Test context was not supplied.");
            context.WriteLine($"Unsupported array-valued key: calls={calls}, wrapperSame={ReferenceEquals(first, replacement)}, " +
                $"value={replacement.Value}, scalarCalls={scalarCalls}, scalar={scalar}.");
            // An array used as ONE key is not the key-array overload. Its legacy
            // ToString() marshalling loses element identity; this is not a supported reset contract.
            Assert.AreEqual(900, replacement.Value);
            Assert.AreEqual(10, scalar);
            Assert.AreEqual(1, scalarCalls);
            Assert.AreEqual(2, calls, "Managed equality still detects the distinct key objects.");
            Assert.AreNotSame(first, replacement);
        }
        finally
        {
            composition.Dispose();
        }
    }

    [TestMethod]
    public void NullKeyArray_IsRejectedBeforeFactoryRuns()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        try
        {
            int calls = 0;
            composition.ComposeContent(new ComposableLambda2(c =>
            {
#pragma warning disable CS8625
                var error = Assert.ThrowsExactly<ArgumentNullException>(
                    () => c.RememberSaveableKeyed(() => ++calls, null));
#pragma warning restore CS8625
                Assert.AreEqual("keys", error.ParamName);
            }));
            composition.ApplyChanges();
            Assert.AreEqual(0, calls);
        }
        finally
        {
            composition.Dispose();
        }
    }

    [TestMethod]
    public void KeylessAndEmptyInputs_AgreeButSingleNullIsAKey()
    {
        VerifyInputShape(i => new MutableState<string>(i.ToString()), state => int.Parse(state.Value));
        VerifyInputShape(i => new MutableNumberState<int>(i), state => state.Value);
        VerifyInputShape(i => i, value => value);
    }

    static void VerifyInputShape<T>(Func<int, T> create, Func<T, int> read)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        int calls = 0;
        T? observed = default;
        try
        {
            T Compose(int shape)
            {
                composition.ComposeContent(new ComposableLambda2(c =>
                {
                    Func<T> factory = () => create(++calls);
                    // Pin one call-site identity while selecting different public overloads.
                    observed = shape switch
                    {
                        0 => c.RememberSaveable(factory, line: 355, file: nameof(RememberSaveableTests)),
                        1 => c.RememberSaveableKeyed(factory, [], line: 355, file: nameof(RememberSaveableTests)),
                        2 => c.RememberSaveable(factory, key1: null, line: 355, file: nameof(RememberSaveableTests)),
                        _ => throw new InvalidOperationException("Unknown input shape."),
                    };
                }));
                composition.ApplyChanges();
                return observed ?? throw new InvalidOperationException("Input-shape value was not composed.");
            }

            var keyless = Compose(0);
            var empty = Compose(1);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, read(empty));
            if (!typeof(T).IsValueType)
                Assert.AreSame(keyless, empty);
            var singleNull = Compose(2);
            Assert.AreEqual(2, calls, "A single null input is not the same as no inputs.");
            Assert.AreEqual(2, read(singleNull));
            empty = Compose(1);
            Assert.AreEqual(3, calls, "Removing the null input must reset.");
            Assert.AreEqual(3, read(empty));
            keyless = Compose(0);
            Assert.AreEqual(3, calls);
            Assert.AreEqual(3, read(keyless));
            if (!typeof(T).IsValueType)
                Assert.AreSame(empty, keyless);
        }
        finally
        {
            composition.Dispose();
        }
    }

    static void VerifyKeys<T>(Func<int, T> create, Action<T>? mutate, Func<T, int> read, string mode)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context is unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        int calls = 0;
        int initial = 10;
        object?[] keys = mode switch
        {
            "single" => [null],
            "two" => [null, "A"],
            _ => [null, "A", 7],
        };
        T? observed = default;
        try
        {
            T Compose()
            {
                // A new initializer on every pass must not itself invalidate the cached value.
                int currentInitial = initial;
                composition.ComposeContent(new ComposableLambda2(c =>
                    ComposeValue(c, () =>
                    {
                        calls++;
                        return create(currentInitial);
                    }, keys, mode, value => observed = value)));
                composition.ApplyChanges();
                return observed ?? throw new InvalidOperationException("Saveable value was not composed.");
            }

            var first = Compose();
            Assert.AreEqual(10, read(first));
            Assert.AreEqual(1, calls);
            mutate?.Invoke(first);
            initial = 20;
            // Equal elements in a new array, including a distinct but equal string.
            keys = keys.Select(key => key is string text ? new string(text.ToCharArray()) : key).ToArray();
            var same = Compose();
            Assert.AreEqual(mutate is null ? 10 : 900, read(same));
            Assert.AreEqual(1, calls, "Equal keys must not rerun the current factory.");
            if (mutate is not null)
                Assert.AreSame(first, same);

            for (int index = 0; index < keys.Length; index++)
            {
                var previous = same;
                // Mutate the caller's existing array, not just its reference.
                keys[index] = $"changed-{index}";
                same = Compose();
                Assert.AreEqual(initial, read(same), $"Key {index} did not reset the value.");
                Assert.AreEqual(index + 2, calls, $"Key {index} did not rerun the factory exactly once.");
                if (mutate is not null)
                    Assert.AreNotSame(previous, same);
                Assert.AreEqual(mutate is null ? 10 : 900, read(first),
                    "Reset must not rebind or mutate a previously returned wrapper.");
                initial++;
                var unchanged = Compose();
                Assert.AreEqual(index + 2, calls);
                if (mutate is not null)
                    Assert.AreSame(same, unchanged);
            }

            int beforeNull = calls;
            keys[0] = null;
            Assert.AreEqual(initial, read(Compose()));
            Assert.AreEqual(beforeNull + 1, calls, "Changing a non-null key back to null must reset.");
            if (mode is "array" or "ambient-array")
            {
                keys = [];
                initial++;
                var empty = Compose();
                Assert.AreEqual(initial, read(empty));
                Assert.AreEqual(beforeNull + 2, calls, "Changing key count must reset.");
                keys = [];
                var equalEmpty = Compose();
                Assert.AreEqual(beforeNull + 2, calls);
                if (mutate is not null)
                    Assert.AreSame(empty, equalEmpty);
            }
        }
        finally
        {
            composition.Dispose();
        }
    }

    [global::AndroidX.Compose.Composable]
    internal static void ComposeValue<T>(IComposer composer, Func<T> factory,
        object?[] keys, string mode, Action<T> observe)
    {
        observe(mode switch
        {
            "single" => composer.RememberSaveable(factory, key1: keys[0]),
            "two" => composer.RememberSaveable(factory, keys[0], keys[1]),
            "three" => composer.RememberSaveable(factory, keys[0], keys[1], keys[2]),
            "array" => composer.RememberSaveableKeyed(factory, keys),
            "ambient-array" => Composables.RememberSaveableKeyed(factory, keys),
            _ => throw new InvalidOperationException($"Unknown saveable key mode '{mode}'."),
        });
    }
}
