using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Docked email search, following the pinned Kotlin ReplyDockedSearchBar.
/// </summary>
public sealed class ReplySearchBar : ComposableNode
{
    readonly IReadOnlyList<Email> _emails;
    readonly Action<long> _onSelected;

    /// <summary>Creates a standalone search bar without a navigation callback.</summary>
    public ReplySearchBar() : this(LocalEmailsDataProvider.AllEmails, static _ => { }) { }

    /// <summary>Searches the supplied inbox and opens the selected email by its stable ID.</summary>
    public ReplySearchBar(IReadOnlyList<Email> emails, Action<long> onSelected)
    {
        ArgumentNullException.ThrowIfNull(emails);
        ArgumentNullException.ThrowIfNull(onSelected);
        _emails = emails;
        _onSelected = onSelected;
    }

    /// <inheritdoc />
    public override void Render(IComposer composer)
    {
        var node = new Composed(c =>
        {
            var session = c.Remember(() => new ReplySearchSession());
            var scope = c.RememberCoroutineScope();
            string searchEmails = c.StringResource(Resource.String.reply_search_emails);
            string search = c.StringResource(Resource.String.reply_search);
            string back = c.StringResource(Resource.String.reply_back);
            string profile = c.StringResource(Resource.String.reply_profile);
            string noHistory = c.StringResource(Resource.String.reply_no_search_history);
            string noItem = c.StringResource(Resource.String.reply_no_item_found);

            ComposableNode Input() => new SearchBarInputField(session.Input, session.Expansion)
            {
                Modifier = Modifier.FillMaxWidth(),
                Placeholder = new Text(searchEmails),
                OnSearch = _ => Run(scope, ct => session.DismissAsync(clearQuery: false, ct)),
                LeadingIcon = new Composed(_ =>
                    session.Expansion.TargetValue.Equals(Material3.SearchBarValue.Expanded)
                        ? new Icon(Resource.Drawable.ic_arrow_back, back)
                        {
                            Modifier = Modifier.Padding(start: 16).Clickable(
                                () => Run(scope, ct => session.DismissAsync(clearQuery: true, ct))),
                        }
                        : new Icon(Resource.Drawable.ic_search, search)
                        {
                            Modifier = Modifier.Padding(start: 16),
                        }),
                TrailingIcon = new Image(Resource.Drawable.avatar_6, profile)
                {
                    Modifier = Modifier.Padding(all: 12).Size(32).Clip(Shape.Circle()),
                },
            };

            return new BoxWithConstraints(bounds =>
            {
                var expanded = new ExpandedDockedSearchBar(session.Expansion)
                {
                    Modifier = Modifier.WidthIn(max: bounds.MaxWidth),
                    InputField = Input(),
                };
                expanded.Add(new Composed(_ =>
                {
                    var query = session.Input.Text;
                    var matches = ReplySearchSession.FindMatches(_emails, query);
                    if (matches.Count == 0)
                        return new Text(query.Length == 0 ? noHistory : noItem)
                        {
                            Modifier = Modifier.Padding(all: 16),
                        };

                    return new LazyColumn<Email>(matches, email => new ListItem
                    {
                        Headline = new Text(email.Subject),
                        Supporting = new Text(email.Sender.FullName),
                        Leading = new Image(email.Sender.Avatar, profile)
                        {
                            Modifier = Modifier.Size(32).Clip(Shape.Circle()),
                        },
                        Modifier = Modifier.Clickable(
                            () => Run(scope, ct => session.SelectAsync(email, _onSelected, ct))),
                    })
                    {
                        Modifier = Modifier.FillMaxWidth(),
                        ContentPadding = new PaddingValues(16),
                        VerticalArrangement = Arrangement.SpacedBy(4.Dp()),
                        Key = static email => email.Id,
                    };
                }));

                return new Box
                {
                    Modifier.FillMaxWidth(),
                    new SearchBar(session.Expansion)
                    {
                        Modifier = Modifier.FillMaxWidth(),
                        InputField = Input(),
                    },
                    expanded,
                };
            })
            {
                Modifier = Modifier.FillMaxWidth().Padding(16),
            };
        });
        node.Render(composer);
    }

    static async void Run(CoroutineScope scope, Func<CancellationToken, Task> action)
    {
        try
        {
            await scope.Launch(action);
        }
        catch (OperationCanceledException)
        {
            // Leaving the search composition cancels its in-flight animation.
        }
    }
}
