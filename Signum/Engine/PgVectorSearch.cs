using Pgvector;
using Signum.Engine.Linq;
using Signum.Engine.Maps;

namespace Signum.Engine;

public static class PgVectorSearch
{
    /// <summary>
    /// https://github.com/pgvector/pgvector?tab=readme-ov-file#querying
    /// Calculates the distance between two vectors using the specified distance metric
    /// </summary>
    [AvoidEagerEvaluation]
    public static float Distance(PGVectorDistanceMetric distanceMetric, Vector vector1, Vector vector2)
    {
        throw new InvalidOperationException("PgVectorSearch.Distance is only supported inside a database query");
    }

    /// <summary>
    /// Calculates the L2 (Euclidean) norm of a vector
    /// </summary>
    [AvoidEagerEvaluation]
    public static float L2_Norm(Vector vector)
    {
        throw new InvalidOperationException("PgVectorSearch.L2_Norm is only supported inside a database query");
    }

    /// <summary>
    /// Normalizes a vector to unit length (L2 normalization)
    /// </summary>
    [AvoidEagerEvaluation]
    public static Vector Normalize(Vector vector)
    {
        throw new InvalidOperationException("PgVectorSearch.Normalize is only supported inside a database query");
    }

    public static string GetPgVectorDistanceFunction(PGVectorDistanceMetric metric) => metric switch
    {
        PGVectorDistanceMetric.Cosine => "cosine_distance",
        PGVectorDistanceMetric.InnerProduct => "inner_product",
        PGVectorDistanceMetric.L1 => "l1_distance",
        PGVectorDistanceMetric.L2 => "l2_distance",
        _ => throw new UnexpectedValueException(metric)
    };
}

public enum PGVectorDistanceMetric
{
    Cosine,
    L2, // Euclidean
    InnerProduct,
    L1,
    Hamming,
    Jaccard
}
