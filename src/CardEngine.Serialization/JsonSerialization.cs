using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardEngine.Serialization;

internal static class JsonSerialization
{
    internal static JsonSerializerOptions Options { get; } = CreateOptions();

    internal static T Deserialize<T>(string json, string documentName)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException($"{documentName} JSON is empty.");
    }

    internal static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
