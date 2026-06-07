using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Bookmark toggle. Reads the <see cref="MutableStateList{T}"/> of
/// bookmarked post ids and renders <c>ic_bookmark</c> (outline) or
/// <c>ic_bookmark_filled</c> based on membership. Tapping flips the
/// bit.
/// </summary>
internal static class BookmarkButton
{
    public static IconToggleButton Build(string postId, MutableStateList<string> bookmarks) =>
        new(
            @checked:         bookmarks.Contains(postId),
            onCheckedChange:  isChecked =>
            {
                if (isChecked)
                {
                    if (!bookmarks.Contains(postId))
                        bookmarks.Add(postId);
                }
                else
                {
                    bookmarks.Remove(postId);
                }
            })
        {
            new Icon(
                bookmarks.Contains(postId)
                    ? Resource.Drawable.ic_bookmark_filled
                    : Resource.Drawable.ic_bookmark,
                bookmarks.Contains(postId) ? "Remove bookmark" : "Add bookmark"),
        };
}
