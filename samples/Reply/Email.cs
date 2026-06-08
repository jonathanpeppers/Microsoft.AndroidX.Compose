using System.Collections.Generic;

namespace ComposeNet.Samples.Reply;

/// <summary>
/// A single email. Port of upstream's <c>Email</c> data class.
/// </summary>
public sealed class Email
{
    public Email(
        long id,
        Account sender,
        string subject,
        string body,
        string createdAt,
        IReadOnlyList<Account>?         recipients  = null,
        IReadOnlyList<EmailAttachment>? attachments = null,
        bool                            isImportant = false,
        bool                            isStarred   = false,
        MailboxType                     mailbox     = MailboxType.Inbox,
        IReadOnlyList<Email>?           threads     = null)
    {
        Id          = id;
        Sender      = sender;
        Recipients  = recipients  ?? System.Array.Empty<Account>();
        Subject     = subject;
        Body        = body;
        Attachments = attachments ?? System.Array.Empty<EmailAttachment>();
        IsImportant = isImportant;
        IsStarred   = isStarred;
        Mailbox     = mailbox;
        CreatedAt   = createdAt;
        Threads     = threads     ?? System.Array.Empty<Email>();
    }

    /// <summary>Stable identity used by <c>LazyColumn</c> keys.</summary>
    public long Id { get; }

    public Account                       Sender      { get; }
    public IReadOnlyList<Account>        Recipients  { get; }
    public string                        Subject     { get; }
    public string                        Body        { get; }
    public IReadOnlyList<EmailAttachment> Attachments { get; }
    public bool                          IsImportant { get; set; }
    public bool                          IsStarred   { get; set; }
    public MailboxType                   Mailbox     { get; set; }
    public string                        CreatedAt   { get; }
    public IReadOnlyList<Email>          Threads     { get; }
}
