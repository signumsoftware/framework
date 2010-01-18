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

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ResultTable
    {
        public Column[] Columns { get; private set; }
        public ResultRow[] Rows { get; private set; }
        internal Array[] Values;

        public IEnumerable<Column> VisibleColumns
        {
            get
            {
                return Columns.Where(c => c.Visible);
            }
        }

        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable("Table");
            dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Name, c.Type)).ToArray());
            foreach (var row in Rows)
            {
                dt.Rows.Add(Columns.Select((_, i) => row[i]).ToArray());
            }
            return dt;
        }

        public static ResultTable Create<T>(IEnumerable<T> collection, IEnumerable<Column> columns)
        {
            ResultTable result = new ResultTable();
            result.Rows = collection.Select((_, i) => new ResultRow(i, result)).ToArray();
            result.Columns = columns.ToArray();
            result.Values = columns.Select((c, i) => CreateValuesUntyped(c, collection)).ToArray(); 

            return result;
        }

        

        static Array CreateValuesUntyped(Column column, IEnumerable collection)
        {
            Type getterType = column.Getter.GetType();

            if (getterType.GetGenericTypeDefinition() != typeof(Func<,>))
                throw new InvalidOperationException("column.Getter should be a Func<T,S> for some T and S");

            MethodInfo mi = miCreateValues.MakeGenericMethod(getterType.GetGenericArguments());

            return (Array)mi.Invoke(null, new object[] { column.Getter, collection });
        }


        static MethodInfo miCreateValues = ReflectionTools.GetMethodInfo(() => CreateValues<int, int>(null, null)).GetGenericMethodDefinition();
        static Array CreateValues<T, S>(Func<T, S> getter, IEnumerable<T> collection)
        {
            return collection.Select(getter).ToArray();
        }
    }


    [Serializable]
    public class ResultRow
    {
        public readonly int Index;
        public readonly ResultTable Table; 

        public object this[int columnIndex]
        {
            get { return Table.Values[columnIndex].GetValue(Index); }
        }

        public object this[Column column]
        {
            get { return Table.Values[column.Index].GetValue(Index); }
        }

        internal ResultRow(int index, ResultTable table)
        {
            this.Index = index;
            this.Table = table;
        }
    }

    [Serializable]
    public class QueryDescription
    {
        public Type[] EntityImplementations { get; set; }
        public List<Column> Columns { get; set; }
    }

    [Serializable]
    public class Column
    {
        public int Index { get; internal set; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public PropertyInfo twinProperty; 
        public PropertyInfo TwinProperty 
        {
            get { return twinProperty; }
            set
            {
                twinProperty = value;
                if (twinProperty != null)
                {
                    DisplayName = twinProperty.NiceName();
                    Format = Reflector.FormatString(twinProperty);
                    Unit = twinProperty.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName);
                }
            }
        }
        public string Format { get; set; }
        public string Unit { get; set; }

        public string DisplayName { get; set; }
        public bool Filterable { get; set; }
        public bool Visible { get; set; }
        public bool Sortable { get; set; }

        [NonSerialized]
        readonly internal Delegate Getter;
        [NonSerialized]
        readonly internal Meta Meta;

        public const string Entity = "Entity";
        public bool IsEntity
        {
            get { return this.Name == Entity;  }
        }

        public Column(int index, MemberInfo mi, Meta meta)
        {
            Index = index;
            Name = mi.Name;
           
            Type = mi.ReturningType();
            Meta = meta;

            if (typeof(IdentifiableEntity).IsInstanceOfType(Type))
                throw new InvalidOperationException("{0} column returns subtype of IdentifiableEntity, use a Lite instead!!".Formato(mi.MemberName()));

            if (meta is CleanMeta)
                TwinProperty = (PropertyInfo)((CleanMeta)meta).Member;
            else if (mi is PropertyInfo)
                DisplayName = ((PropertyInfo)mi).NiceName();
            else
                DisplayName = mi.Name.NiceName(); 

            Sortable = true;
            if (IsEntity)
            {
                Visible = false;
                Filterable = false;
            }
            else
            {
                Filterable = true;
                Visible = true;
            }

            ParameterExpression pe = Expression.Parameter(mi.DeclaringType, "p");
            Getter = Expression.Lambda(Expression.PropertyOrField(pe, mi.Name), pe).Compile();
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
                queryKey is Type ? ((Type)queryKey).NicePluralName() :
                queryKey is Enum ? ((Enum)queryKey).NiceToString() :
                queryKey.ToString();
        }
    }
}