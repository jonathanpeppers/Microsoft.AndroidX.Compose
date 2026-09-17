namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Profile avatar — a circular <see cref="Image"/> sourced from a
/// drawable resource id. Port of upstream's <c>ReplyProfileImage</c>.
/// </summary>
public static class ReplyProfileImage
{
    /// <summary>Build a circular avatar from a drawable resource.</summary>
    public static Image Build(
        int drawableResource,
        string description,
        Action? toggleSelection = null,
        object? selectionKey = null)
    {
        var modifier = Modifier.Size(40).Clip(Shape.Circle());
        if (toggleSelection is not null)
        {
            modifier = modifier
                .DetectTapGestures(onTap: _ => toggleSelection(), key: selectionKey)
                .Semantics(s => s.OnClick(toggleSelection));
        }
        return new Image(drawableResource, description) { Modifier = modifier };
    }

    /// <summary>
    /// "Selected" variant — a filled primary-color circle with a check
    /// mark, used to show that an email row is in multi-select mode.
    /// </summary>
    public static ComposableNode BuildSelected(
        Action? toggleSelection = null,
        object? selectionKey = null) =>
        new Composed(c =>
        {
            var scheme = c.ColorScheme();
            var modifier = Modifier
                .Size(40)
                .Clip(Shape.Circle())
                .Background(Color.FromPacked(scheme.Primary));
            if (toggleSelection is not null)
            {
                modifier = modifier
                    .DetectTapGestures(onTap: _ => toggleSelection(), key: selectionKey)
                    .Semantics(s => s.OnClick(toggleSelection));
            }
            return new Box
            {
                modifier,
                new Icon(Resource.Drawable.ic_check, null)
                {
                    Modifier = Modifier.Size(24),
                    Tint = Color.FromPacked(scheme.OnPrimary),
                },
            };
        });
}
