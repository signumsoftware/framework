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
        public QueryToken Token;

        public FilterOperation Operation { get; set; }

        object value;
        public object Value
        {
            get { return value; }

            set
            {
                this.value = ReflectionTools.ChangeType(value, Token.Type); 
            }
        }
    }
    
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

    public enum FilterType
    {
        Number,
        String, 
        DateTime,
        Lite,
        Entity, //Just for complex tokens, not columns
        Embedded,
        Boolean, 
        Enum,
    }

    public enum UniqueType
    {
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        SingleOrMany, 
        Only
    }
}
