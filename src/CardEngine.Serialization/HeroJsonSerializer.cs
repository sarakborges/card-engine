using CardEngine.Core.Content;

namespace CardEngine.Serialization;

public static class HeroJsonSerializer
{
    public static HeroDefinition Deserialize(string json)
    {
        var document = JsonSerialization.Deserialize<HeroDocument>(json, "hero");
        return new HeroDefinition(
            new HeroId(document.Id),
            document.StartingHealth,
            string.IsNullOrWhiteSpace(document.HeroPowerId)
                ? null
                : new HeroPowerId(document.HeroPowerId));
    }

    public static string Serialize(HeroDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerialization.Serialize(new HeroDocument
        {
            Id = definition.Id.Value,
            StartingHealth = definition.StartingHealth,
            HeroPowerId = definition.HeroPowerId?.Value,
        });
    }

    private sealed class HeroDocument
    {
        public string Id { get; init; } = string.Empty;
        public int StartingHealth { get; init; }
        public string? HeroPowerId { get; init; }
    }
}
