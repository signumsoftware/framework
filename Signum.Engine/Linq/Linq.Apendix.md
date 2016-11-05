LINQ Apendix

Here are the entity classes used as the Data Model. Just usual Signum Entities. 

```C#
[Serializable, EntityKind(EntityKind.Master, EntityData.Transactional)]
public class BugEntity : Entity
{
    public string Description { get; set; }

    public DateTime Start { get; set; }

    public DateTime? End { get; set; }

    decimal? hours;
    public decimal? Hours
    {
        get { return hours; }
    }

    Status status;
    public Status Status { get; set; }

    [NotNullValidator, ImplementedBy(typeof(CustomerEntity), typeof(DeveloperEntity))]
    public IBugDiscoverer Discoverer { get; set; }

    public DeveloperEntity Fixer { get; set; }

    [NotNullValidator]
    public Lazy<ProjectEntity> Project { get; set; }

    public MList<CommentEntity> Comments { get; set; } = new MList<CommentEntity>();

    protected override void PreSaving(ref bool graphModified)
    {
        CalculateHours();
    }

    private void CalculateHours()
    {
        Hours = (End - Start)?.TotalHours;
        Notify(() => Hours);
    }

    static Expression<Func<BugEntity, string>> ToStringExpression = e => e.Description;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}


[Serializable]
public class CommentEntity : EmbeddedEntity
{
    public string Text { get; set; }

    public DateTime Date { get; set; }

	[ImplementedBy(typeof(CustomerEntity), typeof(DeveloperEntity))]
    public IBugDiscoverer Writer { get; set; }

    public override string ToString()
    {
        return "{0}: {1}".FormatWith(writer, text);
    }
}


public enum Status
{
    Open, 
    Fixed,
    Rejected, 
}


[Serializable, EntityKind(EntityKind.Master, EntityData.Master)]
public class ProjectEntity : Entity
{
    public string Name { get; set; }

    public bool IsInternal { get; set; }

    public override string ToString()
    {
        return name + (isInternal ? " [Internal]" : "");
    }
}


public interface IBugDiscoverer: IEntity
{
    public string Name { get; }
}


[Serializable, EntityKind(EntityKind.Master, EntityData.Master)]
public class DeveloperEntity : Entity, IBugDiscoverer
{
    public string Name { get; set; }

    static Expression<Func<DeveloperEntity, string>> ToStringExpression = e => e.Name;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}


[Serializable, EntityKind(EntityKind.Master, EntityData.Master)]
public class CustomerEntity : Entity, IBugDiscoverer
{
    public string Name { get; set; }

    static Expression<Func<CustomerEntity, string>> ToStringExpression = e => e.Name;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}
```

