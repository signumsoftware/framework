using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Properties;
using System.Runtime.Serialization;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ResultColumn
    {
        Column column;
        public Column Column
        {
            get { return column; }
        }

        int index;
        public int Index
        {
            get { return index; }
            internal set { index = value; }
        }

        internal IList Values;

        public ResultColumn(Column column, IList values)
        {
            this.column = column;
            this.Values = values;
        }
      
    }

    [Serializable]
    public class ResultTable 
    {
        internal IList entityValues;
        public bool HasEntities
        {
            get { return entityValues != null; }
        }

        public ColumnDescription entityColumn;
        public ColumnDescription EntityColumn { get { return entityColumn; } }

        ResultColumn[] columns;
        public ResultColumn[] Columns { get { return columns; } }

        ResultRow[] rows;
        public ResultRow[] Rows { get { return rows; } }

        public ResultTable(ResultColumn[] columns, int totalElements, int currentPage, int elementsPerPage)
        {
            int rows = columns.Select(a => a.Values.Count).Distinct().SingleEx(() => "Unsyncronized number of rows in the results");

            ResultColumn entity = columns.Where(c => c.Column is _EntityColumn).SingleOrDefaultEx(); ;
            if (entity != null)
            {
                this.entityColumn = ((ColumnToken)entity.Column.Token).Column;
                entityValues = entity.Values;
            }
            this.columns = columns.Where(c => !(c.Column is _EntityColumn) && c.Column.Token.IsAllowed() == null).ToArray();

            for (int i = 0; i < Columns.Length; i++)
                Columns[i].Index = i;

            this.rows = 0.To(rows).Select(i => new ResultRow(i, this)).ToArray();

            this.totalElements = totalElements;
            this.currentPage = currentPage;
            this.elementsPerPage = elementsPerPage;
        }

        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable("Table");
            dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Column.Name,
                c.Column.Type.IsLite() ? typeof(string) : c.Column.Type.UnNullify())).ToArray());
            foreach (var row in Rows)
            {
                dt.Rows.Add(Columns.Select((_, i) => Convert(row[i])).ToArray());
            }
            return dt;
        }

        private object Convert(object p)
        {
            if (p is Lite<IdentifiableEntity>)
                return ((Lite<IdentifiableEntity>)p).KeyLong();

            return p;
        }

        int totalElements;
        public int TotalElements { get { return totalElements; } }

        int currentPage;
        public int CurrentPage { get { return currentPage; } }

        int elementsPerPage;
        public int ElementsPerPage { get { return elementsPerPage; } }

        public int TotalPages
        {
            get { return ElementsPerPage == -1 ? 1 : (TotalElements + ElementsPerPage - 1) / ElementsPerPage; } //Round up
        }

        public int? StartElementIndex
        {
            get { return ElementsPerPage == -1 ? (int?)null : (ElementsPerPage * (CurrentPage - 1)) + 1; }
        }

        public int? EndElementIndex
        {
            get { return ElementsPerPage == -1 ? (int?)null : StartElementIndex.Value + Rows.Count() - 1; }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            Dictionary<string, string> strings = new Dictionary<string, string>();
            Dictionary<Lite<IdentifiableEntity>, Lite<IdentifiableEntity>> lites = new Dictionary<Lite<IdentifiableEntity>, Lite<IdentifiableEntity>>();

            if (entityValues != null)
                NormalizeLites(entityValues, lites);

            foreach (var col in columns)
            {
                if (col.Values is IEnumerable<Lite<IIdentifiable>>)
                    NormalizeLites(col.Values, lites);
                else if (col.Values is IEnumerable<string>)
                    NormalizeStrings(col.Values, strings);
            }
        }

        static void NormalizeStrings(IList values, Dictionary<string, string> strings)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var val = (string)values[i];
                if (val != null)
                    values[i] = strings.GetOrCreate(val, val);
            }
        }

        static void NormalizeLites(IList values, Dictionary<Lite<IdentifiableEntity>, Lite<IdentifiableEntity>> lites)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var val = (Lite<IdentifiableEntity>)values[i];
                if (val != null)
                    values[i] = lites.GetOrCreate(val, val);
            }
        }
    }


    [Serializable]
    public class ResultRow
    {
        public readonly int Index;
        public readonly ResultTable Table;

        public object this[int columnIndex]
        {
            get { return Table.Columns[columnIndex].Values[Index]; }
        }

        public object this[ResultColumn column]
        {
            get { return column.Values[Index]; }
        }

        internal ResultRow(int index, ResultTable table)
        {
            this.Index = index;
            this.Table = table;
        }

        public Lite<IdentifiableEntity> Entity
        {
            get { return (Lite<IdentifiableEntity>)Table.entityValues[Index]; }
        }

        public T GetValue<T>(string columnName)
        {
            return (T)this[Table.Columns.Where(c => c.Column.Name == columnName).SingleEx(() => "column not found")];
        }

        public T GetValue<T>(int columnIndex)
        {
            return (T)this[columnIndex];
        }

        public T GetValue<T>(ResultColumn column)
        {
            return (T)this[column];
        }
    }
}
