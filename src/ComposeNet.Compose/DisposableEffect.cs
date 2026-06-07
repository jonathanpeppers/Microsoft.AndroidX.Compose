using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Tree-syntax wrapper around
/// <see cref="Compose.DisposableEffect(object?, System.Func{DisposableEffectScope, System.Action})"/>.
/// Re-runs <c>Effect</c> on first composition and any time
/// <c>Key1</c> / <c>Key2</c> / <c>Key3</c> changes; calls the cleanup
/// <see cref="System.Action"/> on key change or when this node leaves
/// the composition.
/// </summary>
/// <remarks>
/// <code>
/// new DisposableEffect(_sensorId, scope =&gt;
/// {
///     var registration = SensorRegistry.Subscribe(_sensorId, OnSensorTick);
///     return () =&gt; registration.Dispose();
/// })
/// </code>
/// </remarks>
public sealed class DisposableEffect : ComposableNode
{
    readonly object? _key1, _key2, _key3;
    readonly int _keyCount;
    readonly System.Func<DisposableEffectScope, System.Action> _effect;

    /// <summary>Single-key form.</summary>
    public DisposableEffect(
        object? key1,
        System.Func<DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        _key1 = key1; _keyCount = 1; _effect = effect;
    }

    /// <summary>Two-key form.</summary>
    public DisposableEffect(
        object? key1,
        object? key2,
        System.Func<DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        _key1 = key1; _key2 = key2; _keyCount = 2; _effect = effect;
    }

    /// <summary>Three-key form.</summary>
    public DisposableEffect(
        object? key1,
        object? key2,
        object? key3,
        System.Func<DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        _key1 = key1; _key2 = key2; _key3 = key3; _keyCount = 3; _effect = effect;
    }

    public override void Render(IComposer composer)
    {
        switch (_keyCount)
        {
            case 1: Compose.DisposableEffect(_key1, _effect); break;
            case 2: Compose.DisposableEffect(_key1, _key2, _effect); break;
            default: Compose.DisposableEffect(_key1, _key2, _key3, _effect); break;
        }
    }
}
