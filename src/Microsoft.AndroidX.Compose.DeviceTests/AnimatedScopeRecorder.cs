using Android.Runtime;
using AndroidX.Compose.Animation;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.UI;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Records the actual values supplied by Kotlin's native default dispatcher.</summary>
[Register("net/compose/devicetests/AnimatedScopeRecorder")]
public sealed class AnimatedScopeRecorder : Java.Lang.Object, IAnimatedVisibilityScope
{
    internal EnterTransition? Enter;
    internal ExitTransition? Exit;
    internal string? Label;
    internal int Calls;

    /// <summary>No animation is run by this argument-recording scope.</summary>
    public Transition Transition => throw new InvalidOperationException("The argument recorder has no native transition.");

    /// <summary>Receives arguments after Kotlin has applied its default mask.</summary>
    public IModifier AnimateEnterExit(IModifier modifier, EnterTransition enter, ExitTransition exit, string label)
    {
        Enter = enter;
        Exit = exit;
        Label = label;
        Calls++;
        return modifier;
    }
}
