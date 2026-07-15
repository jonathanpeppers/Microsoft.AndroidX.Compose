namespace AndroidX.Compose;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class ComposableDirectTargetAttribute(Type containingType, string methodName) : Attribute
{
    public Type ContainingType { get; } = containingType;

    public string MethodName { get; } = methodName;
}
