namespace AndroidX.Compose;

/// <summary>Describes how a snackbar stopped being visible.</summary>
public enum SnackbarResult
{
    /// <summary>The snackbar timed out or was dismissed.</summary>
    Dismissed,

    /// <summary>The user selected the snackbar action.</summary>
    ActionPerformed,
}
