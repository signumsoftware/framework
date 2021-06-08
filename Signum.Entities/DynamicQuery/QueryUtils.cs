using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;

namespace Signum.Entities.DynamicQuery
{
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

            if (uType == typeof(Date) || uType == typeof(DateTimeOffset))
                return FilterType.DateTime;

            if (uType == typeof(TimeSpan))
                return FilterType.Time;

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

                    goto default;
                default:
                    return null;

            }
        }

        public static List<FilterOperation> GetFilterOperations(FilterType filtertype)
        {
            return FilterOperations[filtertype];
        }

        static readonly Dictionary<FilterType, List<FilterOperation>> FilterOperations = new Dictionary<FilterType, List<FilterOperation>>
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
                }
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
                }
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
                }
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
                }
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
                }
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
                }
            },
            {
                FilterType.Guid, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                    FilterOperation.IsIn,
                    FilterOperation.IsNotIn,
                }
            },
            {
                FilterType.Lite, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                    FilterOperation.IsIn,
                    FilterOperation.IsNotIn,
                }
            },
            {
                FilterType.Embedded, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                }
            },
            {
                FilterType.Boolean, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                }
            },
        };


        public static QueryToken? SubToken(QueryToken? token, QueryDescription qd, SubTokensOptions options, string key)
        {
            var result = SubTokenBasic(token, qd, options, key);

            if (result != null)
                return result;

            if ((options & SubTokensOptions.CanAggregate) != 0)
                return AggregateTokens(token, qd).SingleOrDefaultEx(a => a.Key == key);

            return null;
        }

        public static List<QueryToken> SubTokens(this QueryToken? token, QueryDescription qd, SubTokensOptions options)
        {
            var result = SubTokensBasic(token, qd, options);

            if ((options & SubTokensOptions.CanAggregate) != 0)
                result.InsertRange(0, AggregateTokens(token, qd));

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

        public static Func<bool>? MergeEntityColumns = null;
        static List<QueryToken> SubTokensBasic(QueryToken? token, QueryDescription qd, SubTokensOptions options)
        {
            if (token == null)
            {
                if (MergeEntityColumns != null && !MergeEntityColumns())
                    return qd.Columns.Select(cd => (QueryToken)new ColumnToken(cd, qd.QueryName)).ToList();

                var dictonary = qd.Columns.Where(a => !a.IsEntity).Select(cd => (QueryToken)new ColumnToken(cd, qd.QueryName)).ToDictionary(t => t.Key);

                var entity = new ColumnToken(qd.Columns.SingleEx(a => a.IsEntity), qd.QueryName);

                dictonary.Add(entity.Key, entity);

                foreach (var item in entity.SubTokensInternal(options).OrderByDescending(a=>a.Priority).ThenBy(a => a.ToString()))
                {
                    if (!dictonary.ContainsKey(item.Key))
                    {
                        dictonary.Add(item.Key, item);
                    }
                }

                return dictonary.Values.ToList();

            }
            else
                return token.SubTokensInternal(options);
        }

        public static bool IsColumnToken(string tokenString)
        {
            return tokenString.IndexOf('.') == -1 && tokenString != "Entity";
        }

        public static QueryToken Parse(string tokenString, QueryDescription qd, SubTokensOptions options)
        {
            if (string.IsNullOrEmpty(tokenString))
                throw new ArgumentNullException(nameof(tokenString));

            //https://stackoverflow.com/questions/35418597/split-string-on-the-dot-characters-that-are-not-inside-of-brackets
            string[] parts = Regex.Split(tokenString, @"\.(?!([^[]*\]|[^(]*\)))");

            string firstPart = parts.FirstEx();

            QueryToken? result = SubToken(null, qd, options, firstPart);

            if (result == null)
                throw new FormatException("Column {0} not found on query {1}".FormatWith(firstPart, QueryUtils.GetKey(qd.QueryName)));

            foreach (var part in parts.Skip(1))
            {
                var newResult = SubToken(result, qd, options, part);
                result = newResult ?? throw new FormatException("Token with key '{0}' not found on {1} of query {2}".FormatWith(part, result.FullKey(), QueryUtils.GetKey(qd.QueryName)));
            }

            return result;
        }

        public static string? CanFilter(QueryToken token)
        {
            if (token == null)
                return "No column selected";

            if (token.Type != typeof(string) && token.Type.ElementType() != null)
                return "You can not filter by collections, continue the sequence";

            return null;
        }

        public static string? CanColumn(QueryToken token)
        {
            if (token == null)
                return "No column selected";

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return "You can not add collections as columns";

            if (token.HasAllOrAny())
                return "Columns can not contain '{0}', '{1}', {2} or {3}".FormatWith(
                    CollectionAnyAllType.All.NiceToString(),
                    CollectionAnyAllType.Any.NiceToString(),
                    CollectionAnyAllType.NoOne.NiceToString(),
                    CollectionAnyAllType.AnyNo.NiceToString());

            return null;
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

            if (token.HasAllOrAny())
                return "'{0}', '{1}', '{2}' or '{3}' can not be ordered".FormatWith(
                    CollectionAnyAllType.All.NiceToString(),
                    CollectionAnyAllType.Any.NiceToString(),
                    CollectionAnyAllType.NoOne.NiceToString(),
                    CollectionAnyAllType.AnyNo.NiceToString());

            return null;
        }


        static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity ident) => ident.ToLite()).GetGenericMethodDefinition();
        internal static Expression ExtractEntity(this Expression expression, bool idAndToStr)
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
            if (expression.Type.UnNullify() == typeof(PrimaryKey))
            {
                var unwrappedType = routes.Select(r => PrimaryKey.Type(r.RootType)).Distinct().SingleEx();

                return Expression.Convert(Expression.Field(expression.UnNullify(), "Object"), unwrappedType.Nullify());
            }

            return expression;
        }

        internal static Type UnwrapPrimaryKey(Type type, PropertyRoute[] routes)
        {
            if (type.UnNullify() == typeof(PrimaryKey))
            {
                return routes.Select(r => PrimaryKey.Type(r.RootType)).Distinct().SingleEx().Nullify();
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
    }

    public enum SubTokensOptions
    {
        CanAggregate = 1,
        CanAnyAll = 2,
        CanElement = 4,
    }
}
