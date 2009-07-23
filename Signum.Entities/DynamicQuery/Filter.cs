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
        public string Name { get; set; }

        public Type Type { get; set; }

        public FilterOperation Operation { get; set; }

        object value;
        public object Value
        {
            get { return value; }

            set
            {
                this.value = ReflectionTools.ChangeType(value, Type); 
            }
        }
    }
    
    [Serializable]
    public enum FilterOperation
    {
        [LocDescription]
        EqualTo,
        [LocDescription]
        DistinctTo,
        [LocDescription]
        GreaterThan,
        [LocDescription]
        GreaterThanOrEqual,
        [LocDescription]
        LessThan,
        [LocDescription]
        LessThanOrEqual,
        [LocDescription]
        Contains,
        [LocDescription]
        StartsWith,
        [LocDescription]
        EndsWith,
        [LocDescription]
        Like,
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
