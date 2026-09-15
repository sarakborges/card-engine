using CardEngine.Core.Content;

namespace CardEngine.Serialization;

public static class HeroPowerJsonSerializer
{
    public static HeroPowerDefinition Deserialize(string json)
    {
        var document = JsonSerialization.Deserialize<HeroPowerDocument>(json, "hero power");
        return new HeroPowerDefinition(
            new HeroPowerId(document.Id),
            document.UsesPerTurn,
            EffectJsonCodec.ToDefinitions(document.Effects));
    }

    public static string Serialize(HeroPowerDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerialization.Serialize(new HeroPowerDocument
        {
            Id = definition.Id.Value,
            UsesPerTurn = definition.UsesPerTurn,
            Effects = EffectJsonCodec.ToDocuments(definition.Effects),
        });
    }

    private sealed class HeroPowerDocument
    {
        public string Id { get; init; } = string.Empty;
        public int UsesPerTurn { get; init; } = 1;
        public List<EffectDocument> Effects { get; init; } = [];
    }
}
