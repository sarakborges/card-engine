using CardEngine.Core.Content;

namespace CardEngine.Serialization;

public static class CardTypeJsonSerializer
{
    public static CardTypeDefinition Deserialize(string json)
    {
        var document = JsonSerialization.Deserialize<CardTypeDocument>(json, "card type");
        return new CardTypeDefinition(
            new CardTypeId(document.Id),
            document.DestinationAfterPlay);
    }

    public static string Serialize(CardTypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerialization.Serialize(new CardTypeDocument
        {
            Id = definition.Id.Value,
            DestinationAfterPlay = definition.DestinationAfterPlay,
        });
    }

    private sealed class CardTypeDocument
    {
        public string Id { get; init; } = string.Empty;
        public CardDestination DestinationAfterPlay { get; init; }
    }
}
