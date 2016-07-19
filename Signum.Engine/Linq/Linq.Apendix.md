LINQ Apendix

Here are the entity classes used as the Data Model. Just usual Signum Entities. 

```C#
[Serializable]
public class BugEntity : Entity
{
    string description;
    public string Description
    {
        get { return description; }
        set { Set(ref description, value); }
    }

    DateTime start;
    public DateTime Start
    {
        get { return start; }
        set { if (Set(ref start, value)) CalculateHours(); }
    }

    DateTime? end;
    public DateTime? End
    {
        get { return end; }
        set { if (Set(ref end, value)) CalculateHours(); }
    }

    decimal? hours;
    public decimal? Hours
    {
        get { return hours; }
    }

    Status status;
    public Status Status
    {
        get { return status; }
        set { Set(ref status, value); }
    }

    IBugDiscoverer discoverer;
    [NotNullValidator]
    public IBugDiscoverer Discoverer
    {
        get { return discoverer; }
        set { Set(ref discoverer, value); }
    }

    DeveloperEntity fixer;
    public DeveloperEntity Fixer
    {
        get { return fixer; }
        set { Set(ref fixer, value); }
    }

    Lazy<ProjectEntity> project;
    [NotNullValidator]
    public Lazy<ProjectEntity> Project
    {
        get { return project; }
        set { Set(ref project, value); }
    }

    MList<CommentEntity> comments;
    public MList<CommentEntity> Comments
    {
        get { return comments; }
        set { Set(ref comments, value); }
    }

    protected override void PreSaving(ref bool graphModified)
    {
        CalculateHours();
    }

    private void CalculateHours()
    {
        hours = end.HasValue ? (decimal?)(end.Value - start).TotalHours : null;
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
    string text;
    public string Text
    {
        get { return text; }
        set { Set(ref text, value); }
    }

    DateTime date;
    public DateTime Date
    {
        get { return date; }
        set { Set(ref date, value); }
    }

    IBugDiscoverer writer;
    public IBugDiscoverer Writer
    {
        get { return writer; }
        set { Set(ref writer, value); }
    }

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


[Serializable]
public class ProjectEntity : Entity
{
    string name;
    public string Name
    {
        get { return name; }
        set { Set(ref name, value); }
    }

    bool isInternal;
    public bool IsInternal
    {
        get { return isInternal; }
        set { Set(ref isInternal, value); }
    }

    public override string ToString()
    {
        return name + (isInternal ? " [Internal]" : "");
    }
}


[ImplementedBy(typeof(CustomerEntity), typeof(DeveloperEntity))]
public interface IBugDiscoverer: IEntity
{
    public string Name { get; }
}


[Serializable]
public class DeveloperEntity : Entity, IBugDiscoverer
{
    string name;
    public string Name
    {
        get { return name; }
        set { Set(ref name, value); }
    }

    static Expression<Func<DeveloperEntity, string>> ToStringExpression = e => e.Name;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}


[Serializable]
public class CustomerEntity : Entity, IBugDiscoverer
{
    string name;
    public string Name
    {
        get { return name; }
        set { Set(ref name, value); }
    }

    static Expression<Func<CustomerEntity, string>> ToStringExpression = e => e.Name;
    [ExpressionField]
    public override string ToString()
    {
        return ToStringExpression.Evaluate(this);
    }
}
```

