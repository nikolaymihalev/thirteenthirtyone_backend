namespace ThirteenThirtyOne.Game.Domain;

public sealed record EngineCompatibility(
    string RulesVersion,
    int EngineVersion,
    int RngAlgorithmVersion,
    int BoundedSamplingAlgorithmVersion,
    int ShuffleAlgorithmVersion,
    int StateHashVersion)
{
    public static EngineCompatibility V1 { get; } = new("1.1", 1, 1, 1, 1, 1);
}
