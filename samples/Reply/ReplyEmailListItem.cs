namespace Microsoft.AndroidX.Compose.Samples.Reply;

/// <summary>
/// A single email row in the inbox list. Port of upstream's
/// <c>ReplyEmailListItem</c>.
/// </summary>
public static class ReplyEmailListItem
{
    /// <summary>Build the row for the given <see cref="Email"/>.</summary>
    public static ComposableNode Build(
        Email          email,
        Action<long>   navigateToDetail,
        Action<long>   toggleSelection,
        bool           isOpened   = false,
        bool           isSelected = false) =>
        new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            long bg =
                isSelected ? scheme.PrimaryContainer :
                isOpened   ? scheme.SecondaryContainer :
                             scheme.SurfaceVariant;

            return new Card
            {
                Modifier.Companion
                    .Padding(horizontal: 16, vertical: 4)
                    .Background(new Color(bg))
                    .CombinedClickable(
                        onClick:     () => navigateToDetail(email.Id),
                        onLongClick: () => toggleSelection(email.Id)),
                new Column
                {
                    Modifier.Companion.FillMaxWidth().Padding(20),
                    BuildHeaderRow(email, isSelected, toggleSelection, scheme),
                    new Text(email.Subject)
                    {
                        FontSize = 16,
                        Modifier = Modifier.Companion.Padding(top: 12, bottom: 8, start: 0, end: 0),
                    },
                    new Text(email.Body)
                    {
                        FontSize = 14,
                        MaxLines = 2,
                    },
                },
            };
        });

    static Row BuildHeaderRow(Email email, bool isSelected, Action<long> toggleSelection, global::AndroidX.Compose.Material3.ColorScheme scheme)
    {
        var avatar = new AnimatedContent<bool>(
            targetState: isSelected,
            content:     selected => selected
                ? ReplyProfileImage.BuildSelected()
                : ReplyProfileImage.Build(email.Sender.Avatar, email.Sender.FullName));

        return new Row
        {
            Modifier.Companion.FillMaxWidth(),
            avatar,
            new Column
            {
                Modifier.Companion
                    .Weight(1f)
                    .Padding(horizontal: 12, vertical: 4),
                new Text(email.Sender.FirstName)
                {
                    FontSize = 12,
                },
                new Text(email.CreatedAt)
                {
                    FontSize = 12,
                },
            },
            new IconButton(onClick: NoOp)
            {
                Modifier.Companion
                    .Clip(Shape.Circle())
                    .Background(new Color(scheme.SurfaceVariant)),
                new Icon(Resource.Drawable.ic_star_border, "Favorite")
                {
                    TintArgb = scheme.Outline,
                },
            },
        };
    }

    static void NoOp() { }
}
