using Microsoft.SqlServer.Server;
using Signum.Engine.Linq;
using Signum.Engine.Maps;

namespace Signum.Engine;

public static class SqlVectorSearch
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql
    /// Calculates the distance between two vectors using the specified distance metric
    /// </summary>
    [AvoidEagerEvaluation]
    public static float Vector_Distance(SqlVectorDistanceMetric distanceMetric, float[] vector1, float[] vector2)
    {
        throw new InvalidOperationException("VectorSearch.Vector_Distance is only supported inside a database query");
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql
    /// Calculates the norm (length) of a vector
    /// </summary>
    [AvoidEagerEvaluation]
    public static float Vector_Norm(float[] vector, SqlVectorNormType normType)
    {
        throw new InvalidOperationException("VectorSearch.Vector_Norm is only supported inside a database query");
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql
    /// Normalizes a vector to unit length
    /// </summary>
    [AvoidEagerEvaluation]
    public static float[] Vector_Normalize(float[] vector, SqlVectorNormType normType)
    {
        throw new InvalidOperationException("VectorSearch.Vector_Normalize is only supported inside a database query");
    }

    /// <summary>
    /// Performs a vector similarity search using the specified distance metric
    /// </summary>
    public static IQueryable<WithDistance<T>> Vector_Search<T>(Expression<Func<T, byte[]>> vectorField,
        float[] queryVector, SqlVectorDistanceMetric distanceMetric, int top_n)
        where T : Entity
    {
        var schema = Schema.Current;
        var table = schema.Table<T>();

        var columns = IndexKeyColumns.Split(table, vectorField).SelectMany(a => a.columns).ToArray();
        if (columns.Length != 1)
            throw new InvalidOperationException("vectorField must reference exactly one column");

        return Vector_Search<T>(table, columns.SingleEx(), queryVector, distanceMetric, top_n);
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/containstable-transact-sql?view=sql-server-ver16
    /// </summary>
    /// <param name="table">either an Entity or MListElement</param>
    [SqlMethod(Name = "VECTOR_SEARCH"), AvoidEagerEvaluation]
    public static IQueryable<WithDistance<T>> Vector_Search<T>(ITable table, IColumn columns, float[] queryVector, SqlVectorDistanceMetric distanceMetric, int top_n)
    {
        var mi = (MethodInfo)MethodInfo.GetCurrentMethod()!;
        return new Query<WithDistance<T>>(DbQueryProvider.Single, Expression.Call(mi,
            Expression.Constant(table, typeof(ITable)),
            Expression.Constant(columns, typeof(IColumn)),
            Expression.Constant(queryVector, typeof(float[])),
            Expression.Constant(distanceMetric, typeof(SqlVectorDistanceMetric)),
            Expression.Constant(top_n, typeof(int?))
        ));
    }

    public static string GetSqlVectorDistanceMetric(SqlVectorDistanceMetric metric) => metric switch
    {
        SqlVectorDistanceMetric.Cosine => "cosine",
        SqlVectorDistanceMetric.Euclidean => "euclidean",
        SqlVectorDistanceMetric.DotProduct => "dot",
        _ => throw new UnexpectedValueException(metric)
    };

    public static string GetSqlVectorNormType(SqlVectorNormType norm) => norm switch
    {
        SqlVectorNormType.Norm1 => "norm1",
        SqlVectorNormType.Norm2 => "norm2",
        SqlVectorNormType.Norminf => "norminf",
        _ => throw new UnexpectedValueException(norm)
    };
}

public enum SqlVectorDistanceMetric
{
    Cosine,
    Euclidean,
    DotProduct
}

public enum SqlVectorNormType
{
    /// <summary>
    /// The 1-norm, which is the sum of the absolute values of the vector components.
    /// </summary>
    Norm1,
    /// <summary>
    /// The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.
    /// </summary>
    Norm2,
    /// <summary>
    /// The infinity norm, which is the maximum of the absolute values of the vector components.
    /// </summary>
    Norminf
}

public class WithDistance<T> : IView
{
    public T Original { get; set; }

    public float Distance { get; set; }
}
