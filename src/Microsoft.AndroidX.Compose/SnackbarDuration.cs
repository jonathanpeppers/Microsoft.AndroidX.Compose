namespace AndroidX.Compose;

/// <summary>Controls how long a snackbar remains visible.</summary>
public enum SnackbarDuration
{
    /// <summary>Show the snackbar for a short period.</summary>
    Short,

    /// <summary>Show the snackbar for a longer period.</summary>
    Long,

    /// <summary>Keep the snackbar visible until dismissed or acted upon.</summary>
    Indefinite,
}
