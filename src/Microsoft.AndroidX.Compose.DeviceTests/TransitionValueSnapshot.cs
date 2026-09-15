using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record TransitionValueSnapshot(
    int Phase,
    Transition<TransitionTestState> Transition,
    TransitionAnimation<float> Scale,
    TransitionAnimation<float> Alpha,
    TransitionAnimation<Color> Color,
    TransitionTestState Current,
    TransitionTestState Target,
    bool Running,
    bool Idle,
    float ScaleValue,
    float AlphaValue,
    Color ColorValue,
    object TailIdentity);
