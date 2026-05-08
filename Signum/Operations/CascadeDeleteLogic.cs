using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Basics;

namespace Signum.Operations;

public class CascadeReferenceDto
{
    public string TypeName { get; set; } = null!;
    public string PropertyRoute { get; set; } = null!;
    public int Count { get; set; }
}

public static class CascadeDeleteLogic
{
    // All registered routes are excluded from the cascade-delete modal.
    // null  = excluded but no auto-delete.
    // non-null = excluded AND auto-delete handler (called at SchemaCompleted).
    // Applications can manipulate this dictionary before SchemaCompleted fires to suppress or override behaviour.
    public static Dictionary<(PropertyRoute route, Type refType), Action<Schema>> RegisteredCascades = new();

    static void RegisterRoute((PropertyRoute route, Type refType) p, Action<Schema> onSchemaCompleted)
    {
        RegisteredCascades[p] = onSchemaCompleted;

        if (handlersRegistered) //WhenIncluded
            onSchemaCompleted(Schema.Current);
    }

    static IEnumerable<Type> GetTypes(PropertyRoute route, Type? castType, bool hasOnDelete)
    {
        if (castType != null)
            return [castType];

        var implementations = route.GetImplementations();
        if (implementations.IsByAll)
            throw new InvalidOperationException($"Property {route} is {implementations}, you need to specify the forType parameter");
        if (hasOnDelete && implementations.Types.Only() != route.Type.CleanType())
            throw new InvalidOperationException($"Property {route} is {implementations}, if you use onDelete you need to cast to the specific type");

        return implementations.Types;
    }

    public static FluentInclude<T> WithCascadeDeleteBy<T, TTarget>(
        this FluentInclude<T> fi,
        Expression<Func<T, Lite<TTarget>?>> property,
        PreUnsafeDeleteHandler<TTarget>? onDelete = null)
        where T : Entity
        where TTarget : class, IEntity
    {
        var route = PropertyRoute.Construct(property, avoidLastCasting: true);
        var castType = GetCasting(property);
        foreach (var type in GetTypes(route, castType, onDelete != null))
        {
            Action<Schema>? onSchemaCompleted =  giGetHandler.GetInvoker(typeof(T), typeof(TTarget), type)(property, onDelete);
            RegisterRoute((route, type), onSchemaCompleted);
        }
        return fi;
    }

    public static FluentInclude<T> WithCascadeDeleteBy<T, TTarget>(
        this FluentInclude<T> fi,
        Expression<Func<T, TTarget?>> property,
        PreUnsafeDeleteHandler<TTarget>? onDelete = null)
        where T : Entity
        where TTarget : class, IEntity
    {
        var route = PropertyRoute.Construct(property, avoidLastCasting: true);
        var castType = GetCasting(property);
        var liteProperty = route.GetLambdaExpression<T, Lite<TTarget>?>(safeNullAccess: false, toLite: true);
        foreach (var type in GetTypes(route, castType, onDelete != null))
        {
            Action<Schema>? onSchemaCompleted = giGetHandler.GetInvoker(typeof(T), typeof(TTarget), type)(liteProperty, onDelete);
            RegisterRoute((route, type), onSchemaCompleted);
        }
        return fi;
    }

    public static Type? GetCasting(LambdaExpression lambda)
    {
        return lambda.Body.NodeType == ExpressionType.Convert ? lambda.Body.Type.CleanType() : null;
    }

    static readonly GenericInvoker<Func<LambdaExpression, Delegate?, Action<Schema>>> giGetHandler =
        new((prop, manualImp) => GetHandler<Entity, IEntity, Entity>((Expression<Func<Entity, Lite<IEntity>?>>)prop, (PreUnsafeDeleteHandler<Entity>?)manualImp));

    static Action<Schema> GetHandler<T, TTarget, TForType>(Expression<Func<T, Lite<TTarget>?>> property, PreUnsafeDeleteHandler<TForType>? onDelete)
        where T : Entity
        where TTarget : class, IEntity
        where TForType : Entity, TTarget
    {
        if (onDelete != null)
        {
            return schema =>
            {
                schema.EntityEvents<TForType>().PreUnsafeDelete += onDelete;
            };
        }
        else
        {
            return schema =>
            {
                schema.EntityEvents<TForType>().PreUnsafeDelete += targetQuery =>
                {
                    Database.Query<T>()
                        .Where(source => targetQuery.Any(target => property.Evaluate(source)!.Is(target)))
                        .UnsafeDelete();
                    return null;
                };
            };
        }
    }

    public static FluentInclude<T> WithCascadeDeleteMListBy<T, TElement, TTarget>(
        this FluentInclude<T> fi,
        Expression<Func<T, MList<TElement>>> mlist,
        Expression<Func<TElement, Lite<TTarget>?>> property,
        PreUnsafeDeleteHandler<TTarget>? onDelete = null)
        where T : Entity
        where TTarget : class, IEntity
    {
        var castType = GetCasting(property);
        var elementRoute = PropertyRoute.Construct(mlist).Add("Item").Continue(property, avoidLastCasting: true);
        foreach (var type in GetTypes(elementRoute, castType, onDelete != null))
        {
            Action<Schema> onSchemaCompleted = giGetMListHandler.GetInvoker(typeof(T), typeof(TElement), typeof(TTarget), type)(mlist, property, onDelete);
            RegisterRoute((elementRoute, type), onSchemaCompleted);
        }
        return fi;
    }

    public static FluentInclude<T> WithCascadeDeleteMListBy<T, TElement, TTarget>(
        this FluentInclude<T> fi,
        Expression<Func<T, MList<TElement>>> mlist,
        Expression<Func<TElement, TTarget?>> property,
        PreUnsafeDeleteHandler<TTarget>? onDelete = null)
        where T : Entity
        where TTarget : class, IEntity
    {
        var castType = GetCasting(property);
        var mlistItemsRoute = PropertyRoute.Construct(mlist).Add("Item");
        var elementRoute = mlistItemsRoute.Continue(property, avoidLastCasting: true);
        var liteProperty = elementRoute.GetLambdaExpression<TElement, Lite<TTarget>?>(safeNullAccess: false, skipBefore: mlistItemsRoute, toLite: true);
        foreach (var type in GetTypes(elementRoute, castType, onDelete != null))
        {
            Action<Schema> onSchemaCompleted = giGetMListHandler.GetInvoker(typeof(T), typeof(TElement), typeof(TTarget), type)(mlist, liteProperty, onDelete);
            RegisterRoute((elementRoute, type), onSchemaCompleted);
        }
        return fi;
    }

    static readonly GenericInvoker<Func<LambdaExpression, LambdaExpression, Delegate?, Action<Schema>>> giGetMListHandler =
        new((mlist, prop, manualImp) => GetMListHandler<Entity, Entity, IEntity, Entity>((Expression<Func<Entity, MList<Entity>>>)mlist, (Expression<Func<Entity, Lite<IEntity>?>>)prop, (PreUnsafeDeleteHandler<Entity>?)manualImp));

    static Action<Schema> GetMListHandler<T, TElement, TTarget, TForType>(
        Expression<Func<T, MList<TElement>>> mlist,
        Expression<Func<TElement, Lite<TTarget>?>> property,
        PreUnsafeDeleteHandler<TForType>? onDelete)
        where T : Entity
        where TTarget : class, IEntity
        where TForType : Entity, TTarget
    {
        if (onDelete != null)
        {
            return schema =>
            {
                schema.EntityEvents<TForType>().PreUnsafeDelete += onDelete;
            };
        }
        else
        {
            return schema =>
            {
                schema.EntityEvents<TForType>().PreUnsafeDelete += targetQuery =>
                {
                    Database.MListQuery(mlist)
                        .Where(mle => targetQuery.Any(target => property.Evaluate(mle.Element)!.Is(target)))
                        .UnsafeDeleteMList();
                    return null;
                };
            };
        }
    }

    // Called once at SchemaCompleted. Iterates RegisteredCascades and invokes every non-null handler.
    // Applications can add, remove, or null-out entries before schema completes to customise behaviour.
    static bool handlersRegistered; 

    public static void RegisterCascadeDeleteHandlers()
    {
        var schema = Schema.Current;
        foreach (var handler in RegisteredCascades.Values)
            handler?.Invoke(schema);

        handlersRegistered = true;
    }

    public static List<CascadeReferenceDto> GetReferences(Lite<Entity> target)
    {
        var targetType = target.EntityType;

        var excludedKeys = RegisteredCascades.Keys.ToHashSet();

        var virtualMListBackRefs = VirtualMList.RegisteredVirtualMLists
            .TryGetC(targetType)
            ?.Values
            .Select(vmi => vmi.BackReferenceRoute)
            .ToHashSet()
            ?? [];

        var result = new List<CascadeReferenceDto>();

        foreach (var table in Schema.Current.Tables.Values)
        {
            if (table.Type == targetType)
                continue;

            foreach (var (refTable, info) in table.DependentTables())
            {
                if (refTable.Type != targetType)
                    continue;
                if (info.IsImplementedByAll)
                    continue;
                if (virtualMListBackRefs.Contains(info.PropertyRoute))
                    continue;
                if (excludedKeys.Contains((info.PropertyRoute, refTable.Type)))
                    continue;

                var count = InvokeCount(table.Type, targetType, info, target.Id);

                if (count > 0)
                    result.Add(new CascadeReferenceDto
                    {
                        TypeName = TypeLogic.GetCleanName(table.Type),
                        PropertyRoute = info.PropertyRoute.ToString(),
                        Count = count,
                    });
            }
        }

        return result;
    }

    static int InvokeCount(Type rootType, Type targetType, RelationInfo info, PrimaryKey targetId)
    {
        // innerType: the lambda's body type — the Lite type argument for lite fields,
        // or the declared field type for entity fields. For direct FK this equals targetType;
        // for ImplementedBy it's the declared base/interface and != targetType.
        var innerType = info.PropertyRoute.Type.CleanType();

        if (!info.IsCollection)
            return giGetCount.GetInvoker(rootType, innerType, targetType)(info.PropertyRoute, targetId);

        // MList: count root entities that have any matching element (EXISTS / Any).
        var mlistItemsRoute = info.PropertyRoute.GetMListItemsRoute()!;
        return giGetMListCount.GetInvoker(rootType, mlistItemsRoute.Type, innerType, targetType)(info.PropertyRoute, mlistItemsRoute, targetId);
    }

    // Handles direct Lite FK, ImplementedBy, and entity FK (entity converted to Lite via ToLite()).
    // For entity FK: TInterface == TRef == targetType; the lambda wraps the entity body in ToLite().
    // For ImplementedBy Lite: TInterface is the declared interface; Lite<out T> covariance allows the cast.
    static readonly GenericInvoker<Func<PropertyRoute, PrimaryKey, int>> giGetCount =
        new((route, id) => GetCount<Entity, IEntity, Entity>(route, id));

    static int GetCount<TRoot, TInterface, TRef>(PropertyRoute route, PrimaryKey targetId)
        where TRoot : Entity
        where TInterface : class, IEntity
        where TRef : Entity, TInterface
    {
        var lambda = route.GetLambdaExpression<TRoot, Lite<TInterface>?>(safeNullAccess: false, toLite: !route.Type.IsLite());
        var targetLite = (Lite<TInterface>)Lite.Create<TRef>(targetId);
        return Database.Query<TRoot>()
            .Where(e => lambda.Evaluate(e).Is(targetLite))
            .Count();
    }

    // Counts root entities that have any MList element referencing the target.
    static readonly GenericInvoker<Func<PropertyRoute, PropertyRoute, PrimaryKey, int>> giGetMListCount =
        new((route, mlistItemsRoute, id) => GetMListCount<Entity, Entity, IEntity, Entity>(route, mlistItemsRoute, id));

    static int GetMListCount<TRoot, TElement, TInterface, TRef>(PropertyRoute route, PropertyRoute mlistItemsRoute, PrimaryKey targetId)
        where TRoot : Entity
        where TInterface : class, IEntity
        where TRef : Entity, TInterface
    {
        var mlistLambda = mlistItemsRoute.Parent!.GetLambdaExpression<TRoot, MList<TElement>?>(safeNullAccess: false);

        var elementLambda = (Expression<Func<TElement, Lite<TInterface>?>>)route.GetLambdaExpression(
            typeof(TElement), typeof(Lite<TInterface>), safeNullAccess: false, skipBefore: mlistItemsRoute, toLite: !route.Type.IsLite());

        var targetLite = (Lite<TInterface>)Lite.Create<TRef>(targetId);
        return Database.Query<TRoot>()
            .Where(e => mlistLambda.Evaluate(e)!.Any(item => elementLambda.Evaluate(item).Is(targetLite)))
            .Count();
    }
}
