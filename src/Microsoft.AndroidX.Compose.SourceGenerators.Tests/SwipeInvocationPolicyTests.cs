using Microsoft.AndroidX.Compose.Maui.Handlers;
using Microsoft.Maui;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public sealed class SwipeInvocationPolicyTests
{
    [Theory]
    [InlineData(SwipeMode.Reveal, SwipeBehaviorOnInvoked.Auto, false)]
    [InlineData(SwipeMode.Execute, SwipeBehaviorOnInvoked.Auto, true)]
    [InlineData(SwipeMode.Reveal, SwipeBehaviorOnInvoked.Close, false)]
    [InlineData(SwipeMode.Execute, SwipeBehaviorOnInvoked.Close, false)]
    [InlineData(SwipeMode.Reveal, SwipeBehaviorOnInvoked.RemainOpen, true)]
    [InlineData(SwipeMode.Execute, SwipeBehaviorOnInvoked.RemainOpen, true)]
    public void InvocationBehavior_IsModeAware(
        SwipeMode mode,
        SwipeBehaviorOnInvoked behavior,
        bool expected) =>
        Assert.Equal(
            expected,
            SwipeInvocationPolicy.ShouldRemainOpen(mode, behavior));
}
