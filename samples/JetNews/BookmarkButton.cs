using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Bookmark toggle. Reads the <see cref="BookmarksViewModel"/> and
/// renders <c>ic_bookmark</c> (outline) or <c>ic_bookmark_filled</c>
/// based on membership. Tapping flips the bit.
/// </summary>
internal static class BookmarkButton
{
    public static IconToggleButton Build(string postId, BookmarksViewModel bookmarks) =>
        new(
            @checked:         bookmarks.Contains(postId),
            onCheckedChange:  isChecked => bookmarks.Set(postId, isChecked))
        {
            new Icon(
                bookmarks.Contains(postId)
                    ? Resource.Drawable.ic_bookmark_filled
                    : Resource.Drawable.ic_bookmark,
                bookmarks.Contains(postId) ? "Remove bookmark" : "Add bookmark"),
        };
}
