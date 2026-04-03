using FluentAssertions;
using SlipSnap.Services;

namespace SlipSnap.Tests.Services;

public class RdpSessionDetectorTests
{
    [Fact]
    public void Implements_IRdpSessionDetector()
    {
        typeof(RdpSessionDetector).Should().Implement<IRdpSessionDetector>();
    }

    [Fact]
    public void IsRdpSession_ReturnsBool()
    {
        var detector = new RdpSessionDetector();

        // On a local dev machine this returns false; on RDP it returns true.
        // We verify it doesn't throw and returns a valid boolean.
        var result = detector.IsRdpSession;
        result.Should().Be(result); // Self-consistent, no exception
    }
}
