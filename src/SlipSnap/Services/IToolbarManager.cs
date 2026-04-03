using SlipSnap.Models;

namespace SlipSnap.Services;

public interface IToolbarManager
{
    void ApplySettings(AppSettings settings);
    void UpdateVisibility(bool isFullscreenPresent, bool isRdpSession);
    void CloseAll();
}
