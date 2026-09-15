using System.Text.Json;
using CardEngine.Core.Content;

namespace CardEngine.Serialization;

public static class GameDataDirectoryLayout
{
    public const string RulesFileName = "rules.json";
    public const string CardTypesDirectoryName = "card-types";
    public const string CardsDirectoryName = "cards";
    public const string HeroesDirectoryName = "heroes";
    public const string HeroPowersDirectoryName = "hero-powers";
}

public static class GameDataDirectoryLoader
{
    public static GameData Load(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var root = Path.GetFullPath(rootDirectory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Game content directory '{root}' does not exist.");
        }

        var rules = LoadRequiredFile(
            Path.Combine(root, GameDataDirectoryLayout.RulesFileName),
            RulesJsonSerializer.Deserialize);
        var cardTypes = LoadDefinitions(
            root,
            GameDataDirectoryLayout.CardTypesDirectoryName,
            CardTypeJsonSerializer.Deserialize,
            definition => definition.Id.Value);
        var cards = LoadDefinitions(
            root,
            GameDataDirectoryLayout.CardsDirectoryName,
            CardJsonSerializer.Deserialize,
            definition => definition.Id.Value);
        var heroes = LoadDefinitions(
            root,
            GameDataDirectoryLayout.HeroesDirectoryName,
            HeroJsonSerializer.Deserialize,
            definition => definition.Id.Value);
        var heroPowers = LoadDefinitions(
            root,
            GameDataDirectoryLayout.HeroPowersDirectoryName,
            HeroPowerJsonSerializer.Deserialize,
            definition => definition.Id.Value);

        var content = GameContent.Create(cardTypes, cards, heroes, heroPowers);
        return new GameData(content, rules);
    }

    private static T LoadRequiredFile<T>(string path, Func<string, T> deserialize)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required game content file '{path}' does not exist.", path);
        }

        return DeserializeFile(path, deserialize);
    }

    private static IReadOnlyList<T> LoadDefinitions<T>(
        string root,
        string directoryName,
        Func<string, T> deserialize,
        Func<T, string> idSelector)
    {
        var directory = Path.Combine(root, directoryName);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Required game content directory '{directory}' does not exist.");
        }

        var paths = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        var definitions = new T[paths.Length];
        for (var index = 0; index < paths.Length; index++)
        {
            var path = paths[index];
            var definition = DeserializeFile(path, deserialize);
            ValidateFileNameMatchesId(path, idSelector(definition));
            definitions[index] = definition;
        }

        return definitions;
    }

    private static void ValidateFileNameMatchesId(string path, string id)
    {
        var fileId = Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(fileId, id, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Game content file '{path}' declares id '{id}', but its filename must be '{id}.json'.");
        }
    }

    private static T DeserializeFile<T>(string path, Func<string, T> deserialize)
    {
        try
        {
            return deserialize(File.ReadAllText(path));
        }
        catch (Exception exception) when (
            exception is JsonException or ArgumentException or NotSupportedException)
        {
            throw new InvalidDataException($"Invalid game content file '{path}'.", exception);
        }
    }
}
