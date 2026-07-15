using System.ComponentModel;

namespace AndroidX.Compose;

/// <summary>
/// Marks a delegate parameter whose body executes as composable content.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ComposableContentAttribute : Attribute;
