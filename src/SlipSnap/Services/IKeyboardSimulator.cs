using SlipSnap.Interop;

namespace SlipSnap.Services;

public interface IKeyboardSimulator
{
    void SendKeys(params VirtualKey[] keys);
}
