namespace AndroidX.Compose.Samples.Reply;

internal static class ReplyComposeFab
{
    internal static ExtendedFloatingActionButton Build(
        AndroidX.Compose.Runtime.IComposer composer,
        bool expanded)
    {
        var scheme = composer.ColorScheme();
        return new ExtendedFloatingActionButton(onClick: NoOp, expanded: expanded)
        {
            ContainerColor = Color.FromPacked(scheme.TertiaryContainer),
            ContentColor = Color.FromPacked(scheme.OnTertiaryContainer),
            Icon = new Icon(
                Resource.Drawable.ic_edit,
                composer.StringResource(Resource.String.reply_edit)),
            Text = new Text(composer.StringResource(Resource.String.reply_compose)),
        };
    }

    static void NoOp() { }
}
