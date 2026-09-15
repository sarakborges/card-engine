using CardEngine.Core.Actions;
using CardEngine.Core.Cards;
using CardEngine.Core.Random;
using CardEngine.Core.State;

namespace CardEngine.Core.Game;

public sealed class Game
{
    private readonly GameConfig _config;
    private readonly List<PlayerRuntime> _players;
    private int _activePlayerIndex;
    private int _turnNumber = 1;
    private bool _isFinished;
    private int? _winnerIndex;

    private Game(GameConfig config, IRandomSource random)
    {
        _config = config;
        _players = [CreatePlayer(0, config, random), CreatePlayer(1, config, random)];
    }

    public GameState State => new(
        _players.Select(player => new PlayerState(
            player.Index,
            player.Health,
            player.Hand.ToArray(),
            player.Deck.Count)).ToArray(),
        _activePlayerIndex,
        _turnNumber,
        _isFinished,
        _winnerIndex);

    public static Game Create(GameConfig config, int seed)
    {
        Validate(config);
        return new Game(config, new SeededRandomSource(seed));
    }

    public IReadOnlyList<GameAction> GetLegalActions()
    {
        if (_isFinished)
        {
            return Array.Empty<GameAction>();
        }

        var player = _players[_activePlayerIndex];
        if (player.Hand.Count == 0)
        {
            return [new PassAction()];
        }

        var target = 1 - _activePlayerIndex;
        return player.Hand
            .Select(card => card.Id)
            .Distinct(StringComparer.Ordinal)
            .Select(cardId => (GameAction)new PlayCardAction(cardId, target))
            .ToArray();
    }

    public void Apply(GameAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_isFinished)
        {
            throw new InvalidOperationException("The game is already finished.");
        }

        var legalActions = GetLegalActions();
        if (!legalActions.Contains(action))
        {
            throw new InvalidOperationException("The action is not legal in the current state.");
        }

        switch (action)
        {
            case PlayCardAction play:
                ApplyPlayCard(play);
                break;
            case PassAction:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        if (_isFinished)
        {
            return;
        }

        if (_turnNumber >= _config.MaxTurns)
        {
            FinishByHealth();
            return;
        }

        AdvanceTurn();
    }

    private void ApplyPlayCard(PlayCardAction action)
    {
        var player = _players[_activePlayerIndex];
        var cardIndex = player.Hand.FindIndex(card =>
            string.Equals(card.Id, action.CardId, StringComparison.Ordinal));
        var card = player.Hand[cardIndex];
        player.Hand.RemoveAt(cardIndex);

        var target = _players[action.TargetPlayerIndex];
        target.Health = Math.Max(0, target.Health - card.Damage);

        if (target.Health == 0)
        {
            _isFinished = true;
            _winnerIndex = _activePlayerIndex;
        }
    }

    private void AdvanceTurn()
    {
        _activePlayerIndex = 1 - _activePlayerIndex;
        _turnNumber++;
        _players[_activePlayerIndex].Draw();
    }

    private void FinishByHealth()
    {
        _isFinished = true;
        var first = _players[0].Health;
        var second = _players[1].Health;
        _winnerIndex = first == second ? null : first > second ? 0 : 1;
    }

    private static PlayerRuntime CreatePlayer(int index, GameConfig config, IRandomSource random)
    {
        var deck = config.Deck.ToList();
        random.Shuffle(deck);
        var player = new PlayerRuntime(index, config.StartingHealth, deck);

        for (var i = 0; i < config.OpeningHandSize; i++)
        {
            player.Draw();
        }

        return player;
    }

    private static void Validate(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Deck.Count == 0)
        {
            throw new ArgumentException("The deck must contain at least one card.", nameof(config));
        }

        if (config.StartingHealth <= 0 || config.OpeningHandSize < 0 || config.MaxTurns <= 0)
        {
            throw new ArgumentException("Game configuration values are invalid.", nameof(config));
        }

        if (config.Deck.Any(card => string.IsNullOrWhiteSpace(card.Id) || card.Damage < 0))
        {
            throw new ArgumentException("All cards require an id and non-negative damage.", nameof(config));
        }
    }

    private sealed class PlayerRuntime(int index, int health, List<CardDefinition> deck)
    {
        public int Index { get; } = index;

        public int Health { get; set; } = health;

        public List<CardDefinition> Deck { get; } = deck;

        public List<CardDefinition> Hand { get; } = [];

        public void Draw()
        {
            if (Deck.Count == 0)
            {
                return;
            }

            var last = Deck.Count - 1;
            Hand.Add(Deck[last]);
            Deck.RemoveAt(last);
        }
    }
}
