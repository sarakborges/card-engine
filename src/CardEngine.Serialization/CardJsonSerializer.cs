using CardEngine.Core.Content;

namespace CardEngine.Serialization;

public static class CardJsonSerializer
{
    public static CardDefinition Deserialize(string json)
    {
        var document = JsonSerialization.Deserialize<CardDocument>(json, "card");
        return new CardDefinition(
            new CardId(document.Id),
            new CardTypeId(document.TypeId),
            EffectJsonCodec.ToDefinitions(document.Effects));
    }

    public static string Serialize(CardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerialization.Serialize(new CardDocument
        {
            Id = definition.Id.Value,
            TypeId = definition.TypeId.Value,
            Effects = EffectJsonCodec.ToDocuments(definition.Effects),
        });
    }

    private sealed class CardDocument
    {
        public string Id { get; init; } = string.Empty;
        public string TypeId { get; init; } = string.Empty;
        public List<EffectDocument> Effects { get; init; } = [];
    }
}
