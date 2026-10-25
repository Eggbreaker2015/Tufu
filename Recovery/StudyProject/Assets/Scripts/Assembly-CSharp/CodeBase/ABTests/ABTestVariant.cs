namespace Codebase.ABTests
{
public readonly struct ABTestVariant
{
    private readonly string variant;
    public string Value => variant;
    public ABTestVariant(string variant) { this.variant = variant; }
    public bool Is(string expectedVariant) => variant == expectedVariant;
    public static implicit operator string(ABTestVariant variant) => variant.variant;
    public override string ToString() => variant ?? "null";
}
}
