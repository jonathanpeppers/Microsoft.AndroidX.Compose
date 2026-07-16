namespace AndroidX.Compose.SourceGenerators;

internal readonly struct DefaultArgumentBinding
{
    public DefaultArgumentBinding(string kotlinName, int surfacedParameterIndex)
    {
        KotlinName = kotlinName;
        SurfacedParameterIndex = surfacedParameterIndex;
    }

    public string KotlinName { get; }

    public int SurfacedParameterIndex { get; }
}
