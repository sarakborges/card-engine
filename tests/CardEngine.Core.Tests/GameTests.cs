using CardEngine.Core.Actions;
using CardEngine.Core.Cards;
using CardEngine.Core.Game;

namespace CardEngine.Core.Tests;

public sealed class GameTests
{
    [Fact]
    public void Same_seed_produces_same_opening_hands()
    {
        var config = CreateConfig();

        var first = Game.Create(config, 42);
        var second = Game.Create(config, 42);

        Assert.Equal(
            first.State.Players[0].Hand.Select(card => card.Id),
            second.State.Players[0].Hand.Select(card => card.Id));
        Assert.Equal(
            first.State.Players[1].Hand.Select(card => card.Id),
            second.State.Players[1].Hand.Select(card => card.Id));
    }

    [Fact]
    public void Playing_card_deals_damage_and_advances_turn()
    {
        var strike = new CardDefinition("strike", "Strike", 4);
        var game = Game.Create(new GameConfig([strike], OpeningHandSize: 1), 7);

        game.Apply(new PlayCardAction("strike", 1));

        Assert.Equal(16, game.State.Players[1].Health);
        Assert.Equal(1, game.State.ActivePlayerIndex);
        Assert.Equal(2, game.State.TurnNumber);
    }

    private static GameConfig CreateConfig() => new(
        [
            new CardDefinition("a", "A", 1),
            new CardDefinition("b", "B", 2),
            new CardDefinition("c", "C", 3),
            new CardDefinition("d", "D", 4),
            new CardDefinition("e", "E", 5),
        ],
        OpeningHandSize: 3);
}
