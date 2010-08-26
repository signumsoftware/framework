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

namespace Signum.Entities.DynamicQuery
{
    public struct ColumnValues
    {
        public ColumnValues(Column column, Array values)
        {
            this.column = column;
            this.values = values;
        }

        Column column;
        public Column Column { get { return column; } }

        Array values;
        public Array Values { get { return values; } }
    }

    [Serializable]
    public class ResultTable
    {
        public Column[] Columns { get; private set; }
        public ResultRow[] Rows { get; private set; }
        internal Array[] Values;

        public ResultTable(params ColumnValues[] columns)
        {
            int rows = columns.Select(a=>a.Values.Length).Distinct().Single("Unsyncronized number of rows in the results"); 

            string errors = columns.Where((c, i) => c.Column.Index != i).ToString(c => "{0} ({1})".Formato(c.Column.Name, c.Column.Index), " ");
            if (errors.HasText())
                throw new InvalidOperationException(Resources.SomeColumnsAreNotCorrectlyNumered0.Formato(errors));

            this.Columns = columns.Where(c => c.Column.IsAllowed()).Select(c=>c.Column).ToArray();
            this.Rows = 0.To(rows).Select(i => new ResultRow(i, this)).ToArray();
            this.Values = columns.Select(c =>!c.Column.IsAllowed()? null: c.Values).ToArray();
        }
   
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

        public T GetValue<T>(string columnName)
        {
            return (T)this[Table.Columns.Where(c => c.Name == columnName).Single("column not found")];
        }

        public T GetValue<T>(int columnIndex)
        {
            return (T)this[columnIndex];
        }

        public T GetValue<T>(Column column)
        {
            return (T)this[column];
        }
    }
}
