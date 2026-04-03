using System.Text.Json.Serialization;

namespace SlipSnap.Models;

/// <summary>
/// Configuration for a single toolbar instance.
/// </summary>
public class ToolbarConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ToolbarEdge Edge { get; set; } = ToolbarEdge.Left;

    public bool IsEnabled { get; set; }

    [JsonConverter(typeof(JsonStringEnumListConverter))]
    public List<ToolbarButtonType> Buttons { get; set; } = new(DefaultButtons);

    public double PositionPercent { get; set; } = 0.5;

    private static readonly ToolbarButtonType[] DefaultButtons =
    [
        ToolbarButtonType.StartMenu,
        ToolbarButtonType.TaskView,
        ToolbarButtonType.NextDesktop,
        ToolbarButtonType.PrevDesktop
    ];

    /// <summary>
    /// Clamp and deduplicate values to ensure valid state.
    /// </summary>
    public void Normalize()
    {
        PositionPercent = Math.Clamp(PositionPercent, 0.0, 1.0);

        // Remove duplicates, preserving order
        Buttons = Buttons.Distinct().ToList();

        // If enabled with no buttons, add defaults
        if (IsEnabled && Buttons.Count == 0)
        {
            Buttons = new List<ToolbarButtonType>(DefaultButtons);
        }
    }

    public static ToolbarConfig CreateDefault(ToolbarEdge edge, bool isEnabled = false) =>
        new()
        {
            Edge = edge,
            IsEnabled = isEnabled,
            Buttons = new List<ToolbarButtonType>(DefaultButtons),
            PositionPercent = 0.5
        };
}

/// <summary>
/// JSON converter for List&lt;ToolbarButtonType&gt; using string enum names.
/// </summary>
public class JsonStringEnumListConverter : JsonConverter<List<ToolbarButtonType>>
{
    public override List<ToolbarButtonType> Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        var list = new List<ToolbarButtonType>();
        if (reader.TokenType != System.Text.Json.JsonTokenType.StartArray)
        {
            return list;
        }

        while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (value is not null && Enum.TryParse<ToolbarButtonType>(value, ignoreCase: true, out var buttonType))
                {
                    list.Add(buttonType);
                }
            }
        }

        return list;
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, List<ToolbarButtonType> value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item.ToString());
        }
        writer.WriteEndArray();
    }
}
