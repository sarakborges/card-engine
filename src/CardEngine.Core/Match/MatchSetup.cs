using CardEngine.Core.Content;

namespace CardEngine.Core.Match;

public sealed record PlayerSetup(HeroId HeroId, IReadOnlyList<CardId> Deck);

public sealed record MatchSetup(IReadOnlyList<PlayerSetup> Players, MatchRules Rules)
{
    public void Validate(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(Players);
        ArgumentNullException.ThrowIfNull(Rules);
        Rules.Validate();

        if (Players.Count != 2)
        {
            throw new ArgumentException("The initial platform supports exactly two players.", nameof(Players));
        }

        for (var playerIndex = 0; playerIndex < Players.Count; playerIndex++)
        {
            var player = Players[playerIndex];
            ArgumentNullException.ThrowIfNull(player.Deck);

            if (!content.ContainsHero(player.HeroId))
            {
                throw new ArgumentException($"Player {playerIndex} references unknown hero '{player.HeroId}'.");
            }

            if (player.Deck.Count == 0)
            {
                throw new ArgumentException($"Player {playerIndex} deck must contain at least one card.");
            }

            foreach (var cardId in player.Deck)
            {
                if (!content.ContainsCard(cardId))
                {
                    throw new ArgumentException($"Player {playerIndex} deck references unknown card '{cardId}'.");
                }
            }
        }
    }
}
