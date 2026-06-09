namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Profile avatar — a circular <see cref="Image"/> sourced from a
/// drawable resource id. Port of upstream's <c>ReplyProfileImage</c>.
/// </summary>
public static class ReplyProfileImage
{
    /// <summary>Build a circular avatar from a drawable resource.</summary>
    public static Image Build(int drawableResource, string description) =>
        new(drawableResource, description)
        {
            Modifier = Modifier.Companion
                .Size(40)
                .Clip(Shape.Circle()),
        };

    /// <summary>
    /// "Selected" variant — a filled primary-color circle with a check
    /// mark, used to show that an email row is in multi-select mode.
    /// </summary>
    public static ComposableNode BuildSelected() =>
        new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            return new Box
            {
                Modifier.Companion
                    .Size(40)
                    .Clip(Shape.Circle())
                    .Background(new Color(scheme.Primary)),
                new Icon(Resource.Drawable.ic_check, null)
                {
                    Modifier = Modifier.Companion.Size(24),
                    TintArgb = scheme.OnPrimary,
                },
            };
        });
}
