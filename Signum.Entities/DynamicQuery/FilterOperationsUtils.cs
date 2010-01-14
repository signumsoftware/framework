using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    public static class FilterOperationsUtils
    {
        public static FilterType GetFilterType(Type type)
        {
            switch (Type.GetTypeCode(type.UnNullify()))
            {
                case TypeCode.Boolean:
                    return FilterType.Boolean;
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Single:
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
                     if (type.IsEnum)
                        return FilterType.Enum;

                     if (Reflector.ExtractLite(type) != null)
                         return FilterType.Lite;

                     goto default; 
                default:
                    throw new ApplicationException(Resources.Type0NotSupported.Formato(type));
            }
        }

        public static Dictionary<FilterType, List<FilterOperation>> FilterOperations = new Dictionary<FilterType, List<FilterOperation>>
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
                FilterType.Lite, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                    FilterOperation.Contains,
                    FilterOperation.StartsWith,
                    FilterOperation.EndsWith,
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
    }

}
