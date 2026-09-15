namespace CardEngine.Core.Effects;

public abstract record EffectDefinition
{
    internal EffectDefinition()
    {
    }
}

public sealed record DamageOpponentHeroEffect(int Amount) : EffectDefinition;

public sealed record HealFriendlyHeroEffect(int Amount) : EffectDefinition;
