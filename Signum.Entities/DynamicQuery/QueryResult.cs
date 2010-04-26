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
    [Serializable]
    public class ResultTable
    {
        public Column[] Columns { get; private set; }
        public ResultRow[] Rows { get; private set; }
        internal Array[] Values;

        public ResultTable(StaticColumn[] staticColumns, UserColumn[] userColumns, int rows, Func<Column, Array> values)
        {
            var columns = staticColumns.Cast<Column>().Concat(userColumns.Cast<Column>()).ToArray();
            string errors = columns.Where((c, i) => c.Index != i).ToString(c => "{0} ({1})".Formato(c.Name, c.Index), " ");
            if (errors.HasText())
                throw new InvalidOperationException(Resources.SomeColumnsAreNotCorrectlyNumered0.Formato(errors));

            this.Columns = columns.Where(c => c.IsAllowed()).ToArray();
            this.Rows = 0.To(rows).Select(i => new ResultRow(i, this)).ToArray();
            this.Values = columns.Select(c =>
                {
                    if (!c.IsAllowed())
                        return null;

                    Array array = values(c);
                    if (array.Length != rows)
                        throw new InvalidOperationException(Resources.ResultsInsteadOf1ForColumn2.Formato(array.Length, rows, c.Name));
                    return array;
                }).ToArray();
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
