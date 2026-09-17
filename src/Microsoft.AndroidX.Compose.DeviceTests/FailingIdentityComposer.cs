using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Runtime.Tooling;
using Kotlin.Coroutines;
using Kotlin.Jvm.Functions;
using ComposableCallSite = AndroidX.Compose.ComposableCallSite;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Statically implemented composer double that fails after an occurrence has been allocated.</summary>
public sealed class FailingIdentityComposer : Java.Lang.Object, IComposer
{
    internal IControlledComposition? Composition { get; set; }
    internal WeakReference? AttemptedOwner { get; private set; }
    internal int OwnersAtFailure { get; private set; }

    long IComposer.CompositeKeyHashCode => 0L;
    IControlledComposition IComposer.Composition => Composition
        ?? throw new InvalidOperationException("Test composition is unavailable.");
    void IComposer.StartMovableGroup(int key, Java.Lang.Object? dataKey) { }
    Java.Lang.Object? IComposer.RememberedValue() => null;

    void IComposer.UpdateRememberedValue(Java.Lang.Object? value)
    {
        AttemptedOwner = new WeakReference(value
            ?? throw new InvalidOperationException("Occurrence publication did not supply an owner."));
        OwnersAtFailure = ComposableCallSite.Occurrences.GetOwners().Length;
        throw new InvalidOperationException("Injected occurrence publication failure.");
    }

    IApplier IComposer.Applier => throw Unexpected();
    ICoroutineContext IComposer.ApplyCoroutineContext => throw Unexpected();
    ICompositionData IComposer.CompositionData => throw Unexpected();
    ICompositionLocalMap IComposer.CurrentCompositionLocalMap => throw Unexpected();
    int IComposer.CurrentMarker => throw Unexpected();
    int IComposer.CompoundKeyHash => throw Unexpected();
    bool IComposer.DefaultsInvalid => throw Unexpected();
    bool IComposer.Inserting => throw Unexpected();
    IRecomposeScope? IComposer.RecomposeScope => throw Unexpected();
    Java.Lang.Object? IComposer.RecomposeScopeIdentity => throw Unexpected();
    bool IComposer.Skipping => throw Unexpected();

    void IComposer.Apply(Java.Lang.Object? value, IFunction2 block) => throw Unexpected();
    CompositionContext IComposer.BuildContext() => throw Unexpected();
    bool IComposer.Changed(Java.Lang.Object? value) => throw Unexpected();
    bool IComposer.Changed(bool value) => throw Unexpected();
    bool IComposer.Changed(sbyte value) => throw Unexpected();
    bool IComposer.Changed(char value) => throw Unexpected();
    bool IComposer.Changed(double value) => throw Unexpected();
    bool IComposer.Changed(float value) => throw Unexpected();
    bool IComposer.Changed(int value) => throw Unexpected();
    bool IComposer.Changed(long value) => throw Unexpected();
    bool IComposer.Changed(short value) => throw Unexpected();
    bool IComposer.ChangedInstance(Java.Lang.Object? value) => throw Unexpected();
    void IComposer.CollectParameterInformation() => throw Unexpected();
    Java.Lang.Object? IComposer.Consume(CompositionLocal key) => throw Unexpected();
    void IComposer.CreateNode(IFunction0 factory) => throw Unexpected();
    void IComposer.DeactivateToEndGroup(bool changed) => throw Unexpected();
    void IComposer.DisableReusing() => throw Unexpected();
    void IComposer.DisableSourceInformation() => throw Unexpected();
    void IComposer.EnableReusing() => throw Unexpected();
    void IComposer.EndDefaults() => throw Unexpected();
    void IComposer.EndMovableGroup() => throw Unexpected();
    void IComposer.EndNode() => throw Unexpected();
    void IComposer.EndProvider() => throw Unexpected();
    void IComposer.EndProviders() => throw Unexpected();
    void IComposer.EndReplaceGroup() => throw Unexpected();
    void IComposer.EndReplaceableGroup() => throw Unexpected();
    IScopeUpdateScope? IComposer.EndRestartGroup() => throw Unexpected();
    void IComposer.EndReusableGroup() => throw Unexpected();
    void IComposer.EndToMarker(int marker) => throw Unexpected();
    void IComposer.InsertMovableContent(MovableContent value, Java.Lang.Object? parameter) => throw Unexpected();
    void IComposer.InsertMovableContentReferences(IList<Kotlin.Pair> references) => throw Unexpected();
    Java.Lang.Object IComposer.JoinKey(Java.Lang.Object? left, Java.Lang.Object? right) => throw Unexpected();
    void IComposer.RecordSideEffect(IFunction0 effect) => throw Unexpected();
    void IComposer.RecordUsed(IRecomposeScope scope) => throw Unexpected();
    ICancellationHandle IComposer.ScheduleFrameEndCallback(IFunction0 action) => throw Unexpected();
    bool IComposer.ShouldExecute(bool parametersChanged, int flags) => throw Unexpected();
    void IComposer.SkipCurrentGroup() => throw Unexpected();
    void IComposer.SkipToGroupEnd() => throw Unexpected();
    void IComposer.SourceInformation(string sourceInformation) => throw Unexpected();
    void IComposer.SourceInformationMarkerEnd() => throw Unexpected();
    void IComposer.SourceInformationMarkerStart(int key, string sourceInformation) => throw Unexpected();
    void IComposer.StartDefaults() => throw Unexpected();
    void IComposer.StartNode() => throw Unexpected();
    void IComposer.StartProvider(ProvidedValue value) => throw Unexpected();
    void IComposer.StartProviders(ProvidedValue[] values) => throw Unexpected();
    void IComposer.StartReplaceGroup(int key) => throw Unexpected();
    void IComposer.StartReplaceableGroup(int key) => throw Unexpected();
    IComposer IComposer.StartRestartGroup(int key) => throw Unexpected();
    void IComposer.StartReusableGroup(int key, Java.Lang.Object? dataKey) => throw Unexpected();
    void IComposer.StartReusableNode() => throw Unexpected();
    void IComposer.UseNode() => throw Unexpected();

    static InvalidOperationException Unexpected([CallerMemberName] string member = "") =>
        new($"Unexpected composer call: {member}.");
}
