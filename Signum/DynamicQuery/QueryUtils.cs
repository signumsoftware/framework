using NpgsqlTypes;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Signum.DynamicQuery;

public static class QueryUtils
{
    public static string GetKey(object queryName)
    {
        return queryName is Type t ? Reflector.CleanTypeName(EnumEntity.Extract(t) ?? t) : 
            queryName.ToString()!;
    }
    
    public static string GetNiceName(object queryName)
    {
        return
            queryName is Type t ? (EnumEntity.Extract(t) ?? t).NicePluralName() :
            queryName is Enum e ? e.NiceToString() :
            queryName.ToString()!;
    }

    public static FilterType GetFilterType(Type type)
    {
        FilterType? filterType = TryGetFilterType(type);

        if(filterType == null)
            throw new NotSupportedException("Type {0} not supported".FormatWith(type));

        return filterType.Value;
    }

    public static FilterType? TryGetFilterType(Type type)
    {
        var uType = type.UnNullify();

        if (uType == typeof(Guid))
            return FilterType.Guid;

        if (uType == typeof(DateOnly) || uType == typeof(DateTimeOffset))
            return FilterType.DateTime;

        if (uType == typeof(TimeSpan) || uType == typeof(TimeOnly))
            return FilterType.Time;

        if (uType == typeof(NpgsqlTsVector))
            return FilterType.TsVector;

        if (uType.IsEnum)
            return FilterType.Enum;

        switch (Type.GetTypeCode(uType))
        {
            case TypeCode.Boolean:
                return FilterType.Boolean;
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.Single:
                return FilterType.Decimal;
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return FilterType.Integer;
            case TypeCode.DateTime:
                return FilterType.DateTime;

            case TypeCode.Char:
            case TypeCode.String:
                return FilterType.String;
            case TypeCode.Object:
                if (type.IsLite())
                    return FilterType.Lite;

                if (type.IsIEntity())
                    return FilterType.Lite;

                if (type.IsEmbeddedEntity())
                    return FilterType.Embedded;

                if (type.IsModelEntity())
                    return FilterType.Model;

                goto default;
            default:
                return null;

        }
    }

    public static IList<FilterOperation> GetFilterOperations(QueryToken token)
    {
        var filtertype = GetFilterType(token.Type);

        var result = FilterOperations[filtertype];

        if (token is EntityPropertyToken ept && EntityPropertyToken.HasFullTextIndex(ept.PropertyRoute))
        {
            return result.PreAnd(FilterOperation.FreeText).PreAnd(FilterOperation.ComplexCondition).ToList();
        }

        return result;
    }

    static readonly Dictionary<FilterType, ReadOnlyCollection<FilterOperation>> FilterOperations = new Dictionary<FilterType, ReadOnlyCollection<FilterOperation>>
    {
        {
            FilterType.String, new List<FilterOperation>
            {
                FilterOperation.Contains,
                FilterOperation.EqualTo,
                FilterOperation.StartsWith,
                FilterOperation.EndsWith,
                FilterOperation.Like,
                FilterOperation.NotContains,
                FilterOperation.DistinctTo,
                FilterOperation.NotStartsWith,
                FilterOperation.NotEndsWith,
                FilterOperation.NotLike,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn
            }.ToReadOnly()
        },
        {
            FilterType.DateTime, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.GreaterThan,
                FilterOperation.GreaterThanOrEqual,
                FilterOperation.LessThan,
                FilterOperation.LessThanOrEqual,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Time, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.GreaterThan,
                FilterOperation.GreaterThanOrEqual,
                FilterOperation.LessThan,
                FilterOperation.LessThanOrEqual,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Integer, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.GreaterThan,
                FilterOperation.GreaterThanOrEqual,
                FilterOperation.LessThan,
                FilterOperation.LessThanOrEqual,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Decimal, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.GreaterThan,
                FilterOperation.GreaterThanOrEqual,
                FilterOperation.LessThan,
                FilterOperation.LessThanOrEqual,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Enum, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
                FilterOperation.GreaterThan,
                FilterOperation.GreaterThanOrEqual,
                FilterOperation.LessThan,
                FilterOperation.LessThanOrEqual,
            }.ToReadOnly()
        },
        {
            FilterType.Guid, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Lite, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
                FilterOperation.IsIn,
                FilterOperation.IsNotIn,
            }.ToReadOnly()
        },
        {
            FilterType.Embedded, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
            }.ToReadOnly()
        },
        {
            FilterType.Model, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
            }.ToReadOnly()
        },
        {
            FilterType.Boolean, new List<FilterOperation>
            {
                FilterOperation.EqualTo,
                FilterOperation.DistinctTo,
            }.ToReadOnly()
        },
        {
            FilterType.TsVector, new List<FilterOperation>
            {
                FilterOperation.TsQuery,
                FilterOperation.TsQuery_Plain,
                FilterOperation.TsQuery_Plain,
                FilterOperation.TsQuery_WebSearch,
            }.ToReadOnly()
        },
    };


    public static QueryToken? SubToken(QueryToken? token, QueryDescription qd, SubTokensOptions options, string key)
    {
        var result = SubTokenBasic(token, qd, options, key);

        if (result != null)
            return result;

        if ((options & SubTokensOptions.CanAggregate) != 0)
        {
            var agg = AggregateTokens(token, qd).SingleOrDefaultEx(a => a.Key == key);
            if (agg != null)
                return agg;
        }

        if (options.HasFlag(SubTokensOptions.CanTimeSeries) && key == TimeSeriesToken.KeyText && token == null)
            return new TimeSeriesToken(qd.QueryName);

        return null;
    }

    public static List<QueryToken> SubTokens(this QueryToken? token, QueryDescription qd, SubTokensOptions options)
    {
        var result = SubTokensBasic(token, qd, options);

        if (options.HasFlag(SubTokensOptions.CanAggregate))
            result.InsertRange(0, AggregateTokens(token, qd));

        if (options.HasFlag(SubTokensOptions.CanTimeSeries) && token == null)
            result.Insert(0, new TimeSeriesToken(qd.QueryName));

        return result;
    }


    private static IEnumerable<QueryToken> AggregateTokens(QueryToken? token, QueryDescription qd)
    {
        if (token == null)
        {
            yield return new AggregateToken(AggregateFunction.Count, qd.QueryName);
        }
        else if (!(token is AggregateToken))
        {
            FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

            if (ft == FilterType.Integer || ft == FilterType.Decimal || ft == FilterType.Boolean)
            {
                yield return new AggregateToken(AggregateFunction.Average, token);
                yield return new AggregateToken(AggregateFunction.Sum, token);

                yield return new AggregateToken(AggregateFunction.Min, token);
                yield return new AggregateToken(AggregateFunction.Max, token);
            }
            else if (ft == FilterType.DateTime || ft == FilterType.Time) /*ft == FilterType.String || */
            {
                yield return new AggregateToken(AggregateFunction.Min, token);
                yield return new AggregateToken(AggregateFunction.Max, token);
            }

            if(ft != null)
            {
                yield return new AggregateToken(AggregateFunction.Count, token, FilterOperation.DistinctTo, null);
                yield return new AggregateToken(AggregateFunction.Count, token,  FilterOperation.EqualTo, null);
            }

            if (token.IsGroupable)
            {
                yield return new AggregateToken(AggregateFunction.Count, token, distinct: true);

            }

            if (ft == FilterType.Enum)
            {
                foreach (var v in Enum.GetValues(token.Type.UnNullify()))
                {
                    yield return new AggregateToken(AggregateFunction.Count, token, FilterOperation.EqualTo, v);
                    yield return new AggregateToken(AggregateFunction.Count, token, FilterOperation.DistinctTo, v);
                }
            }

            if (ft == FilterType.Boolean)
            {
                yield return new AggregateToken(AggregateFunction.Count, token, FilterOperation.EqualTo, true);
                yield return new AggregateToken(AggregateFunction.Count, token, FilterOperation.EqualTo, false);
            }
        }
    }

    static QueryToken? SubTokenBasic(QueryToken? token, QueryDescription qd, SubTokensOptions options, string key)
    {
        if (token == null)
        {
            var column = qd.Columns.SingleOrDefaultEx(a=>a.Name == key);

            if (column == null)
                return null;

            return new ColumnToken(column, qd.QueryName);
        }
        else
        {
            return token.SubTokenInternal(key, options);
        }
    }

    static List<QueryToken> SubTokensBasic(QueryToken? token, QueryDescription qd, SubTokensOptions options)
    {
        if (token == null)
            return qd.Columns.Select(cd => (QueryToken)new ColumnToken(cd, qd.QueryName)).ToList();
        else
            return token.SubTokensInternal(options);
    }

    public static readonly Regex SplitRegex = new Regex(@"(?<!\[[^\]]*)\.(?![^\[]*\])");

    public static QueryToken Parse(string tokenString, QueryDescription qd, SubTokensOptions options)
    {
        if (string.IsNullOrEmpty(tokenString))
            throw new ArgumentNullException(nameof(tokenString));

        //Dot not inside of brackets
        string[] parts = SplitRegex.Split(tokenString);

        string firstPart = parts.FirstEx();

        QueryToken? result = SubToken(null, qd, options, firstPart);

        if (result == null)
            throw new FormatException("Column '{0}' not found on query {1}".FormatWith(firstPart, QueryUtils.GetKey(qd.QueryName)));

        foreach (var part in parts.Skip(1))
        {
            var newResult = SubToken(result, qd, options, part);
            result = newResult ?? throw new FormatException("Token with key '{0}' not found on token '{1}' of query {2}".FormatWith(part, result.FullKey(), QueryUtils.GetKey(qd.QueryName)));
        }

        return result;
    }

    public static QueryToken? TryParse(string tokenString, QueryDescription qd, SubTokensOptions options)
    {
        if (string.IsNullOrEmpty(tokenString))
            return null;

        //https://stackoverflow.com/questions/35418597/split-string-on-the-dot-characters-that-are-not-inside-of-brackets
        string[] parts = SplitRegex.Split(tokenString);

        string firstPart = parts.FirstEx();

        QueryToken? result = SubToken(null, qd, options, firstPart);

        if (result == null)
            return null;

        foreach (var part in parts.Skip(1))
        {
            var newResult = SubToken(result, qd, options, part);
            if (newResult == null)
                return null;
            result = newResult;
        }

        return result;
    }

    public static string? CanFilter(QueryToken token)
    {
        if (token == null)
            return "No column selected";

        if (token.Type != typeof(string) && token.Type != typeof(NpgsqlTsVector) && token.Type.ElementType() != null)
            return "You can not filter by collections, continue the sequence";

        if (token is OperationsContainerToken or OperationToken or ManualContainerToken or ManualToken or IndexerContainerToken)
            return $"{token} is not a valid filter";

        return null;
    }

    public static string? CanColumn(QueryToken token)
    {
        if (token == null)
            return "No column selected";

        if (QueryToken.IsCollection(token.Type))
            return "You can not add collections as columns";

        if (token.HasAllOrAny())
            return "Columns can not contain '{0}', '{1}', {2} or {3}".FormatWith(
                CollectionAnyAllType.All.NiceToString(),
                CollectionAnyAllType.Any.NiceToString(),
                CollectionAnyAllType.NotAny.NiceToString(),
                CollectionAnyAllType.NotAll.NiceToString());

        if (token is OperationsContainerToken or ManualContainerToken or PgTsVectorColumnToken or IndexerContainerToken)
            return $"{token} is not a valid column";

        return null;
    }

    public static void RegisterOrderAdapter<T, V>(Expression<Func<T, V>> orderByMember)
    {
        OrderAdapters.Add(qt =>
        {
            if (qt.Type != typeof(T))
                return null;

            return ctx =>
            {
                var exp = qt.BuildExpression(ctx);

                return Expression.Invoke(orderByMember, exp);
            };
        });
    }

    public static List<Func<QueryToken, Func<BuildExpressionContext, Expression>?>> OrderAdapters = 
        new List<Func<QueryToken, Func<BuildExpressionContext, Expression>?>>();

    public static LambdaExpression CreateOrderLambda(QueryToken token, BuildExpressionContext ctx)
    {
        foreach (var ad in OrderAdapters)
        {
            var func = ad(token);
            if (func != null)
            {
                var b = func(ctx);
                return Expression.Lambda(b, ctx.Parameter);
            }
        }

        var body = token.BuildExpression(ctx);
        return Expression.Lambda(body, ctx.Parameter);
    }

    public static string? CanOrder(QueryToken token)
    {
        if (token == null)
            return "No column selected";

        if (token.Type.IsEmbeddedEntity() && !OrderAdapters.Any(a => a(token) != null))
            return "{0} can not be ordered".FormatWith(token.Type.NicePluralName());

        if (QueryToken.IsCollection(token.Type))
            return "Collections can not be ordered";

        if (token.HasToArray() != null)
            return "ToArray can not be ordered";

        if (token.HasAllOrAny())
            return "'{0}', '{1}', '{2}' or '{3}' can not be ordered".FormatWith(
                CollectionAnyAllType.All.NiceToString(),
                CollectionAnyAllType.Any.NiceToString(),
                CollectionAnyAllType.NotAny.NiceToString(),
                CollectionAnyAllType.NotAll.NiceToString());

        if (token is OperationsContainerToken or OperationToken or ManualContainerToken or ManualToken or IndexerContainerToken)
            return $"{token} is not a valid order";

        return null;
    }


    static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity ident) => ident.ToLite()).GetGenericMethodDefinition();
    public static Expression ExtractEntity(this Expression expression, bool idAndToStr)
    {
        if (expression.Type.IsLite())
        {
            if (expression is MethodCallExpression mce && mce.Method.IsInstantiationOf(miToLite))
                return mce.Arguments[0];

            if (!idAndToStr)
            return Expression.Property(expression, "Entity");
        }
        return expression;
    }

    internal static Expression BuildLiteNullifyUnwrapPrimaryKey(this Expression expression, PropertyRoute[] routes)
    {
        var buildLite = BuildLite(expression);

        var primaryKey = UnwrapPrimaryKey(buildLite, routes);

        var nullify = primaryKey.Nullify();

        return nullify;
    }

    internal static Type BuildLiteNullifyUnwrapPrimaryKey(this Type type, PropertyRoute[] routes)
    {
        var buildLite = BuildLite(type);

        var primaryKey = UnwrapPrimaryKey(buildLite, routes);

        var nullify = primaryKey.Nullify();

        return nullify;
    }

    internal static Expression UnwrapPrimaryKey(Expression expression, PropertyRoute[] routes)
    {
        var pkType = UnwrapPrimaryKey(expression.Type, routes);

        if (pkType != expression.Type)
        {
            return Expression.Convert(Expression.Field(expression.UnNullify(), "Object"), pkType.Nullify());
        }

        return expression;
    }

    internal static Type UnwrapPrimaryKey(Type type, PropertyRoute[] routes)
    {
        if (type.UnNullify() == typeof(PrimaryKey))
        {
            return routes.Select(r => r.Type.IsMList() ? PrimaryKey.MListType(r) : PrimaryKey.Type(r.RootType)).Distinct().SingleEx();
        }

        return type;
    }

    internal static Expression BuildLite(this Expression expression)
    {
        if (Reflector.IsIEntity(expression.Type))
            return Expression.Call(miToLite.MakeGenericMethod(expression.Type), expression);

        return expression;
    }

    internal static Type BuildLite(this Type type)
    {
        if (Reflector.IsIEntity(type))
            return Lite.Generate(type);

        return type;
    }


    static readonly MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s, StringComparison.InvariantCultureIgnoreCase));
    static readonly MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s, StringComparison.InvariantCultureIgnoreCase));
    static readonly MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s, StringComparison.InvariantCultureIgnoreCase));
    static readonly MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));
    static readonly MethodInfo miDistinctNullable = ReflectionTools.GetMethodInfo((string s) => LinqHints.DistinctNull<int>(null, null)).GetGenericMethodDefinition();
    static readonly MethodInfo miDistinct = ReflectionTools.GetMethodInfo((string s) => LinqHints.DistinctNull<string>(null, null)).GetGenericMethodDefinition();
    static readonly MethodInfo miEquals = ReflectionTools.GetMethodInfo(() => object.Equals(null, null));

    public static Expression GetCompareExpression(FilterOperation operation, Expression left, Expression right, bool inMemory = false)
    {
        switch (operation)
        {
            case FilterOperation.EqualTo:
                {
                    if (inMemory)
                        return Expression.Call(null, miEquals, 
                            Expression.Convert(left, typeof(object)), 
                            Expression.Convert(right, typeof(object)));

                    return Expression.Equal(left, right);
                }
            case FilterOperation.DistinctTo:
                {
                    if (inMemory)
                        return Expression.Not(Expression.Call(null, miEquals,
                            Expression.Convert(left, typeof(object)),
                            Expression.Convert(right, typeof(object))));

                    var t = left.Type.UnNullify();
                    var mi = t.IsValueType ? miDistinctNullable : miDistinct;
                    return Expression.Call(mi.MakeGenericMethod(t), left.Nullify(), right.Nullify());
                }
            case FilterOperation.GreaterThan: return Expression.GreaterThan(CastNumber(left), CastNumber(right));
            case FilterOperation.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(CastNumber(left), CastNumber(right));
            case FilterOperation.LessThan: return Expression.LessThan(CastNumber(left), CastNumber(right));
            case FilterOperation.LessThanOrEqual: return Expression.LessThanOrEqual(CastNumber(left), CastNumber(right));
            case FilterOperation.Contains: return Expression.Call(Fix(left, inMemory), miContains, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
            case FilterOperation.StartsWith: return Expression.Call(Fix(left, inMemory), miStartsWith, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
            case FilterOperation.EndsWith: return Expression.Call(Fix(left, inMemory), miEndsWith, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
            case FilterOperation.Like: return Expression.Call(miLike, Fix(left, inMemory), right);
            case FilterOperation.NotContains: return Expression.Not(Expression.Call(Fix(left, inMemory), miContains, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase)));
            case FilterOperation.NotStartsWith: return Expression.Not(Expression.Call(Fix(left, inMemory), miStartsWith, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase)));
            case FilterOperation.NotEndsWith: return Expression.Not(Expression.Call(Fix(left, inMemory), miEndsWith, right, Expression.Constant(StringComparison.InvariantCultureIgnoreCase)));
            case FilterOperation.NotLike: return Expression.Not(Expression.Call(miLike, Fix(left, inMemory), right));
            default:
                throw new InvalidOperationException("Unknown operation {0}".FormatWith(operation));
        }
    }

    static Expression CastNumber(Expression expression)
    {
        var type = expression.Type.UnNullify();
        if (!type.IsEnum)
            return expression;

        var uType = Enum.GetUnderlyingType(type);

        if(expression.Type.IsNullable())
            uType = uType.Nullify();

        return Expression.Convert(expression, uType);
    }

    private static Expression Fix(Expression left, bool inMemory)
    {
        if (inMemory)
            return Expression.Coalesce(left, Expression.Constant(""));

        return left;
    }

    public static bool IsList(this FilterOperation fo)
    {
        return fo == FilterOperation.IsIn || fo == FilterOperation.IsNotIn;
    }

    public static bool IsTsQuery(this FilterOperation fo)
    {
        return fo == FilterOperation.TsQuery || 
            fo == FilterOperation.TsQuery_Plain ||
            fo == FilterOperation.TsQuery_Phrase ||
            fo == FilterOperation.TsQuery_WebSearch;
    }
}

public enum SubTokensOptions
{
    CanAggregate = 1,
    CanAnyAll = 2,
    CanElement = 4,
    CanOperation = 8,
    CanToArray = 16,
    CanSnippet= 32,
    CanManual = 64,
    CanTimeSeries = 128,
    CanNested = 256,


    All = CanAggregate | CanAnyAll | CanElement | 
        CanOperation | CanToArray | CanSnippet | 
        CanManual | CanTimeSeries | CanNested,
}
