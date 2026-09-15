namespace AndroidX.Compose;

internal enum SharedStatePhase
{
    Unclaimed,
    Initializing,
    Published,
    Failed,
    Retired,
}
