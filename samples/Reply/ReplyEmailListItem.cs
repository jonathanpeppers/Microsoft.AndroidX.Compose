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
            var bg = Color.FromPacked(
                isSelected ? scheme.PrimaryContainer :
                isOpened   ? scheme.SecondaryContainer :
                             scheme.SurfaceVariant);

            var surface = new Surface
            {
                Shape = Shape.RoundedCorners(16),
                Color = bg,
                Modifier = Modifier
                    .Padding(horizontal: 16, vertical: 4)
                    .Semantics(s => s.Selected(isSelected))
                    .Clip(Shape.RoundedCorners(16))
                    .CombinedClickable(
                        onClick:     () => navigateToDetail(email.Id),
                        onLongClick: () => toggleSelection(email.Id)),
            };
            surface.Add(new Column
            {
                Modifier.FillMaxWidth().Padding(20),
                BuildHeaderRow(email, isSelected, toggleSelection, scheme, c),
                new Text(email.Subject)
                {
                    Modifier = Modifier.Padding(top: 12, bottom: 8),
                }.WithTypography(ReplyTypography.BodyLarge),
                new Text(email.Body)
                {
                    MaxLines = 2,
                }.WithTypography(ReplyTypography.BodyMedium),
            });
            return surface;
        });

    static Row BuildHeaderRow(
        Email email,
        bool isSelected,
        Action<long> toggleSelection,
        AndroidX.Compose.Material3.ColorScheme scheme,
        AndroidX.Compose.Runtime.IComposer composer)
    {
        var avatar = new AnimatedContent<bool>(
            targetState: isSelected,
            content:     selected => selected
                ? ReplyProfileImage.BuildSelected(
                    () => toggleSelection(email.Id),
                    selectionKey: email.Id)
                : ReplyProfileImage.Build(
                    email.Sender.Avatar,
                    email.Sender.FullName,
                    () => toggleSelection(email.Id),
                    selectionKey: email.Id));

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
                    .WithTypography(ReplyTypography.LabelMedium),
                new Text(email.CreatedAt)
                    .WithTypography(ReplyTypography.LabelMedium),
            },
            new IconButton(onClick: NoOp)
            {
                Modifier
                    .Clip(Shape.Circle())
                    .Background(Color.FromPacked(scheme.SurfaceVariant)),
                new Icon(
                    Resource.Drawable.ic_star_border,
                    composer.StringResource(Resource.String.reply_favorite))
                {
                    Tint = Color.FromPacked(scheme.Outline),
                },
            },
        };
    }

    static void NoOp() { }
}
