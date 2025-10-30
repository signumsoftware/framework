LINQ Appendix

Here are the entity classes used as the Data Model, following current Signum Framework conventions:

```csharp
[Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
public class BugEntity : Entity
{
    public string Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public decimal? Hours { get; private set; }
    public Status Status { get; set; }

    [NotNullValidator, ImplementedBy(typeof(CustomerEntity), typeof(DeveloperEntity))]
    public IBugDiscoverer Discoverer { get; set; }

    public DeveloperEntity? Fixer { get; set; }

    [NotNullValidator]
    public Lite<ProjectEntity> Project { get; set; }

    public MList<CommentEntity> Comments { get; set; } = [];

    protected override void PreSaving(ref bool graphModified)
    {
        CalculateHours();
    }

    private void CalculateHours()
    {
        // Calculation logic here
        Notify(() => Hours);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Description);
}

[Serializable]
public class CommentEntity : EmbeddedEntity
{
    public string Text { get; set; }
    public DateTime Date { get; set; }

    [ImplementedBy(typeof(CustomerEntity), typeof(DeveloperEntity))]
    public IBugDiscoverer Writer { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Writer.Name + ": " + Text);
}

public enum Status
{
    Open,
    Fixed,
    Rejected,
}

[Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
public class ProjectEntity : Entity
{
    public string Name { get; set; }
    public bool IsInternal { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

public interface IBugDiscoverer : IEntity
{
    string Name { get; }
}

[Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
public class DeveloperEntity : Entity, IBugDiscoverer
{
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
public class CustomerEntity : Entity, IBugDiscoverer
{
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}
```

