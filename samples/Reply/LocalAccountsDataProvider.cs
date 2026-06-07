using System.Collections.Generic;
using System.Linq;

namespace ComposeNet.Samples.Reply;

/// <summary>
/// A static data store of <see cref="Account"/>s. This includes both
/// <see cref="Account"/>s owned by the current user and all
/// <see cref="Account"/>s of the current user's contacts. Port of
/// upstream's <c>LocalAccountsDataProvider</c>.
/// </summary>
public static class LocalAccountsDataProvider
{
    public static readonly IReadOnlyList<Account> AllUserAccounts = new List<Account>
    {
        new(id: 1L,  uid: 0L, firstName: "Jeff",  lastName: "Hansen",
            email: "hikingfan@gmail.com",
            altEmail: "hkngfan@outside.com",
            avatar: Resource.Drawable.avatar_10,
            isCurrentAccount: true),
        new(id: 2L,  uid: 0L, firstName: "Jeff",  lastName: "H",
            email: "jeffersonloveshiking@gmail.com",
            altEmail: "jeffersonloveshiking@work.com",
            avatar: Resource.Drawable.avatar_2),
        new(id: 3L,  uid: 0L, firstName: "Jeff",  lastName: "Hansen",
            email: "jeffersonc@google.com",
            altEmail: "jeffersonc@gmail.com",
            avatar: Resource.Drawable.avatar_9),
    };

    static readonly IReadOnlyList<Account> AllUserContactAccounts = new List<Account>
    {
        new(id: 4L,  uid: 1L,  firstName: "Tracy",   lastName: "Alvarez",
            email: "tracealvie@gmail.com",
            altEmail: "tracealvie@gravity.com",
            avatar: Resource.Drawable.avatar_1),
        new(id: 5L,  uid: 2L,  firstName: "Allison", lastName: "Trabucco",
            email: "atrabucco222@gmail.com",
            altEmail: "atrabucco222@work.com",
            avatar: Resource.Drawable.avatar_3),
        new(id: 6L,  uid: 3L,  firstName: "Ali",     lastName: "Connors",
            email: "aliconnors@gmail.com",
            altEmail: "aliconnors@android.com",
            avatar: Resource.Drawable.avatar_5),
        new(id: 7L,  uid: 4L,  firstName: "Alberto", lastName: "Williams",
            email: "albertowilliams124@gmail.com",
            altEmail: "albertowilliams124@chromeos.com",
            avatar: Resource.Drawable.avatar_0),
        new(id: 8L,  uid: 5L,  firstName: "Kim",     lastName: "Alen",
            email: "alen13@gmail.com",
            altEmail: "alen13@mountainview.gov",
            avatar: Resource.Drawable.avatar_7),
        new(id: 9L,  uid: 6L,  firstName: "Google",  lastName: "Express",
            email: "express@google.com",
            altEmail: "express@gmail.com",
            avatar: Resource.Drawable.avatar_express),
        new(id: 10L, uid: 7L,  firstName: "Sandra",  lastName: "Adams",
            email: "sandraadams@gmail.com",
            altEmail: "sandraadams@textera.com",
            avatar: Resource.Drawable.avatar_2),
        new(id: 11L, uid: 8L,  firstName: "Trevor",  lastName: "Hansen",
            email: "trevorhandsen@gmail.com",
            altEmail: "trevorhandsen@express.com",
            avatar: Resource.Drawable.avatar_8),
        new(id: 12L, uid: 9L,  firstName: "Sean",    lastName: "Holt",
            email: "sholt@gmail.com",
            altEmail: "sholt@art.com",
            avatar: Resource.Drawable.avatar_6),
        new(id: 13L, uid: 10L, firstName: "Frank",   lastName: "Hawkins",
            email: "fhawkank@gmail.com",
            altEmail: "fhawkank@thisisme.com",
            avatar: Resource.Drawable.avatar_4),
    };

    /// <summary>Get the current user's default account.</summary>
    public static Account GetDefaultUserAccount() => AllUserAccounts[0];

    /// <summary>Whether the given <see cref="Account.Uid"/> belongs to the current user.</summary>
    public static bool IsUserAccount(long uid) =>
        AllUserAccounts.Any(a => a.Uid == uid);

    /// <summary>Get a contact of the current user with the given <see cref="Account.Id"/>.</summary>
    public static Account GetContactAccountByUid(long accountId) =>
        AllUserContactAccounts.First(a => a.Id == accountId);
}
