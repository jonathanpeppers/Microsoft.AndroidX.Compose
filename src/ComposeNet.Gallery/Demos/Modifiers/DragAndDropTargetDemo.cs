using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Modifiers;

/// <summary>DragAndDropTarget — drop zone that accepts text or image drags from other apps.</summary>
public static class DragAndDropTargetDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-drag-and-drop-target",
        CategoryId:  "modifiers",
        Title:       "Drag-and-drop target",
        Description: "A drop zone listening for text or image drags from other apps. Each drop appends a row showing the MIME type and ClipData URI / text.",
        Build:       () => new Composed(c =>
        {
            var drops = Compose.Remember(() => new MutableStateList<string>());
            var target = Compose.Remember(() => new DragAndDropTarget(e =>
            {
                var clip = e.AndroidDragEvent.ClipData;
                string payload;
                if (clip is null || clip.ItemCount == 0)
                    payload = "(no clip data)";
                else
                {
                    var item = clip.GetItemAt(0);
                    payload = item?.Uri?.ToString()
                        ?? item?.Text?.ToString()
                        ?? "(empty item)";
                }
                var mime = e.MimeTypes.Count > 0 ? e.MimeTypes[0] : "?";
                drops.Insert(0, $"{mime} → {payload}");
                return true;
            }));

            var rows = new Column();
            rows.Add(new Text($"Drops received: {drops.Count}"));
            for (int i = 0; i < drops.Count; i++)
                rows.Add(new Text(drops[i]) { Modifier = Modifier.Companion.Padding(0, 2) });

            return new Column
            {
                new Box
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Height(96)
                        .Padding(8)
                        .Background(Color.FromRgb(0xE1, 0xBE, 0xE7))
                        .Border(2, Color.FromRgb(0x6A, 0x1B, 0x9A))
                        .DragAndDropTarget(
                            shouldStartDragAndDrop: e =>
                            {
                                foreach (var m in e.MimeTypes)
                                    if (m.StartsWith("image/", System.StringComparison.Ordinal)
                                        || m.StartsWith("text/", System.StringComparison.Ordinal))
                                        return true;
                                return false;
                            },
                            target: target),
                    new Text("Drop text or images here") { Color = Color.Black },
                },
                rows,
                new Button(onClick: () => drops.Clear()) { new Text("Clear") },
            };
        }));
}
