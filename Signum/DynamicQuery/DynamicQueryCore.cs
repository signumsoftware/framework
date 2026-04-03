using Signum.Engine.Linq;
using Signum.Utilities.Reflection;

namespace Signum.DynamicQuery;

public interface IDynamicQueryCore
{
    object QueryName { get; set; }
    ColumnDescriptionFactory[] StaticColumns { get; }
    Expression? Expression { get; }

    ColumnDescriptionFactory EntityColumnFactory();
    QueryDescription GetQueryDescription();

    ResultTable ExecuteQuery(QueryRequest request);
    Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken);
    ResultTable ExecuteQueryGroup(QueryRequest request);
    Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken cancellationToken);
    object? ExecuteQueryValue(QueryValueRequest request);
    Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken cancellationToken);
    Lite<Entity>? ExecuteUniqueEntity(UniqueEntityRequest request);
    Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken cancellationToken);

    IQueryable<Lite<Entity>> GetEntitiesLite(QueryEntitiesRequest request);
    IQueryable<Entity> GetEntitiesFull(QueryEntitiesRequest request);
}


public static class DynamicQueryCore
{
    public static AutoDynamicQueryCore<T> Auto<T>(IQueryable<T> query)
    {
        return new AutoDynamicQueryCore<T>(query);
    }

    public static ManualDynamicQueryCore<T> Manual<T>(Func<QueryRequest, QueryDescription, CancellationToken, Task<DEnumerableCount<T>>> execute)
    {
        return new ManualDynamicQueryCore<T>(execute);
    }

    internal static IDynamicQueryCore FromSelectorUntyped<T>(Expression<Func<T, object?>> expression)
        where T : Entity
    {
        var eType = expression.Parameters.SingleEx().Type;
        var tType = expression.Body.Type;
        var typedSelector = Expression.Lambda(expression.Body, expression.Parameters);

        return giAutoPrivate.GetInvoker(eType, tType)(typedSelector);
    }

    static readonly GenericInvoker<Func<LambdaExpression, IDynamicQueryCore>> giAutoPrivate =
        new(lambda => FromSelector<TypeEntity, object?>((Expression<Func<TypeEntity, object?>>)lambda));
    public static AutoDynamicQueryCore<T> FromSelector<E, T>(Expression<Func<E, T>> selector)
        where E : Entity
    {
        return new AutoDynamicQueryCore<T>(Database.Query<E>().Select(selector));
    }

    public static Dictionary<string, Meta?>? QueryMetadata(IQueryable query)
    {
        return MetadataVisitor.GatherMetadata(query.Expression);
    }

}

public abstract class DynamicQueryCore<T> : IDynamicQueryCore
{
    public object QueryName { get; set; } = null!;

    public ColumnDescriptionFactory[] StaticColumns { get; protected set; } = null!;

    public abstract ResultTable ExecuteQuery(QueryRequest request);
    public abstract Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken);

    public abstract ResultTable ExecuteQueryGroup(QueryRequest request);
    public abstract Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken cancellationToken);

    public abstract object? ExecuteQueryValue(QueryValueRequest request);
    public abstract Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken cancellationToken);

    public abstract Lite<Entity>? ExecuteUniqueEntity(UniqueEntityRequest request);
    public abstract Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken cancellationToken);

    public abstract IQueryable<Lite<Entity>> GetEntitiesLite(QueryEntitiesRequest request);
    public abstract IQueryable<Entity> GetEntitiesFull(QueryEntitiesRequest request);


    protected virtual ColumnDescriptionFactory[] InitializeColumns()
    {
        var result = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
          .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, null)).ToArray();

        return result;
    }

    public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Enum messageValue)
    {
        return this.Column(column, c => c.OverrideDisplayName = () => messageValue.NiceToString());
    }

    public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Func<string> messageValue)
    {
        return this.Column(column, c => c.OverrideDisplayName = messageValue);
    }

    public DynamicQueryCore<T> ColumnProperyRoutes<S>(Expression<Func<T, S>> column, params PropertyRoute[] routes)
    {
        return this.Column(column, c => c.PropertyRoutes = routes);
    }

    public DynamicQueryCore<T> Column<S>(Expression<Func<T, S>> column, Action<ColumnDescriptionFactory> change)
    {
        MemberInfo member = ReflectionTools.GetMemberInfo(column);
        ColumnDescriptionFactory col = StaticColumns.SingleEx(a => a.Name == member.Name);
        change(col);

        return this;
    }

    public ColumnDescriptionFactory EntityColumnFactory()
    {
        return StaticColumns.Where(c => c.IsEntity).SingleEx(() => "Entity column on {0}".FormatWith(QueryUtils.GetKey(QueryName)));
    }

    public virtual Expression? Expression
    {
        get { return null; }
    }

    public QueryDescription GetQueryDescription()
    {
        var entity = EntityColumnFactory();
        string? allowed = entity.IsAllowed();
        if (allowed != null)
            throw new InvalidOperationException(
                "Not authorized to see Entity column on {0} because {1}".FormatWith(QueryUtils.GetKey(QueryName), allowed));

        var columns = StaticColumns.Where(f => f.IsAllowed() == null).Select(f => f.BuildColumnDescription()).ToList();

        return new QueryDescription(QueryName, columns);
    }
}

