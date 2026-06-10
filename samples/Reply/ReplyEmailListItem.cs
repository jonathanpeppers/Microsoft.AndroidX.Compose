namespace AndroidX.Compose.Samples.Reply;

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
            var scheme = c.ColorScheme();
            long bg =
                isSelected ? scheme.PrimaryContainer :
                isOpened   ? scheme.SecondaryContainer :
                             scheme.SurfaceVariant;

            return new Card
            {
                Modifier
                    .Padding(horizontal: 16, vertical: 4)
                    .Background(bg)
                    .CombinedClickable(
                        onClick:     () => navigateToDetail(email.Id),
                        onLongClick: () => toggleSelection(email.Id)),
                new Column
                {
                    Modifier.FillMaxWidth().Padding(20),
                    BuildHeaderRow(email, isSelected, toggleSelection, scheme),
                    new Text(email.Subject)
                    {
                        FontSize = 16,
                        Modifier = Modifier.Padding(top: 12, bottom: 8),
                    },
                    new Text(email.Body)
                    {
                        FontSize = 14,
                        MaxLines = 2,
                    },
                },
            };
        });

    static Row BuildHeaderRow(Email email, bool isSelected, Action<long> toggleSelection, AndroidX.Compose.Material3.ColorScheme scheme)
    {
        var avatar = new AnimatedContent<bool>(
            targetState: isSelected,
            content:     selected => selected
                ? ReplyProfileImage.BuildSelected()
                : ReplyProfileImage.Build(email.Sender.Avatar, email.Sender.FullName));

        return new Row
        {
            Modifier.FillMaxWidth(),
            avatar,
            new Column
            {
                Modifier
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
                Modifier
                    .Clip(Shape.Circle())
                    .Background(scheme.SurfaceVariant),
                new Icon(Resource.Drawable.ic_star_border, "Favorite")
                {
                    Tint = scheme.Outline,
                },
            },
        };
    }

    static void NoOp() { }
}
