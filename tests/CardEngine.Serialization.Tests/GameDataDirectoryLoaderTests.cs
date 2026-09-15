using Xunit;

namespace CardEngine.Serialization.Tests;

public sealed class GameDataDirectoryLoaderTests
{
    [Fact]
    public void Loads_content_from_one_json_file_per_entity()
    {
        var root = CreateRoot();
        try
        {
            Write(root, "rules.json", """
                {
                  "startingHandSize": 4,
                  "maximumHandSize": 10,
                  "maximumBoardSize": 7,
                  "maximumTurns": 40
                }
                """);
            Write(root, "card-types/spell.json", """
                {
                  "id": "spell",
                  "destinationAfterPlay": "discardPile"
                }
                """);
            Write(root, "cards/fireball.json", """
                {
                  "id": "fireball",
                  "typeId": "spell",
                  "effects": [
                    { "type": "damageOpponentHero", "amount": 6 }
                  ]
                }
                """);
            Write(root, "hero-powers/fireblast.json", """
                {
                  "id": "fireblast",
                  "usesPerTurn": 1,
                  "effects": [
                    { "type": "damageOpponentHero", "amount": 1 }
                  ]
                }
                """);
            Write(root, "heroes/mage.json", """
                {
                  "id": "mage",
                  "startingHealth": 30,
                  "heroPowerId": "fireblast"
                }
                """);

            var data = GameDataDirectoryLoader.Load(root);

            Assert.Equal(4, data.Rules.StartingHandSize);
            Assert.Equal("mage", data.Content.GetHero(new("mage")).Id.Value);
            Assert.Equal("fireblast", data.Content.GetHeroPower(new("fireblast")).Id.Value);
            Assert.Equal("fireball", data.Content.GetCard(new("fireball")).Id.Value);
            Assert.Equal("spell", data.Content.GetCardType(new("spell")).Id.Value);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rejects_entity_when_filename_does_not_match_declared_id()
    {
        var root = CreateRoot();
        try
        {
            Write(root, "rules.json", """
                {
                  "startingHandSize": 3,
                  "maximumHandSize": 10,
                  "maximumBoardSize": 7,
                  "maximumTurns": 40
                }
                """);
            Write(root, "card-types/spell.json", """
                {
                  "id": "spell",
                  "destinationAfterPlay": "discardPile"
                }
                """);
            Write(root, "heroes/not-mage.json", """
                {
                  "id": "mage",
                  "startingHealth": 30
                }
                """);

            var error = Assert.Throws<InvalidDataException>(() => GameDataDirectoryLoader.Load(root));

            Assert.Contains("mage.json", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"card-engine-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "card-types"));
        Directory.CreateDirectory(Path.Combine(root, "cards"));
        Directory.CreateDirectory(Path.Combine(root, "heroes"));
        Directory.CreateDirectory(Path.Combine(root, "hero-powers"));
        return root;
    }

    private static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
