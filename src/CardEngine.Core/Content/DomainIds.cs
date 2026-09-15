namespace CardEngine.Core.Content;

public readonly record struct CardId
{
    public CardId(string value) => Value = DomainId.Validate(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct CardTypeId
{
    public CardTypeId(string value) => Value = DomainId.Validate(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct HeroId
{
    public HeroId(string value) => Value = DomainId.Validate(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct HeroPowerId
{
    public HeroPowerId(string value) => Value = DomainId.Validate(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct CardInstanceId
{
    public CardInstanceId(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        Value = value;
    }

    public long Value { get; }
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal static class DomainId
{
    public static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
