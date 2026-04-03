namespace SlipSnap.Services;

public interface IFullscreenDetector
{
    bool IsFullscreenWindowPresent { get; }
    event EventHandler<bool>? FullscreenStateChanged;
    void StartMonitoring();
    void StopMonitoring();
}
