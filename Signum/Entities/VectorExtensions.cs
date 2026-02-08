using Pgvector;
using Signum.Utilities.Reflection;
using System.Runtime.CompilerServices;

namespace Signum.Entities.VectorSearch;

public static class VectorExtensions
{
    static InvalidOperationException OnlyQueries([CallerMemberName] string method = "") => throw new InvalidOperationException($"Method {method} is only for queries");

    public static Pgvector.Vector GetVectorColumn(this Entity entity, string vectorColumnName) => throw OnlyQueries();

    public static Pgvector.Vector GetVectorColumn<E, V>(this MListElement<E, V> mle, string vectorColumnName) where E : Entity => throw OnlyQueries();
}
