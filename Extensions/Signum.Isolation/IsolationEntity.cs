using System.ComponentModel;

namespace Signum.Isolation;

[EntityKind(EntityKind.String, EntityData.Master, IsLowPopulation = true)]
public class IsolationEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);


    public static IDisposable? Override(Lite<IsolationEntity>? isolation)
    {
        if (isolation == null)
            return null;

        var curr = IsolationEntity.Current;
        if (curr != null)
        {
            if (curr.Is(isolation))
                return null;

            throw new InvalidOperationException("Trying to change isolation from {0} to {1}".FormatWith(curr, isolation));
        }

        return UnsafeOverride(isolation);
    }

    //null: no override
    //Tuple<T>(null): override to null
    public static readonly AsyncThreadVariable<Tuple<Lite<IsolationEntity>?>> CurrentThreadVariable = Statics.ThreadVariable<Tuple<Lite<IsolationEntity>?>>("CurrentIsolation");
    public static IDisposable Disable()
    {
        return UnsafeOverride(null);
    }

    public static IDisposable UnsafeOverride(Lite<IsolationEntity>? isolation)
    {
        var old = CurrentThreadVariable.Value;

        CurrentThreadVariable.Value = Tuple.Create(isolation);

        return new Disposable(() => CurrentThreadVariable.Value = old);
    }

    public static Lite<IsolationEntity>? Current
    {
        get
        {
            var tuple = CurrentThreadVariable.Value;

            if (tuple != null)
                return tuple.Item1;

            return null;
        }
    }

    public static Lite<IsolationEntity>? CurrentUserIsolation
    {
        get
        {
            var userHolder = UserHolder.Current;
            if (userHolder == null)
                return null;

            return userHolder.GetClaim("Isolation") as Lite<IsolationEntity>;
        }
    }
}

[AutoInit]
public static class IsolationOperation
{
    public static ExecuteSymbol<IsolationEntity> Save;
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum IsolationStrategy
{
    Isolated,
    Optional,
    None,
}

public enum IsolationMessage
{
    [Description("Entity {0} has isolation {1} but current isolation is {2}")]
    Entity0HasIsolation1ButCurrentIsolationIs2,
    SelectAnIsolation,
    [Description("Entity '{0}' has isolation {1} but entity '{2}' has isolation {3}")]
    Entity0HasIsolation1ButEntity2HasIsolation3,
    GlobalMode,
    GlobalEntity,
}

public class IsolationMixin : MixinEntity
{
    IsolationMixin(ModifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next)
    {
    }

    [AttachToUniqueIndexes, ForceNotNullable]
    public Lite<IsolationEntity>? Isolation { get; set; } = IsRetrieving ? null : IsolationEntity.Current;

    protected override void CopyFrom(MixinEntity mixin, object[] args)
    {
        Isolation = ((IsolationMixin)mixin).Isolation;
    }
}

public static class IsolationExtensions
{
    [AutoExpressionField]
    public static Lite<IsolationEntity>? Isolation(this IEntity entity) => 
        As.Expression(() => ((Entity)entity).Mixin<IsolationMixin>().Isolation);

    public static Lite<IsolationEntity>? TryIsolation(this IEntity entity)
    {
        var mixin = ((Entity)entity).Mixins.OfType<IsolationMixin>().SingleOrDefaultEx();

        if (mixin == null)
            return null;

        return mixin.Isolation;
    }

    public static T SetIsolation<T>(this T entity, Lite<IsolationEntity> isolation)
        where T : IEntity
    {
        return entity.SetMixin((IsolationMixin m) => m.Isolation, isolation);
    }
    public static T SetIsolationCurrent<T>(this T entity)
    where T : IEntity
    {
        return entity.SetMixin((IsolationMixin m) => m.Isolation, IsolationEntity.Current);
    }
}
