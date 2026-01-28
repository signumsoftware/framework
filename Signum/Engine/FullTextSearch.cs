using Microsoft.SqlServer.Server;
using Signum.Engine.Linq;
using Signum.Engine.Maps;

namespace Signum.Engine;

public static class FullTextSearch
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/queries/contains-transact-sql?view=sql-server-ver16
    /// </summary>
    [AvoidEagerEvaluation]
    public static bool Contains(string[] columns, string searchCondition)
    {
        throw new InvalidOperationException("FullTextSearch.Contains is only supported inside a database query");
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/queries/contains-transact-sql?view=sql-server-ver16
    /// </summary>
    /// <param name="table">either an Entity or MListElement</param>
    [AvoidEagerEvaluation]
    public static bool Contains(object table, string searchCondition)
    {
        throw new InvalidOperationException("FullTextSearch.Contains is only supported inside a database query");
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/containstable-transact-sql?view=sql-server-ver16
    /// </summary>
    /// <param name="table">either an Entity or MListElement</param>
    [SqlMethod(Name = "CONTAINSTABLE"), AvoidEagerEvaluation]
    public static IQueryable<FullTextResultTable> ContainsTable(ITable table, IColumn[]? columns, string searchCondition, int? top_n_by_rank)
    {
        var mi = (MethodInfo)MethodInfo.GetCurrentMethod()!;
        return new Query<FullTextResultTable>(DbQueryProvider.Single, Expression.Call(mi,
            Expression.Constant(table, typeof(ITable)),
            Expression.Constant(columns, typeof(IColumn[])),
            Expression.Constant(searchCondition, typeof(string)),
            Expression.Constant(top_n_by_rank, typeof(int?))
        ));
    }

    public static IQueryable<FullTextResultTable> ContainsTable<T>(Expression<Func<T, object>>? fields, 
        string searchCondition, int? top_n_by_rank) 
        where T : Entity
    {
        var schema = Schema.Current;
        var table = schema.Table<T>();

        IColumn[]? columns = GetColumns(table, fields);
        return ContainsTable(table, columns, searchCondition, top_n_by_rank);
    }

    static IColumn[]? GetColumns(IFieldFinder table, LambdaExpression? fields)
    {
        return fields == null ? null : IndexKeyColumns.Split(table, fields).SelectMany(a => a.columns).ToArray();
    }

    public static IQueryable<FullTextResultTable> ContainsTable<T, V>(Expression<Func<T, MList<V>>> mlistProperty,
        Expression<Func<MListElement<T, V>, object>>? fields, 
        string searchCondition, int? top_n_by_rank)
        where T : Entity
    {
        var schema = Schema.Current;
        var table = schema.TableMList(mlistProperty);

        IColumn[]? columns = GetColumns(table, fields);
        return ContainsTable(table, columns, searchCondition, top_n_by_rank);
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/queries/freetext-transact-sql?view=sql-server-ver16
    /// </summary>
    [AvoidEagerEvaluation]
    public static bool FreeText(string[] columns, string freeTextString)
    {
        throw new InvalidOperationException("FullTextSearch.FreeText is only supported inside a database query");
    }

    /// <summary>
    ///https://learn.microsoft.com/en-us/sql/t-sql/queries/freetext-transact-sql?view=sql-server-ver16
    /// </summary>
    /// <param name="table">either an Entity or MListElement</param>
    [AvoidEagerEvaluation]
    public static bool FreeText(object table, string freeTextString)
    {
        throw new InvalidOperationException("FullTextSearch.FreeText is only supported inside a database query");
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/freetexttable-transact-sql?view=sql-server-ver16
    /// </summary>
    /// <param name="table">either an Entity or MListElement</param>
    [SqlMethod(Name = "FREETEXTTABLE"), AvoidEagerEvaluation]
    public static IQueryable<FullTextResultTable> FreeTextTable(ITable table, IColumn[]? columns, string freeTextString, int? top_n_by_rank = null)
    {
        var mi = (MethodInfo)MethodInfo.GetCurrentMethod()!;
        return new Query<FullTextResultTable>(DbQueryProvider.Single, Expression.Call(mi,
           Expression.Constant(table, typeof(ITable)),
           Expression.Constant(columns, typeof(IColumn[])),
           Expression.Constant(freeTextString, typeof(string)),
           Expression.Constant(top_n_by_rank, typeof(int?))
       ));
    }


    public static IQueryable<FullTextResultTable> FreeTextTable<T>(Expression<Func<T, string?[]>>? fields,
        string searchCondition, int? top_n_by_rank)
        where T : Entity
    {
        var schema = Schema.Current;
        var table = schema.Table<T>();

        IColumn[]? columns = GetColumns(table, fields);
        return FreeTextTable(table, columns, searchCondition, top_n_by_rank);
    }

    public static IQueryable<FullTextResultTable> FreeTextTable<T, V>(Expression<Func<T, MList<V>>> mlistProperty,
        Expression<Func<MListElement<T, V>, object>> fields,
        string searchCondition, int? top_n_by_rank)
        where T : Entity
    {
        var schema = Schema.Current;
        var table = schema.TableMList(mlistProperty);

        IColumn[]? columns = GetColumns(table, fields);
        return FreeTextTable(table, columns, searchCondition, top_n_by_rank);
    }
}

public class FullTextResultTable : IView
{
    [ColumnName("KEY")]
    public PrimaryKey Key;

    [ColumnName("RANK")]
    public int Rank;
}
