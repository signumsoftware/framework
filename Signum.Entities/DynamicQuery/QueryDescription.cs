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
        object queryName;
        public object QueryName
        {
            get { return queryName; }
        }

        List<ColumnDescription> columns;
        public List<ColumnDescription> Columns
        {
            get { return columns; }
        }

        public QueryDescription() { }

        public QueryDescription(object queryName, List<ColumnDescription> columns)
        {
            this.queryName = queryName;
            this.columns = columns;
        }
    }

    [Serializable]
    public class ColumnDescription
    {
        public const string Entity = "Entity";

        string name;
        public string Name
        {
            get { return name; }
            internal set { name = value; }
        }

        Type type;
        public Type Type
        {
            get { return type; }
            internal set { type = value; }
        }

        string unit;
        public string Unit
        {
            get { return unit; }
            internal set { unit = value; }
        }

        string format;
        public string Format
        {
            get { return format; }
            internal set { format = value; }
        }

        Implementations? implementations;
        public Implementations? Implementations
        {
            get { return implementations; }
            internal set { implementations = value; }
        }

        PropertyRoute[] propertyRoutes;
        public PropertyRoute[] PropertyRoutes
        {
            get { return propertyRoutes; }
            internal set { propertyRoutes = value; }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            internal set { displayName = value; }
        }

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
