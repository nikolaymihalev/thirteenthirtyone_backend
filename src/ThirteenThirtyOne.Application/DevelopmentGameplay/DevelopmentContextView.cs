namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record DevelopmentContextView(long ContextId, long? ParentContextId, string ContextType,
    string? DrawKind, string? RecipientPlayerId, int? RemainingNumbers, string? EffectKind,
    string? EffectDrawerPlayerId, string? DecisionOwnerPlayerId, string? EffectTargetPlayerId);
