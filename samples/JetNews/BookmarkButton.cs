namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Bookmark toggle. Reads the <see cref="BookmarksViewModel"/> and
/// renders <c>ic_bookmark</c> (outline) or <c>ic_bookmark_filled</c>
/// based on membership. Tapping flips the bit and fires the optional
/// <c>onToggled</c> callback so screens can show snackbar feedback.
/// </summary>
internal static class BookmarkButton
{
    public static ComposableNode Build(
        string postId,
        BookmarksViewModel bookmarks,
        Action<bool>? onToggled = null) =>
        new Composed(c =>
        {
            bool isBookmarked = bookmarks.Contains(postId);
            string addBookmark = c.StringResource(Resource.String.cd_add_bookmark);
            string removeBookmark = c.StringResource(Resource.String.cd_remove_bookmark);
            return new IconToggleButton(
                @checked: isBookmarked,
                onCheckedChange: isChecked =>
                {
                    bookmarks.Set(postId, isChecked);
                    onToggled?.Invoke(isChecked);
                })
            {
                new Icon(
                isBookmarked
                    ? Resource.Drawable.ic_bookmark_filled
                    : Resource.Drawable.ic_bookmark,
                isBookmarked ? removeBookmark : addBookmark),
            };
        });
}
