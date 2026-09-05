namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record NumericalDeckDeadlockOccurred(GameId Game, int Round, int DrawPileCount, int DiscardPileCount,
    int OpeningSetAsideCount, int NumberHistoryCount, int UnresolvedEffectCount) : GameEvent;
