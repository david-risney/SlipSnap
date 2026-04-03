using FluentAssertions;
using SlipSnap.Services;

namespace SlipSnap.Tests.Services;

public class FullscreenDetectorTests
{
    [Fact]
    public void Implements_IFullscreenDetector()
    {
        // FullscreenDetector requires Win32 hooks so we test interface conformance
        typeof(FullscreenDetector).Should().Implement<IFullscreenDetector>();
    }

    [Fact]
    public void Implements_IDisposable()
    {
        typeof(FullscreenDetector).Should().Implement<IDisposable>();
    }

    [Fact]
    public void IsFullscreenWindowPresent_DefaultsFalse()
    {
        using var detector = new FullscreenDetector(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FullscreenDetector>());
        detector.IsFullscreenWindowPresent.Should().BeFalse();
    }

    [Fact]
    public void FullscreenStateChanged_IsNotNull_Subscribable()
    {
        using var detector = new FullscreenDetector(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FullscreenDetector>());

        bool eventRaised = false;
        detector.FullscreenStateChanged += (_, _) => eventRaised = true;

        // No exception means the event is subscribable
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void StopMonitoring_WithoutStart_DoesNotThrow()
    {
        using var detector = new FullscreenDetector(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FullscreenDetector>());

        var act = () => detector.StopMonitoring();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithoutStart_DoesNotThrow()
    {
        var detector = new FullscreenDetector(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FullscreenDetector>());

        var act = () => detector.Dispose();
        act.Should().NotThrow();
    }
}
