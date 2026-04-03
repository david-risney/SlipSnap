using SlipSnap.Interop;

namespace SlipSnap.Services;

public class RdpSessionDetector : IRdpSessionDetector
{
    public bool IsRdpSession => NativeMethods.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) != 0;
}
