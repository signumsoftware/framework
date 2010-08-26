using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;

namespace Signum.Entities.DynamicQuery
{
    public static class QueryUtils
    {
        public static string GetQueryName(object queryKey)
        {
            return
                queryKey is Type ? ((Type)queryKey).FullName :
                queryKey is Enum ? "{0}.{1}".Formato(queryKey.GetType().Name, queryKey.ToString()) :
                queryKey.ToString();
        }

        public static string GetNiceQueryName(object queryKey)
        {
            return
                queryKey is Type ? ((Type)queryKey).NicePluralName() :
                queryKey is Enum ? ((Enum)queryKey).NiceToString() :
                queryKey.ToString();
        }

        public static FilterType GetFilterType(Type type)
        {
            FilterType? filterType = TryGetFilterType(type);

            if(filterType == null)
                throw new NotSupportedException(Resources.Type0NotSupported.Formato(type));

            return filterType.Value;
        }

        public static FilterType? TryGetFilterType(Type type)
        {
            if (type.IsEnum)
                return FilterType.Enum;

            switch (Type.GetTypeCode(type.UnNullify()))
            {
                case TypeCode.Boolean:
                    return FilterType.Boolean;
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    return FilterType.DecimalNumber;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return FilterType.Number;
                case TypeCode.DateTime:
                    return FilterType.DateTime;
                case TypeCode.Char:
                case TypeCode.String:
                    return FilterType.String;
                case TypeCode.Object:
                    if (Reflector.ExtractLite(type) != null)
                        return FilterType.Lite;

                    if (typeof(IIdentifiable).IsAssignableFrom(type))
                        return FilterType.Lite;

                    if (typeof(EmbeddedEntity).IsAssignableFrom(type))
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
                    FilterOperation.StartsWith,
                    FilterOperation.EndsWith,
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
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
                    FilterOperation.LessThanOrEqual
                }
            },
            { 
                FilterType.Number, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.GreaterThan,
                    FilterOperation.GreaterThanOrEqual,
                    FilterOperation.LessThan,
                    FilterOperation.LessThanOrEqual,
                }
            },
            { 
                FilterType.DecimalNumber, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.GreaterThan,
                    FilterOperation.GreaterThanOrEqual,
                    FilterOperation.LessThan,
                    FilterOperation.LessThanOrEqual,
                }
            },
            { 
                FilterType.Enum, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                }
            },
            { 
                FilterType.Lite, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
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

        public static QueryToken[] SubTokens(QueryToken token, IEnumerable<StaticColumn> staticColumns)
        {
            if (token == null)
                return staticColumns.Select(s => QueryToken.NewColumn(s)).ToArray();
            else
                return token.SubTokens();
        }

        public static QueryToken[] SubTokensFilter(QueryToken token, IEnumerable<StaticColumn> staticColumns)
        {
            return SubTokens(token, staticColumns.Where(s => s.Filterable));
        }


        public static QueryToken[] SubTokensOrder(QueryToken token, IEnumerable<StaticColumn> staticColumns)
        {
            return SubTokens(token, staticColumns.Where(sc => sc.Sortable));
        }


        public static QueryToken[] SubTokensColumn(QueryToken token, IEnumerable<StaticColumn> staticColumns)
        {
            return SubTokens(token, staticColumns);
        }

        public static QueryToken ParseFilter(string tokenString, QueryDescription description)
        {
            return Parse(tokenString, t => SubTokensFilter(t, description.StaticColumns));
        }

        public static QueryToken ParseOrder(string tokenString, QueryDescription description)
        {
            return Parse(tokenString, t => SubTokensOrder(t, description.StaticColumns));
        }

        public static QueryToken ParseColumn(string tokenString, QueryDescription description)
        {
            return Parse(tokenString, t => SubTokensColumn(t, description.StaticColumns));
        }

        public static QueryToken Parse(string tokenString, Func<QueryToken, QueryToken[]> subTokens)
        {
            try
            {
                string[] parts = tokenString.Split('.');

                string firstPart = parts.First();

                QueryToken result = subTokens(null).Select(t => t.Match(firstPart)).NotNull().Single(
                    Resources.Column0NotFound.Formato(firstPart),
                    Resources.MoreThanOneColumnNamed0.Formato(firstPart));

                foreach (var part in parts.Skip(1))
                {
                    result = subTokens(result).Select(t => t.Match(part)).NotNull().Single(
                          Resources.Token0NotCompatibleWith1.Formato(part, result),
                          Resources.MoreThanOneTokenWithKey0FoundOn1.Formato(part, result));
                }

                return result;
            }
            catch (Exception e)
            {
                throw new FormatException("Invalid QueryToken string", e);
            }
        }


       
    }
}
