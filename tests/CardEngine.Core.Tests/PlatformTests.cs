using CardEngine.Core.Actions;
using CardEngine.Core.Content;
using CardEngine.Core.Effects;
using CardEngine.Core.Match;

namespace CardEngine.Core.Tests;

public sealed class PlatformTests
{
    [Fact]
    public void Content_is_data_driven_and_validates_cross_references()
    {
        var missingType = new CardTypeId("missing");

        var exception = Assert.Throws<ArgumentException>(() => GameContent.Create(
            [new CardTypeDefinition(new("spell"), CardDestination.DiscardPile)],
            [new CardDefinition(new("strike"), missingType, [new DamageOpponentHeroEffect(3)])],
            [new HeroDefinition(new("hero"), 20)]));

        Assert.Contains("missing card type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Same_seed_produces_same_card_instances_in_opening_hands()
    {
        var (content, setup) = CreatePlatform();

        var first = Match.Create(content, setup, 42);
        var second = Match.Create(content, setup, 42);

        Assert.Equal(first.State.Players[0].Hand, second.State.Players[0].Hand);
        Assert.Equal(first.State.Players[1].Hand, second.State.Players[1].Hand);
    }

    [Fact]
    public void Playing_board_card_moves_the_specific_instance_to_board()
    {
        var (content, setup) = CreatePlatform(startingHandSize: 4);
        var match = Match.Create(content, setup, 7);
        var card = match.State.Players[0].Hand
            .First(state => content.GetCard(state.DefinitionId).TypeId == new CardTypeId("unit"));

        match.Apply(new PlayCardAction(card.InstanceId));

        Assert.DoesNotContain(card, match.State.Players[0].Hand);
        Assert.Contains(card, match.State.Players[0].Board);
    }

    [Fact]
    public void Hero_power_is_owned_by_hero_definition_and_limited_per_turn()
    {
        var (content, setup) = CreatePlatform();
        var match = Match.Create(content, setup, 7);
        var initialHealth = match.State.Players[1].Hero.Health;

        match.Apply(new UseHeroPowerAction());

        Assert.Equal(initialHealth - 1, match.State.Players[1].Hero.Health);
        Assert.DoesNotContain(new UseHeroPowerAction(), match.GetLegalActions());
    }

    [Fact]
    public void Player_view_hides_opponent_hand_but_keeps_public_board()
    {
        var (content, setup) = CreatePlatform();
        var match = Match.Create(content, setup, 7);

        var view = match.GetViewForPlayer(0);

        Assert.NotEmpty(view.Players[0].VisibleHand);
        Assert.Empty(view.Players[1].VisibleHand);
        Assert.Equal(match.State.Players[1].Hand.Count, view.Players[1].HandCount);
    }

    [Fact]
    public void Stale_action_revision_is_rejected()
    {
        var (content, setup) = CreatePlatform();
        var match = Match.Create(content, setup, 7);
        var staleRevision = match.State.Revision;

        match.Apply(new EndTurnAction(), staleRevision);

        Assert.Throws<InvalidOperationException>(() =>
            match.Apply(new EndTurnAction(), staleRevision));
    }

    private static (GameContent Content, MatchSetup Setup) CreatePlatform(int startingHandSize = 3)
    {
        var content = GameContent.Create(
            [
                new CardTypeDefinition(new("spell"), CardDestination.DiscardPile),
                new CardTypeDefinition(new("unit"), CardDestination.Board),
            ],
            [
                new CardDefinition(new("strike"), new("spell"), [new DamageOpponentHeroEffect(3)]),
                new CardDefinition(new("guard"), new("unit"), []),
            ],
            [new HeroDefinition(new("hero"), 20, new HeroPowerId("ping"))],
            [new HeroPowerDefinition(new("ping"), 1, [new DamageOpponentHeroEffect(1)])]);

        CardId[] deck = [
            new("strike"),
            new("guard"),
            new("strike"),
            new("guard"),
            new("strike"),
            new("guard"),
        ];
        var rules = new MatchRules(StartingHandSize: startingHandSize, MaximumTurns: 20);
        var setup = new MatchSetup(
            [new PlayerSetup(new("hero"), deck), new PlayerSetup(new("hero"), deck)],
            rules);
        return (content, setup);
    }
}
