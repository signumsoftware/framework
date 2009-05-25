using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Filter
    {
        public Column Column { get; set; }
        public FilterOperation Operation { get; set; }

        object value;
        public object Value
        {
            get { return value; }

            set
            {
                this.value = ReflectionTools.ChangeType(value, Column.Type); 
            }
        }
    }
    
    [Serializable]
    public enum FilterOperation
    {
        EqualTo,
        DistinctTo, 
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like,
    }

    public static class FilterOperationExtensions
    {
        public static string NiceToString(this FilterOperation fo)
        {
            switch (fo)
            {
                case FilterOperation.EqualTo: return Resources.EqualTo;
                case FilterOperation.DistinctTo: return Resources.DistinctTo;
                case FilterOperation.GreaterThan: return Resources.GreaterThan;
                case FilterOperation.GreaterThanOrEqual: return Resources.GreaterThanOrEqual;
                case FilterOperation.LessThan: return Resources.LessThan;
                case FilterOperation.LessThanOrEqual: return Resources.LessThanOrEqual;
                case FilterOperation.Contains: return Resources.Contains;
                case FilterOperation.StartsWith: return Resources.StartsWith;
                case FilterOperation.EndsWith: return Resources.EndsWith;
                case FilterOperation.Like: return Resources.Like;
            }
            throw new InvalidOperationException();
        }
    }

    public enum FilterType
    {
        Number,
        String, 
        DateTime,
        Lazy,   
        Boolean, 
        Enum,
    }


}
