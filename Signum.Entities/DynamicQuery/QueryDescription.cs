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

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryResult
    {
        public List<Column> Columns { get; set; }
        public object[][] Data{get;set;}

        public List<Column> VisibleColums
        {
            get { return Columns.Where(c => c.Visible).ToList(); }
        }

        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable("Tabla");
            dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Name, c.Type)).ToArray());
            foreach (var arr in Data)
                dt.Rows.Add(arr);
            return dt;
        }
    }


    [Serializable]
    public class QueryDescription
    {
        public List<Column> Columns { get; set; }
    }

    [Serializable]
    public class Column
    {
        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string DisplayName { get; set; }
        public bool Filterable { get; set; }
        public bool Visible { get; set; }

        [NonSerialized]
        Meta meta;
        internal Meta Meta
        {
            get { return meta; }
            set { meta = value; }
        }

        public PropertyInfo TwinProperty { get; set; }

        public string Format { get; set; }
        public string Unit { get; set; }

        public const string Entity = "Entity";
        public bool IsEntity
        {
            get { return this.Name == Entity;  }
        }

        public Column(MemberInfo mi, Meta meta)
        {
            Name = mi.Name;
            Type = mi.ReturningType();
            Meta = meta;

            if (typeof(IdentifiableEntity).IsInstanceOfType(Type))
                Debug.Write("{0} column returns subtype of IdentifiableEntity, use a Lite instead!!".Formato(mi.MemberName()));

            TwinProperty = (meta as CleanMeta).TryCC(cm => (PropertyInfo)cm.Member);

            if (TwinProperty != null && mi.Name == TwinProperty.Name)
            {
                DisplayName = TwinProperty.NiceName();
                Format = Reflector.FormatString(TwinProperty);
                Unit = TwinProperty.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName);
            }
            else
            {
                DisplayName = Name
                   .Replace("_nf_", "")
                   .Replace("_nv_", "")
                   .Replace("_p_", ".")
                   .Replace("_", " ");
                Format = Reflector.FormatString(Type); 
            }

            if (IsEntity)
            {
                Visible = false;
                Filterable = false;
            }
            else
            {
                Visible = !mi.Name.Contains("_nv_") && !IsEntity;
                Filterable = !mi.Name.Contains("_nf_") && !IsEntity;
            }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(Type.TypeName(), Name); ;
        }
    }

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
                queryKey is Type ? ((Type)queryKey).NiceName() :
                queryKey is Enum ? ((Enum)queryKey).NiceToString() :
                queryKey.ToString();
        }
    }
}