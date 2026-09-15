using CardEngine.Core.Actions;
using CardEngine.Core.Content;
using CardEngine.Core.Effects;
using CardEngine.Core.Random;
using CardEngine.Core.State;

namespace CardEngine.Core.Match;

public sealed class Match
{
    private readonly GameContent _content;
    private readonly MatchRules _rules;
    private readonly PlayerRuntime[] _players;
    private int _activePlayerIndex;
    private int _turnNumber = 1;
    private long _revision = 1;
    private bool _isFinished;
    private int? _winnerIndex;

    private Match(GameContent content, MatchSetup setup, IRandomSource random)
    {
        _content = content;
        _rules = setup.Rules;
        var nextInstanceId = 1L;
        _players = new PlayerRuntime[setup.Players.Count];
        for (var index = 0; index < setup.Players.Count; index++)
        {
            _players[index] = CreatePlayer(index, setup.Players[index], content, random, ref nextInstanceId);
        }

        foreach (var player in _players)
        {
            for (var count = 0; count < _rules.StartingHandSize; count++)
            {
                Draw(player);
            }
        }
    }

    public MatchState State => CreateState();

    public static Match Create(GameContent content, MatchSetup setup, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(setup);
        setup.Validate(content);
        return new Match(content, setup, new DeterministicRandomSource(seed));
    }

    public MatchView GetViewForPlayer(int playerIndex)
    {
        ValidatePlayerIndex(playerIndex);
        var state = CreateState();
        var players = state.Players
            .Select(player => new PlayerView(
                player.Index,
                player.Hero,
                player.DeckCount,
                player.Hand.Count,
                player.Index == playerIndex ? player.Hand : Array.Empty<CardState>(),
                player.Board,
                player.DiscardCount))
            .ToArray();

        return new MatchView(
            state.Revision,
            state.TurnNumber,
            state.ActivePlayerIndex,
            state.IsFinished,
            state.WinnerIndex,
            playerIndex,
            players);
    }

    public IReadOnlyList<MatchAction> GetLegalActions()
    {
        if (_isFinished)
        {
            return Array.Empty<MatchAction>();
        }

        var player = _players[_activePlayerIndex];
        var actions = new List<MatchAction>(player.Hand.Count + 2);

        foreach (var card in player.Hand)
        {
            var definition = _content.GetCard(card.DefinitionId);
            var cardType = _content.GetCardType(definition.TypeId);
            if (cardType.DestinationAfterPlay != CardDestination.Board || player.Board.Count < _rules.MaximumBoardSize)
            {
                actions.Add(new PlayCardAction(card.InstanceId));
            }
        }

        var heroDefinition = _content.GetHero(player.Hero.DefinitionId);
        if (heroDefinition.HeroPowerId is { } powerId)
        {
            var power = _content.GetHeroPower(powerId);
            if (player.Hero.HeroPowerUsesThisTurn < power.UsesPerTurn)
            {
                actions.Add(new UseHeroPowerAction());
            }
        }

        actions.Add(new EndTurnAction());
        return actions;
    }

    public void Apply(MatchAction action, long? expectedRevision = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (expectedRevision is { } revision && revision != _revision)
        {
            throw new InvalidOperationException($"Action was created for stale revision {revision}; current revision is {_revision}.");
        }

        if (_isFinished)
        {
            throw new InvalidOperationException("The match is already finished.");
        }

        if (!GetLegalActions().Contains(action))
        {
            throw new InvalidOperationException("The action is not legal in the current state.");
        }

        switch (action)
        {
            case PlayCardAction playCard:
                ApplyPlayCard(playCard);
                break;
            case UseHeroPowerAction:
                ApplyHeroPower();
                break;
            case EndTurnAction:
                AdvanceTurn();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        _revision = checked(_revision + 1);
    }

    private void ApplyPlayCard(PlayCardAction action)
    {
        var player = _players[_activePlayerIndex];
        var cardIndex = player.Hand.FindIndex(card => card.InstanceId == action.CardInstanceId);
        var card = player.Hand[cardIndex];
        player.Hand.RemoveAt(cardIndex);

        var definition = _content.GetCard(card.DefinitionId);
        ResolveEffects(definition.Effects, player);

        var cardType = _content.GetCardType(definition.TypeId);
        if (cardType.DestinationAfterPlay == CardDestination.Board)
        {
            player.Board.Add(card);
        }
        else
        {
            player.DiscardPile.Add(card);
        }

        EvaluateTerminal(player.Index);
    }

    private void ApplyHeroPower()
    {
        var player = _players[_activePlayerIndex];
        var heroDefinition = _content.GetHero(player.Hero.DefinitionId);
        var powerId = heroDefinition.HeroPowerId
            ?? throw new InvalidOperationException("The active hero has no hero power.");
        var power = _content.GetHeroPower(powerId);
        player.Hero.HeroPowerUsesThisTurn++;
        ResolveEffects(power.Effects, player);
        EvaluateTerminal(player.Index);
    }

    private void ResolveEffects(IEnumerable<EffectDefinition> effects, PlayerRuntime sourcePlayer)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case DamageOpponentHeroEffect damage:
                    DamageHero(_players[1 - sourcePlayer.Index], damage.Amount);
                    break;
                case HealFriendlyHeroEffect heal:
                    sourcePlayer.Hero.Health = checked(sourcePlayer.Hero.Health + heal.Amount);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported effect type '{effect.GetType().Name}'.");
            }
        }
    }

    private static void DamageHero(PlayerRuntime target, int amount)
    {
        target.Hero.Health = Math.Max(0, target.Hero.Health - amount);
    }

    private void EvaluateTerminal(int sourcePlayerIndex)
    {
        var opponent = _players[1 - sourcePlayerIndex];
        if (opponent.Hero.Health == 0)
        {
            _isFinished = true;
            _winnerIndex = sourcePlayerIndex;
        }
    }

    private void AdvanceTurn()
    {
        if (_turnNumber >= _rules.MaximumTurns)
        {
            FinishByHealth();
            return;
        }

        _activePlayerIndex = 1 - _activePlayerIndex;
        _turnNumber++;
        var player = _players[_activePlayerIndex];
        player.Hero.HeroPowerUsesThisTurn = 0;
        Draw(player);
    }

    private void Draw(PlayerRuntime player)
    {
        if (player.Deck.Count == 0)
        {
            return;
        }

        var topIndex = player.Deck.Count - 1;
        var card = player.Deck[topIndex];
        player.Deck.RemoveAt(topIndex);

        if (player.Hand.Count >= _rules.MaximumHandSize)
        {
            player.DiscardPile.Add(card);
            return;
        }

        player.Hand.Add(card);
    }

    private void FinishByHealth()
    {
        _isFinished = true;
        var firstHealth = _players[0].Hero.Health;
        var secondHealth = _players[1].Hero.Health;
        _winnerIndex = firstHealth == secondHealth ? null : firstHealth > secondHealth ? 0 : 1;
    }

    private MatchState CreateState() => new(
        _revision,
        _turnNumber,
        _activePlayerIndex,
        _isFinished,
        _winnerIndex,
        _players.Select(player => new PlayerState(
            player.Index,
            new HeroState(
                player.Hero.DefinitionId,
                player.Hero.Health,
                player.Hero.HeroPowerUsesThisTurn),
            player.Deck.Count,
            player.Hand.Select(ToState).ToArray(),
            player.Board.Select(ToState).ToArray(),
            player.DiscardPile.Count)).ToArray());

    private static CardState ToState(CardRuntime card) => new(card.InstanceId, card.DefinitionId);

    private static PlayerRuntime CreatePlayer(
        int index,
        PlayerSetup setup,
        GameContent content,
        IRandomSource random,
        ref long nextInstanceId)
    {
        var deck = new List<CardRuntime>(setup.Deck.Count);
        foreach (var cardId in setup.Deck)
        {
            deck.Add(new CardRuntime(new CardInstanceId(nextInstanceId++), cardId));
        }

        random.Shuffle(deck);
        var heroDefinition = content.GetHero(setup.HeroId);
        return new PlayerRuntime(index, new HeroRuntime(setup.HeroId, heroDefinition.StartingHealth), deck);
    }

    private static void ValidatePlayerIndex(int playerIndex)
    {
        if (playerIndex is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(playerIndex));
        }
    }

    private sealed record CardRuntime(CardInstanceId InstanceId, CardId DefinitionId);

    private sealed class HeroRuntime(HeroId definitionId, int health)
    {
        public HeroId DefinitionId { get; } = definitionId;
        public int Health { get; set; } = health;
        public int HeroPowerUsesThisTurn { get; set; }
    }

    private sealed class PlayerRuntime(int index, HeroRuntime hero, List<CardRuntime> deck)
    {
        public int Index { get; } = index;
        public HeroRuntime Hero { get; } = hero;
        public List<CardRuntime> Deck { get; } = deck;
        public List<CardRuntime> Hand { get; } = [];
        public List<CardRuntime> Board { get; } = [];
        public List<CardRuntime> DiscardPile { get; } = [];
    }
}
