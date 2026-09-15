using AndroidX.Compose.Samples.Jetchat;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class RecordingGestureStateTests
{
    [Theory]
    [InlineData(-199.9f, 0, false)]
    [InlineData(-200, 80, true)]
    [InlineData(-200, -80, true)]
    [InlineData(-200, 80.1f, false)]
    [InlineData(-200, -80.1f, false)]
    [InlineData(200, 0, false)]
    public void PinnedThresholds_AreDirectionalAndDensityScaled(float x, float y, bool cancels)
    {
        var state = new RecordingGestureState();
        state.Start();
        Assert.Equal(cancels, state.Move(x * 2.625f, y * 2.625f, 2.625f));
        Assert.Equal(!cancels, state.End());
        Assert.False(state.End());
    }

    [Fact]
    public void MovementAccumulates_AndCancelledGestureCannotCommit()
    {
        var state = new RecordingGestureState();
        Assert.False(state.Move(-300, 0, 1));
        Assert.False(state.End());
        state.Start();
        Assert.False(state.Move(-210, 81, 1));
        Assert.True(state.Move(0, -1, 1));
        Assert.False(state.Move(-10, 0, 1));
        Assert.False(state.End());
        state.Start();
        Assert.Equal(0, state.Horizontal);
        Assert.Equal(0, state.Vertical);
        Assert.False(state.Move(40, 90, 1));
        Assert.True(state.End());
    }
}
