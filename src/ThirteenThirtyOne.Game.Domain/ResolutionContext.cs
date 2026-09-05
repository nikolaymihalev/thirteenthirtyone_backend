namespace ThirteenThirtyOne.Game.Domain;

public abstract record ResolutionContext
{
    private protected ResolutionContext(ContextId id, ContextId? parentId)
    {
        if (id.Value < 1 || (parentId.HasValue && (parentId.Value.Value < 1 || parentId.Value.Value >= id.Value)))
        {
            throw new ArgumentException("Context IDs must be positive and ordered after their parents.");
        }

        Id = id;
        ParentId = parentId;
    }

    public ContextId Id { get; }
    public ContextId? ParentId { get; }
}
