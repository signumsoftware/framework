using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ResultTable
    {
        public StaticColumn[] StaticColumns { get; private set; }
        public UserColumn[] UserColumns { get; private set; }
        public ResultRow[] Rows { get; private set; }
        internal Array[] Values;

        public ResultTable(StaticColumn[] staticColumns, UserColumn[] userColumns, int rows, Func<Column, Array> values)
        {
            this.StaticColumns = staticColumns;
            this.UserColumns = userColumns;
            this.Rows = 0.To(rows).Select(i => new ResultRow(i, this)).ToArray();
            this.Values = staticColumns.Cast<Column>().Concat(userColumns.Cast<Column>()).Select(c => values(c)).ToArray();
        }

        public IEnumerable<Column> Columns
        {
            get
            {
                return StaticColumns.Cast<Column>().Concat(UserColumns.Cast<Column>());
            }
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
    }
}
