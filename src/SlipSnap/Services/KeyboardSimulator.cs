using SlipSnap.Interop;

namespace SlipSnap.Services;

public class KeyboardSimulator : IKeyboardSimulator
{
    public void SendKeys(params VirtualKey[] keys)
    {
        // Press all keys down in order
        foreach (var key in keys)
        {
            NativeMethods.keybd_event((byte)key, 0, NativeMethods.KEYEVENTF_KEYDOWN, 0);
        }

        // Release all keys in reverse order
        for (int i = keys.Length - 1; i >= 0; i--)
        {
            NativeMethods.keybd_event((byte)keys[i], 0, NativeMethods.KEYEVENTF_KEYUP, 0);
        }
    }
}
