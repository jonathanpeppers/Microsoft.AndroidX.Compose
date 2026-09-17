namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class SwipeDragLifecycle
{
    public bool IsDragging { get; private set; }

    public bool Begin()
    {
        if (IsDragging)
            return false;
        IsDragging = true;
        return true;
    }

    public bool End()
    {
        if (!IsDragging)
            return false;
        IsDragging = false;
        return true;
    }

    public void Cancel() => IsDragging = false;
}
