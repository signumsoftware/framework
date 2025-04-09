using NpgsqlTypes;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Runtime.CompilerServices;

namespace Signum.Entities.TsVector;

public static class TsVectorExtensions
{
    static InvalidOperationException OnlyQueries([CallerMemberName] string method = "") => throw new InvalidOperationException($"Method {method} is only for queries");

    public static NpgsqlTsVector ToTsVector(this string value) => throw OnlyQueries();
    public static NpgsqlTsVector ToTsVector(this string value, string langConfig) => throw OnlyQueries();

    public static NpgsqlTsVector GetTsVectorColumn(this Entity entity) => entity.GetTsVectorColumn(PostgresTsVectorColumn.DefaultTsVectorColumn);
    public static NpgsqlTsVector GetTsVectorColumn(this Entity entity, string tsVectorColumnName) => throw OnlyQueries();

    public static NpgsqlTsVector GetTsVectorColumn<E, V>(this MListElement<E, V> mle) where E : Entity => mle.GetTsVectorColumn(PostgresTsVectorColumn.DefaultTsVectorColumn);
    public static NpgsqlTsVector GetTsVectorColumn<E, V>(this MListElement<E, V> mle, string tsVectorColumnName) where E : Entity => throw OnlyQueries();

    public static bool Matches(this NpgsqlTsVector vector, NpgsqlTsQuery query) => throw OnlyQueries();

    /// <summary>
    /// Examples:<br/>
    /// to_tsquery('hello')<br/>
    /// to_tsquery('hello &amp; !world')<br/>
    /// to_tsquery('(hello | hi) &amp; (world | earth)')<br/>
    /// to_tsquery('hello &lt;-&gt; world') (order matters)<br/>  
    /// to_tsquery('run:*') (prefix matters)<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery(this string value) => throw OnlyQueries();
    /// <summary>
    /// Examples:<br/>
    /// to_tsquery('hello')<br/>
    /// to_tsquery('hello &amp; !world')<br/>
    /// to_tsquery('(hello | hi) &amp; (world | earth)')<br/>
    /// to_tsquery('hello &lt;-&gt; world') (order matters)<br/>  
    /// to_tsquery('run:*') (prefix matters)<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery(this string value, string langConfig) => throw OnlyQueries();

    /// <summary>
    /// Examples:<br/>
    /// plainto_tsquery('hello world') (equivalent to hello &amp world)<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_Plain(this string value) => throw OnlyQueries();
    /// <summary>
    /// Examples:<br/>
    /// plainto_tsquery('hello world') (equivalent to hello &amp world)<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_Plain(this string value, string langConfig) => throw OnlyQueries();

    /// <summary>
    /// Examples:<br/>
    /// phraseto_tsquery('hello world') (equivalent to hello&lt;-&gt; world) <br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_Phrase(this string value) => throw OnlyQueries();
    /// <summary>
    /// Examples:<br/>
    /// phraseto_tsquery('hello world') (equivalent to hello&lt;-&gt; world) <br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    /// </summary>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_Phrase(this string value, string langConfig) => throw OnlyQueries();

    /// Examples:<br/>
    /// websearch_to_tsquery('hello world') → 'hello' &amp; 'world'<br/>
    /// websearch_to_tsquery('hello OR world') → 'hello' | 'world'<br/>
    /// websearch_to_tsquery('"hello world"') → 'hello' &lt;-&gt; 'world'<br/>
    /// websearch_to_tsquery('hello -world') → 'hello' &amp; !'world'<br/>
    /// websearch_to_tsquery('"quick fox" OR rabbit -lazy') → ('quick' &lt;-&gt; 'fox') | ('rabbit' &amp; !'lazy')<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_WebSearch(this string value) => throw OnlyQueries();

    /// Examples:<br/>
    /// websearch_to_tsquery('hello world') → 'hello' &amp; 'world'<br/>
    /// websearch_to_tsquery('hello OR world') → 'hello' | 'world'<br/>
    /// websearch_to_tsquery('"hello world"') → 'hello' &lt;-&gt; 'world'<br/>
    /// websearch_to_tsquery('hello -world') → 'hello' &amp; !'world'<br/>
    /// websearch_to_tsquery('"quick fox" OR rabbit -lazy') → ('quick' &lt;-&gt; 'fox') | ('rabbit' &amp; !'lazy')<br/>
    /// More info: <a href="https://www.postgresql.org/docs/current/textsearch-controls.html">PostgreSQL Full-Text Search</a>
    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery ToTsQuery_WebSearch(this string value, string langConfig) => throw OnlyQueries();


    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery And(this NpgsqlTsQuery a, NpgsqlTsQuery b) => throw OnlyQueries();

    [AvoidEagerEvaluation]
    public static NpgsqlTsQuery Or(this NpgsqlTsQuery a, NpgsqlTsQuery b) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this NpgsqlTsVector vector, NpgsqlTsQuery query) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> while normalizing
    ///     the result according to the behaviors specified by <paramref name="normalization" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this NpgsqlTsVector vector, NpgsqlTsQuery query, TsRankingNormalization normalization) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> with custom
    ///     weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this NpgsqlTsVector vector, NpgsqlTsQuery query, float[] weights) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> while normalizing
    ///     the result according to the behaviors specified by <paramref name="normalization" />
    ///     and using custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this NpgsqlTsVector vector, NpgsqlTsQuery query, TsRankingNormalization normalization, float[] weights) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this NpgsqlTsVector vector, NpgsqlTsQuery query) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method while normalizing the result according to the behaviors specified by
    ///     <paramref name="normalization" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this NpgsqlTsVector vector, NpgsqlTsQuery query, TsRankingNormalization normalization) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method with custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this NpgsqlTsVector vector, NpgsqlTsQuery query, float[] weights) => throw OnlyQueries();

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover density
    ///     method while normalizing the result according to the behaviors specified by <paramref name="normalization" />
    ///     and using custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this NpgsqlTsVector vector, NpgsqlTsQuery query, TsRankingNormalization normalization, float[] weights) => throw OnlyQueries();

    //public static float Headline(this NpgsqlTsQuery query, string document) => throw OnlyQueries();
    //public static float Headline(this NpgsqlTsQuery query, string document, string options) => throw OnlyQueries();
    //public static float Headline(this NpgsqlTsQuery query, string document, string options, string langConfig) => throw OnlyQueries();


    internal static MethodInfo miMatches = ReflectionTools.GetMethodInfo((NpgsqlTsVector v) => Matches(v, null!));
    internal static MethodInfo miRank = ReflectionTools.GetMethodInfo((NpgsqlTsVector v) => Rank(v, null!));

    static MethodInfo miToTsQuery = ReflectionTools.GetMethodInfo((string query) => ToTsQuery(query));
    static MethodInfo miToTsQuery_Plain = ReflectionTools.GetMethodInfo((string query) => ToTsQuery_Plain(query));
    static MethodInfo miToTsQuery_Phrase = ReflectionTools.GetMethodInfo((string query) => ToTsQuery_Phrase(query));
    static MethodInfo miToTsQuery_WebSearch = ReflectionTools.GetMethodInfo((string query) => ToTsQuery_WebSearch(query));

    internal static MethodInfo GetTsQueryMethodInfo(FilterOperation operation)
    {
        return operation switch
        {
            FilterOperation.TsQuery => miToTsQuery,
            FilterOperation.TsQuery_Plain => miToTsQuery_Plain,
            FilterOperation.TsQuery_Phrase => miToTsQuery_Phrase,
            FilterOperation.TsQuery_WebSearch => miToTsQuery_WebSearch,
            _ => throw new UnexpectedValueException(operation)
        };
    }

    static MethodInfo miAnd = ReflectionTools.GetMethodInfo(() => And(null!, null!));
    static MethodInfo miOr = ReflectionTools.GetMethodInfo(() => Or(null!, null!));

    internal static MethodInfo GetTsQueryGroupOperator(FilterGroupOperation operation)
    {
        return operation switch
        {
            FilterGroupOperation.And => miAnd,
            FilterGroupOperation.Or => miOr,
            _ => throw new UnexpectedValueException(operation)
        };
    }
}

public enum TsRankingNormalization
{
    /// <summary>
    ///     Ignores the document length.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     Divides the rank by 1 + the logarithm of the document length.
    /// </summary>
    DivideBy1PlusLogLength = 1,

    /// <summary>
    ///     Divides the rank by the document length.
    /// </summary>
    DivideByLength = 2,

    /// <summary>
    ///     Divides the rank by the mean harmonic distance between extents (this is implemented only by ts_rank_cd).
    /// </summary>
    DivideByMeanHarmonicDistanceBetweenExtents = 4,

    /// <summary>
    ///     Divides the rank by the number of unique words in document.
    /// </summary>
    DivideByUniqueWordCount = 8,

    /// <summary>
    ///     Divides the rank by 1 + the logarithm of the number of unique words in document.
    /// </summary>
    DividesBy1PlusLogUniqueWordCount = 16,

    /// <summary>
    ///     Divides the rank by itself + 1.
    /// </summary>
    DivideByItselfPlusOne = 32
}
