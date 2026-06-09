namespace Microsoft.AndroidX.Compose.Gallery.Demos.Shared;

/// <summary>Pastel palette reused across carousel/list demos so cells are visually distinct.</summary>
internal static class Palette
{
    /// <summary>Eight Material-feel pastel colors, cycled with <c>i % Length</c>.</summary>
    public static readonly Color[] Pastels =
    {
        Color.FromRgb(0xD0, 0xBC, 0xFF),
        Color.FromRgb(0xB3, 0xE5, 0xFC),
        Color.FromRgb(0xC8, 0xE6, 0xC9),
        Color.FromRgb(0xFF, 0xE0, 0xB2),
        Color.FromRgb(0xEF, 0xB8, 0xC8),
        Color.FromRgb(0xFF, 0xCD, 0xD2),
        Color.FromRgb(0xCC, 0xC2, 0xDC),
        Color.FromRgb(0xD7, 0xCC, 0xC8),
    };
}
