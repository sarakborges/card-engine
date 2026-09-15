using System.Text.Json;
using CardEngine.Core.Cards;

namespace CardEngine.Serialization;

public static class CardCatalogSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static IReadOnlyList<CardDefinition> Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<List<CardDefinition>>(json, Options)
            ?? throw new JsonException("Card catalog JSON did not contain a card array.");
    }

    public static string Serialize(IReadOnlyList<CardDefinition> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        return JsonSerializer.Serialize(cards, Options);
    }
}
