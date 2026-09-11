using System.Reflection;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Managed-only composer proxy that fails after an occurrence has been allocated.</summary>
public class FailingIdentityComposer : DispatchProxy
{
    internal IControlledComposition? Composition { get; set; }
    internal WeakReference? AttemptedOwner { get; private set; }
    internal int OwnersAtFailure { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.Name switch
        {
            "get_CompositeKeyHashCode" => 0L,
            "get_Composition" => Composition
                ?? throw new InvalidOperationException("Test composition is unavailable."),
            "StartMovableGroup" => null,
            "RememberedValue" => null,
            "UpdateRememberedValue" => FailPublication(args),
            _ => throw new InvalidOperationException($"Unexpected composer call: {targetMethod?.Name}."),
        };

    object FailPublication(object?[]? args)
    {
        AttemptedOwner = new WeakReference(args?[0]
            ?? throw new InvalidOperationException("Occurrence publication did not supply an owner."));
        OwnersAtFailure = ComposableCallSite.Occurrences.GetOwners().Length;
        throw new InvalidOperationException("Injected occurrence publication failure.");
    }
}
