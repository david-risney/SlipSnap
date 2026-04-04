using System.Text.Json.Serialization;

namespace SlipSnap.Models;

/// <summary>
/// Complete application settings persisted to %AppData%\SlipSnap\settings.json.
/// </summary>
public class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;

    public int OpacityPercent { get; set; } = 80;

    public bool FullscreenOnly { get; set; }

    public bool HideInRdpSession { get; set; } = true;

    public bool TouchMode { get; set; }

    public Dictionary<ToolbarEdge, ToolbarConfig> Toolbars { get; set; } = CreateDefaultToolbars();

    /// <summary>
    /// Clamp and normalize all values to ensure valid state.
    /// </summary>
    public void Normalize()
    {
        OpacityPercent = Math.Clamp(OpacityPercent, 10, 100);

        // Ensure all four edges exist
        foreach (ToolbarEdge edge in Enum.GetValues<ToolbarEdge>())
        {
            if (!Toolbars.ContainsKey(edge))
            {
                Toolbars[edge] = ToolbarConfig.CreateDefault(edge);
            }

            Toolbars[edge].Edge = edge;
            Toolbars[edge].Normalize();
        }
    }

    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        settings.Normalize();
        return settings;
    }

    private static Dictionary<ToolbarEdge, ToolbarConfig> CreateDefaultToolbars()
    {
        var toolbars = new Dictionary<ToolbarEdge, ToolbarConfig>();
        foreach (ToolbarEdge edge in Enum.GetValues<ToolbarEdge>())
        {
            toolbars[edge] = ToolbarConfig.CreateDefault(edge, isEnabled: edge == ToolbarEdge.Left);
        }
        return toolbars;
    }
}
