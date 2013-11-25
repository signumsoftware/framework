using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    public static class QueryUtils
    {
        public static string GetQueryUniqueKey(object queryName)
        {
            if (queryName is Type)
                queryName = EnumEntity.Extract((Type)queryName) ?? (Type)queryName;

            return
                queryName is Type ? ((Type)queryName).FullName :
                queryName is Enum ? "{0}.{1}".Formato(queryName.GetType().Name, queryName.ToString()) :
                queryName.ToString();
        }

        public static string GetCleanName(object queryName)
        {
            if (queryName is Type)
                queryName = EnumEntity.Extract((Type)queryName) ?? (Type)queryName;

            return (queryName is Type ? Reflector.CleanTypeName((Type) queryName) : queryName.ToString());
        }

        public static string GetNiceName(object queryName)
        {
            return GetNiceName(queryName, null); 
        }

        public static string GetNiceName(object queryName, CultureInfo ci)
        {
            if (queryName is Type)
                queryName = EnumEntity.Extract((Type)queryName) ?? (Type)queryName;

            return
                queryName is Type ? ((Type)queryName).NicePluralName() :
                queryName is Enum ? ((Enum)queryName).NiceToString() :
                queryName.ToString();
        }

        public static FilterType GetFilterType(Type type)
        {
            FilterType? filterType = TryGetFilterType(type);

            if(filterType == null)
                throw new NotSupportedException("Type {0} not supported".Formato(type));

            return filterType.Value;
        }

        public static FilterType? TryGetFilterType(Type type)
        {
            var uType = type.UnNullify();

            if (uType == typeof(Guid))
                return FilterType.Guid;

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

                    if (type.IsIIdentifiable())
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

        static Dictionary<FilterType, List<FilterOperation>> FilterOperations = new Dictionary<FilterType, List<FilterOperation>>
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
                    FilterOperation.IsIn
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
                    FilterOperation.IsIn
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
                    FilterOperation.IsIn
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
                    FilterOperation.IsIn
                }
            },
            { 
                FilterType.Enum, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.IsIn
                }
            },
            { 
                FilterType.Guid, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.IsIn
                }
            },
            { 
                FilterType.Lite, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                    FilterOperation.IsIn
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

        public static Func<bool> MergeEntityColumns = null;

        public static QueryToken SubToken(QueryToken token, QueryDescription qd, bool canAggregate, string key)
        {
            var result = SubTokenBasic(token, qd, key);

            if (result != null)
                return result;

            if (canAggregate)
                return AggregateTokens(token, qd).SingleOrDefaultEx(a => a.Key == key);

            return null;
        }

        public static List<QueryToken> SubTokens(this QueryToken token, QueryDescription qd, bool canAggregate)
        {
            var result = SubTokensBasic(token, qd);

            if (canAggregate)
                result.AddRange(AggregateTokens(token, qd));

            return result;
        }

        private static IEnumerable<QueryToken> AggregateTokens(QueryToken token, QueryDescription qd)
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
                else if (ft == FilterType.DateTime) /*ft == FilterType.String || */
                {
                    yield return new AggregateToken(AggregateFunction.Min, token);
                    yield return new AggregateToken(AggregateFunction.Max, token);
                }
            }
        }

        static QueryToken SubTokenBasic(QueryToken token, QueryDescription qd, string key)
        {
            if (token == null)
            {
                var column = qd.Columns.SingleOrDefaultEx(a=>a.Name == key);

                if (column != null)
                    return new ColumnToken(column, qd.QueryName);

                if (MergeEntityColumns != null && !MergeEntityColumns())
                    return null;

                var entity = new ColumnToken(qd.Columns.SingleEx(a => a.IsEntity), qd.QueryName);

                var result = SubTokenBasic(entity, qd, key);

                if (result != null)
                    result.Subordinated = true;

                return result;
            }
            else
            {
                return token.SubTokenInternal(key);
            }
        }

        static List<QueryToken> SubTokensBasic(QueryToken token, QueryDescription qd)
        {
            if (token == null)
            {
                if (MergeEntityColumns != null && !MergeEntityColumns())
                    return qd.Columns.Select(cd => (QueryToken)new ColumnToken(cd, qd.QueryName)).ToList();

                var dictonary = qd.Columns.Where(a => !a.IsEntity).Select(cd => (QueryToken)new ColumnToken(cd, qd.QueryName)).ToDictionary(t => t.Key);

                var entity = new ColumnToken(qd.Columns.SingleEx(a => a.IsEntity), qd.QueryName);

                dictonary.Add(entity.Key, entity);

                foreach (var item in entity.SubTokensInternal().OrderBy(a => a.ToString()))
                {
                    if (!dictonary.ContainsKey(item.Key))
                    {
                        item.Subordinated = true;
                        dictonary.Add(item.Key, item);
                    }
                }

                return dictonary.Values.ToList();
            }
            else
                return token.SubTokensInternal();
        }

        public static QueryToken Parse(string tokenString, QueryDescription qd, bool canAggregate)
        {
            if (string.IsNullOrEmpty(tokenString))
                throw new ArgumentNullException("tokenString");

            string[] parts = tokenString.Split('.');

            string firstPart = parts.FirstEx();

            QueryToken result = SubToken(null, qd, canAggregate, firstPart);

            if (result == null)
                throw new FormatException("Column {0} not found on query {1}".Formato(firstPart, QueryUtils.GetCleanName(qd.QueryName)));

            foreach (var part in parts.Skip(1))
            {
                var newResult = SubToken(result, qd, canAggregate, part);

                if (newResult == null)
                    throw new FormatException("Token with key '{0}' not found on {1} of query {2}".Formato(part, result.FullKey(), QueryUtils.GetCleanName(qd.QueryName)));

                result = newResult;
            }

            return result;
        }

        public static string CanFilter(QueryToken token)
        {
            if (token == null)
                return "No column selected";

            if (token.Type != typeof(string) && token.Type.ElementType() != null)
                return "You can not filter by collections, continue the sequence";
            
            return null;
        }

        public static string CanColumn(QueryToken token)
        {
            if (token == null)
                return "No column selected"; 

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return "You can not add collections as columns";

            if (token.HasAllOrAny())
                return "Columns can not contain '{0}', '{1}', {2} or {3}".Formato(
                    CollectionElementType.All.NiceToString(), 
                    CollectionElementType.Any.NiceToString(),
                    CollectionElementType.NoOne.NiceToString(),
                    CollectionElementType.AnyNo.NiceToString());

            return null; 
        }

        public static string CanOrder(QueryToken token)
        {
            if (token == null)
                return "No column selected"; 

            if (token.Type.IsEmbeddedEntity())
                return "{0} can not be ordered".Formato(token.Type.NicePluralName());

            if (token.HasAllOrAny())
                return "Columns can not contain '{0}', '{1}', {2} or {3}".Formato(
                    CollectionElementType.All.NiceToString(),
                    CollectionElementType.Any.NiceToString(),
                    CollectionElementType.NoOne.NiceToString(),
                    CollectionElementType.AnyNo.NiceToString());

            return null;
        }


        static MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity ident) => ident.ToLite()).GetGenericMethodDefinition();
        internal static Expression ExtractEntity(this Expression expression, bool idAndToStr)
        {
            if (expression.Type.IsLite())
            {
                MethodCallExpression mce = expression as MethodCallExpression;
                if (mce != null && mce.Method.IsInstantiationOf(miToLite))
                    return mce.Arguments[0];

                if (!idAndToStr)
                    return Expression.Property(expression, "Entity");
            }
            return expression;
        }

        internal static Expression BuildLite(this Expression expression)
        {
            if (Reflector.IsIIdentifiable(expression.Type))
                return Expression.Call(miToLite.MakeGenericMethod(expression.Type), expression);

            return expression;
        }

        internal static Type BuildLite(this Type type)
        {
            if (Reflector.IsIIdentifiable(type))
                return Lite.Generate(type);

            return type;
        }


        static MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));
        static MethodInfo miDistinctNullable = ReflectionTools.GetMethodInfo((string s) => LinqHints.DistinctNull<int>(null, null)).GetGenericMethodDefinition();
        static MethodInfo miDistinct = ReflectionTools.GetMethodInfo((string s) => LinqHints.DistinctNull<string>(null, null)).GetGenericMethodDefinition();

        public static Expression GetCompareExpression(FilterOperation operation, Expression left, Expression right, bool inMemory = false)
        {
            switch (operation)
            {
                case FilterOperation.EqualTo: return Expression.Equal(left, right);
                case FilterOperation.DistinctTo: 
                    {
                        var t = left.Type.UnNullify();
                        var mi = t.IsValueType ? miDistinctNullable : miDistinct;
                        return Expression.Call(mi.MakeGenericMethod(t), left.Nullify(), right.Nullify());
                    }
                case FilterOperation.GreaterThan: return Expression.GreaterThan(left, right);
                case FilterOperation.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(left, right);
                case FilterOperation.LessThan: return Expression.LessThan(left, right);
                case FilterOperation.LessThanOrEqual: return Expression.LessThanOrEqual(left, right);
                case FilterOperation.Contains: return Expression.Call(Fix(left, inMemory), miContains, right);
                case FilterOperation.StartsWith: return Expression.Call(Fix(left, inMemory), miStartsWith, right);
                case FilterOperation.EndsWith: return Expression.Call(Fix(left, inMemory), miEndsWith, right);
                case FilterOperation.Like: return Expression.Call(miLike, Fix(left, inMemory), right);
                case FilterOperation.NotContains: return Expression.Not(Expression.Call(Fix(left, inMemory), miContains, right));
                case FilterOperation.NotStartsWith: return Expression.Not(Expression.Call(Fix(left, inMemory), miStartsWith, right));
                case FilterOperation.NotEndsWith: return Expression.Not(Expression.Call(Fix(left, inMemory), miEndsWith, right));
                case FilterOperation.NotLike: return Expression.Not(Expression.Call(miLike, Fix(left, inMemory), right));
                default:
                    throw new InvalidOperationException("Unknown operation {0}".Formato(operation));
            }
        }

        private static Expression Fix(Expression left, bool inMemory)
        {
            if (inMemory)
                return Expression.Coalesce(left, Expression.Constant(""));

            return left;
        }
    }
}
