using System.Collections.Concurrent;

namespace AndroidX.Compose;

/// <summary>Deterministic positional identity for a tree child and its runtime type.</summary>
internal static class CompositionGroupKey
{
    static readonly ConcurrentDictionary<Type, int> TypeKeys = new();

    public static int Compute(int index, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        int typeKey = TypeKeys.GetOrAdd(type, static nodeType =>
            SourceLocationKey.Compute(0, nodeType.AssemblyQualifiedName
                ?? throw new InvalidOperationException(
                    $"Composition node type '{nodeType}' has no assembly-qualified identity.")));
        return SourceLocationKey.Mix(typeKey, index);
    }
}
