using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Linq.Expressions;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryDescription
    {
        public object QueryName { get; set; }
        public List<ColumnDescription> Columns { get; set; }
    }

    [Serializable]
    public class ColumnDescription
    {
        public const string Entity = "Entity";

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Unit { get; internal set; }
        public string Format { get; internal set; }

        public Implementations? Implementations { get; internal set; }

        public PropertyRoute[] PropertyRoutes { get; set; }

        public string DisplayName { get; set; }

        public ColumnDescription(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }

        public bool IsEntity
        {
            get { return Name == Entity;  }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }    
}
