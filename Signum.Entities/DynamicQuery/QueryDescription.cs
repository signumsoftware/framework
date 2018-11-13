using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

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
    public class ColumnDescription :ISerializable
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", name);
            info.AddValue("type", type.AssemblyQualifiedName);

            if (unit != null)
                info.AddValue("unit", unit);

            if (format != null)
                info.AddValue("format", format);

            if (implementations != null)
                info.AddValue("implementations", implementations);

            if (propertyRoutes != null)
            {
                if (propertyRoutes.Length == 1)
                    info.AddValue("propertyRoute", propertyRoutes.Single());
                else
                    info.AddValue("propertyRoutes", propertyRoutes);
            }

            if (displayName != null)
                info.AddValue("displayName", displayName);
        }


        ColumnDescription(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "name": name = (string)entry.Value; break;
                    case "type": type = Type.GetType((string)entry.Value); break;
                    case "unit": unit = (string)entry.Value; break;
                    case "format": format = (string)entry.Value; break;
                    case "implementations": implementations = (Implementations)entry.Value; break;
                    case "propertyRoutes": propertyRoutes = (PropertyRoute[])entry.Value; break;
                    case "propertyRoute": propertyRoutes = new[] { (PropertyRoute)entry.Value }; break;
                    case "displayName": displayName = (string)entry.Value; break;
                }
            }
        }
    }
}
