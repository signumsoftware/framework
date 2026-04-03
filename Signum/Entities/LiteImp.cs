namespace Signum.Entities.Internal;

public abstract class LiteImp : Modifiable
{
    public abstract void SetId(PrimaryKey id);
}

public sealed class LiteImp<T, M> : LiteImp, Lite<T>
   where T : Entity
{
    T? entityOrNull;
    PrimaryKey? id;
    int? partitionId;
    M? model;

    // Methods
    private LiteImp()
    {
    }

    public LiteImp(PrimaryKey id, M? model, int? partitionId)
    {
        if (typeof(T).IsAbstract)
            throw new InvalidOperationException(typeof(T).Name + " is abstract");

        if (PrimaryKey.Type(typeof(T)) != id.Object.GetType())
            throw new InvalidOperationException($"{typeof(T).TypeName()} requires ids of type {PrimaryKey.Type(typeof(T)).TypeName()}, not {id.Object.GetType().TypeName()}");

        this.partitionId = partitionId;
        this.id = id;
        this.model = model;
        this.Modified = ModifiedState.Clean;
    }

    public LiteImp(T entity, M? model)
    {
        if (typeof(T).IsAbstract)
            throw new InvalidOperationException(typeof(T).Name + " is abstract");

        if (entity.GetType() != typeof(T))
            throw new ArgumentNullException(nameof(entity));

        this.entityOrNull = entity;
        this.id = entity.IdOrNull;
        this.partitionId = entity.PartitionId;
        this.model = model;
        this.Modified = entity.Modified;
    }

    public Entity? UntypedEntityOrNull
    {
        get { return (Entity?)(object?)entityOrNull; }
    }

    public T? EntityOrNull
    {
        get { return entityOrNull; }
    }

    public bool IsNew
    {
        get { return entityOrNull != null && entityOrNull.IsNew; }
    }

    public T Entity
    {
        get
        {
            if (entityOrNull == null)
                throw new InvalidOperationException("The lite {0} is not loaded, use Database.Retrieve or consider rewriting your query".FormatWith(this));
            return entityOrNull;
        }
    }

    public Type EntityType
    {
        get { return typeof(T); }
    }

    public Type ModelType
    {
        get { return typeof(M); }
    }

    public PrimaryKey Id
    {
        get
        {
            if (id == null)
                throw new InvalidOperationException("The Lite is pointing to a new entity and has no Id yet");
            return id.Value;
        }
    }

    public PrimaryKey? IdOrNull
    {
        get { return id; }
    }

    public object? Model => this.model;

    public int? PartitionId => this.partitionId;

    public void SetEntity(Entity ei)
    {
        if (id == null)
            throw new InvalidOperationException("New entities are not allowed");

        if (id != ei.id || EntityType != ei.GetType())
            throw new InvalidOperationException("Entities do not match");

        this.entityOrNull = (T)ei;
        if (ei != null && this.model == null)
            this.model = Lite.ConstructModel<T, M>(this.entityOrNull);
    }

    public void ClearEntity()
    {
        if (this.entityOrNull != null)
            RefreshId();

        if (id == null)
            throw new InvalidOperationException("Removing entity not allowed in new Lite");

        this.model = this.entityOrNull == null ? (M)(object)null! : Lite.ConstructModel<T, M>(this.entityOrNull!);
        this.entityOrNull = null;
    }

    public PrimaryKey RefreshId()
    {
        var newId = entityOrNull!.Id;
        id = newId;
        partitionId = entityOrNull!.partitionId;
        return newId;
    }

    protected internal override void PreSaving(PreSavingContext ctx)
    {
        if (entityOrNull != null)
        {
            entityOrNull.PreSaving(ctx);
        }
    }

    public override string? ToString()
    {
        if (this.entityOrNull != null)
            return this.entityOrNull.ToString();

        var result = this.model?.ToString();

        return result ?? (this.EntityType.NiceName() + " " + Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        if (this == obj)
            return true;

        if (!(obj is Lite<T> lite))
            return false;

        if (lite.EntityType != this.EntityType)
            return false;

        if (IdOrNull != null && lite.IdOrNull != null)
            return Id == lite.Id;
        else
            return object.ReferenceEquals(this.entityOrNull, lite.EntityOrNull);
    }

    const int MagicMask = 123456853;
    public override int GetHashCode()
    {
        return this.id == null ? entityOrNull!.GetHashCode() ^ MagicMask :
            this.EntityType.FullName!.GetHashCode() ^ this.Id.GetHashCode() ^ MagicMask;
    }

    public string Key()
    {
        return "{0};{1}".FormatWith(TypeLogic.GetCleanName(this.EntityType), this.Id);
    }

    public string KeyLong()
    {
        return "{0};{1};{2}".FormatWith(TypeLogic.GetCleanName(this.EntityType), this.Id, this.ToString());
    }

    public int CompareTo(Lite<Entity>? other)
    {
        return ToString()!.CompareTo(other?.ToString());
    }

    public int CompareTo(object? obj)
    {
        if (obj is Lite<Entity> lite)
            return CompareTo(lite);

        throw new InvalidOperationException("obj is not a Lite");
    }

    public void SetModel(object? model)
    {
        this.model = (M?)model;
    }

    public override void SetId(PrimaryKey id)
    {
        if (PrimaryKey.Type(typeof(T)) != id.Object.GetType())
            throw new InvalidOperationException($"{typeof(T).TypeName()} requires ids of type {PrimaryKey.Type(typeof(T)).TypeName()}, not {id.Object.GetType().TypeName()}");

        this.id = id;
    }

    public Lite<T> Clone()
    {
        return new LiteImp<T, M>(Id, model, this.PartitionId);
    }

    public M1 GetModel<M1>() where M1 : ModelEntity
    {
        return (M1)(object)this.model!;
    }
}
