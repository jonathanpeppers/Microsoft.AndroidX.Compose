
namespace ComposeNet.Samples.Reply;

/// <summary>
/// A static data store of <see cref="Email"/>s. Port of upstream's
/// <c>LocalEmailsDataProvider</c>.
/// </summary>
public static class LocalEmailsDataProvider
{
    static readonly IReadOnlyList<Email> Threads = new List<Email>
    {
        new(id: 8L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(13L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Your update on Google Play Store is live!",
            body: """
              Your update, 0.1.1, is now live on the Play Store and available for your alpha users to start testing.

              Your alpha testers will be automatically notified. If you'd rather send them a link directly, go to your Google Play Console and follow the instructions for obtaining an open alpha testing link.
              """,
            mailbox: MailboxType.Trash,
            createdAt: "3 hours ago"),
        new(id: 5L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(13L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Update to Your Itinerary",
            body: "",
            createdAt: "2 hours ago"),
        new(id: 6L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(10L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Recipe to try",
            body: "Raspberry Pie: We should make this pie recipe tonight! The filling is very quick to put together.",
            createdAt: "2 hours ago",
            mailbox: MailboxType.Sent),
        new(id: 7L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(9L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Delivered",
            body: "Your shoes should be waiting for you at home!",
            createdAt: "2 hours ago"),
        new(id: 9L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(10L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "(No subject)",
            body: """
              Hey,

              Wanted to email and see what you thought of
              """,
            createdAt: "3 hours ago",
            mailbox: MailboxType.Drafts),
        new(id: 1L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(6L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Brunch this weekend?",
            body: """
              I'll be in your neighborhood doing errands and was hoping to catch you for a coffee this Saturday. If you don't have anything scheduled, it would be great to see you! It feels like its been forever.

              If we do get a chance to get together, remind me to tell you about Kim. She stopped over at the house to say hey to the kids and told me all about her trip to Mexico.

              Talk to you soon,

              Ali
              """,
            createdAt: "40 mins ago"),
        new(id: 2L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(5L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Bonjour from Paris",
            body: "Here are some great shots from my trip...",
            attachments: new[]
            {
                new EmailAttachment(Resource.Drawable.paris_1, "Bridge in Paris"),
                new EmailAttachment(Resource.Drawable.paris_2, "Bridge in Paris at night"),
                new EmailAttachment(Resource.Drawable.paris_3, "City street in Paris"),
                new EmailAttachment(Resource.Drawable.paris_4, "Street with bike in Paris"),
            },
            isImportant: true,
            createdAt: "1 hour ago"),
    };

    /// <summary>All emails surfaced to the UI.</summary>
    public static readonly IReadOnlyList<Email> AllEmails = new List<Email>
    {
        new(id: 0L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(9L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Package shipped!",
            body: """
              Cucumber Mask Facial has shipped.

              Keep an eye out for a package to arrive between this Thursday and next Tuesday. If for any reason you don't receive your package before the end of next week, please reach out to us for details on your shipment.

              As always, thank you for shopping with us and we hope you love our specially formulated Cucumber Mask!
              """,
            createdAt: "20 mins ago",
            isStarred: true,
            threads: Threads),
        new(id: 1L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(6L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Brunch this weekend?",
            body: """
              I'll be in your neighborhood doing errands and was hoping to catch you for a coffee this Saturday. If you don't have anything scheduled, it would be great to see you! It feels like its been forever.

              If we do get a chance to get together, remind me to tell you about Kim. She stopped over at the house to say hey to the kids and told me all about her trip to Mexico.

              Talk to you soon,

              Ali
              """,
            createdAt: "40 mins ago",
            threads: Threads),
        new(id: 2L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(5L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Bonjour from Paris",
            body: "Here are some great shots from my trip...",
            attachments: new[]
            {
                new EmailAttachment(Resource.Drawable.paris_1, "Bridge in Paris"),
                new EmailAttachment(Resource.Drawable.paris_2, "Bridge in Paris at night"),
                new EmailAttachment(Resource.Drawable.paris_3, "City street in Paris"),
                new EmailAttachment(Resource.Drawable.paris_4, "Street with bike in Paris"),
            },
            isImportant: true,
            createdAt: "1 hour ago",
            threads: Threads),
        new(id: 3L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(8L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "High school reunion?",
            body: """
              Hi friends,

              I was at the grocery store on Sunday night.. when I ran into Genie Williams! I almost didn't recognize her afer 20 years!

              Anyway, it turns out she is on the organizing committee for the high school reunion this fall. I don't know if you were planning on going or not, but she could definitely use our help in trying to track down lots of missing alums. If you can make it, we're doing a little phone-tree party at her place next Saturday, hoping that if we can find one person, thee more will...
              """,
            createdAt: "2 hours ago",
            mailbox: MailboxType.Sent,
            threads: Threads),
        new(id: 4L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(11L),
            recipients: new[]
            {
                LocalAccountsDataProvider.GetDefaultUserAccount(),
                LocalAccountsDataProvider.GetContactAccountByUid(8L),
                LocalAccountsDataProvider.GetContactAccountByUid(5L),
            },
            subject: "Brazil trip",
            body: """
              Thought we might be able to go over some details about our upcoming vacation.

              I've been doing a bit of research and have come across a few paces in Northern Brazil that I think we should check out. One, the north has some of the most predictable wind on the planet. I'd love to get out on the ocean and kitesurf for a couple of days if we're going to be anywhere near or around Taiba. I hear it's beautiful there and if you're up for it, I'd love to go. Other than that, I haven't spent too much time looking into places along our road trip route. I'm assuming we can find places to stay and things to do as we drive and find places we think look interesting. But... I know you're more of a planner, so if you have ideas or places in mind, lets jot some ideas down!

              Maybe we can jump on the phone later today if you have a second.
              """,
            createdAt: "2 hours ago",
            isStarred: true,
            threads: Threads),
        new(id: 5L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(13L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Update to Your Itinerary",
            body: "",
            createdAt: "2 hours ago",
            threads: Threads),
        new(id: 6L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(10L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Recipe to try",
            body: "Raspberry Pie: We should make this pie recipe tonight! The filling is very quick to put together.",
            createdAt: "2 hours ago",
            mailbox: MailboxType.Sent,
            threads: Threads),
        new(id: 7L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(9L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Delivered",
            body: "Your shoes should be waiting for you at home!",
            createdAt: "2 hours ago",
            threads: Threads),
        new(id: 8L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(13L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Your update on Google Play Store is live!",
            body: """
              Your update, 0.1.1, is now live on the Play Store and available for your alpha users to start testing.

              Your alpha testers will be automatically notified. If you'd rather send them a link directly, go to your Google Play Console and follow the instructions for obtaining an open alpha testing link.
              """,
            mailbox: MailboxType.Trash,
            createdAt: "3 hours ago",
            threads: Threads),
        new(id: 9L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(10L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "(No subject)",
            body: """
              Hey,

              Wanted to email and see what you thought of
              """,
            createdAt: "3 hours ago",
            mailbox: MailboxType.Drafts,
            threads: Threads),
        new(id: 10L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(5L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Try a free TrailGo account",
            body: """
              Looking for the best hiking trails in your area? TrailGo gets you on the path to the outdoors faster than you can pack a sandwich.

              Whether you're an experienced hiker or just looking to get outside for the afternoon, there's a segment that suits you.
              """,
            createdAt: "3 hours ago",
            mailbox: MailboxType.Trash,
            threads: Threads),
        new(id: 11L,
            sender: LocalAccountsDataProvider.GetContactAccountByUid(5L),
            recipients: new[] { LocalAccountsDataProvider.GetDefaultUserAccount() },
            subject: "Free money",
            body: "You've been selected as a winner in our latest raffle! To claim your prize, click on the link.",
            createdAt: "3 hours ago",
            mailbox: MailboxType.Spam,
            threads: Threads),
    };

    /// <summary>Get an <see cref="Email"/> with the given <paramref name="id"/>, or <see langword="null"/>.</summary>
    public static Email? Get(long id) => AllEmails.FirstOrDefault(e => e.Id == id);

    /// <summary>Create a new, blank <see cref="Email"/>.</summary>
    public static Email Create() => new(
        id:        DateTime.UtcNow.Ticks,
        sender:    LocalAccountsDataProvider.GetDefaultUserAccount(),
        subject:   "Monthly hosting party",
        body:      "I would like to invite everyone to our monthly event hosting party",
        createdAt: "Just now");

    /// <summary>Create a new <see cref="Email"/> that is a reply to the email with the given id.</summary>
    public static Email CreateReplyTo(long replyToId)
    {
        var replyTo = Get(replyToId);
        if (replyTo is null) return Create();
        var firstRecipient = replyTo.Recipients.Count > 0
            ? replyTo.Recipients[0]
            : LocalAccountsDataProvider.GetDefaultUserAccount();
        var newRecipients = new List<Account> { replyTo.Sender };
        newRecipients.AddRange(replyTo.Recipients);
        return new Email(
            id:          DateTime.UtcNow.Ticks,
            sender:      firstRecipient,
            recipients:  newRecipients,
            subject:     replyTo.Subject,
            isStarred:   replyTo.IsStarred,
            isImportant: replyTo.IsImportant,
            createdAt:   "Just now",
            body:        "Responding to the above conversation.");
    }

    /// <summary>Get the list of folder labels by which emails can be categorized.</summary>
    public static IReadOnlyList<string> GetAllFolders() => new[]
    {
        "Receipts",
        "Pine Elementary",
        "Taxes",
        "Vacation",
        "Mortgage",
        "Grocery coupons",
    };
}
