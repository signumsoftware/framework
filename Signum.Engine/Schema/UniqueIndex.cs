using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Data;

namespace Signum.Engine.Maps
{
    public class UniqueIndex
    {
        public ITable Table { get; private set; }
        public IColumn[] Columns { get; private set; }
        public string Where { get; set; }

        public UniqueIndex(ITable table, params Field[] fields)
        {
            if (table == null)
                throw new ArgumentNullException("table");

            if (fields == null || fields.IsEmpty())
                throw new InvalidOperationException("No fields");

            if (fields.OfType<FieldEmbedded>().Any())
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            this.Table = table;
            this.Columns = fields.SelectMany(f => f.Columns()).ToArray();
        }


        public UniqueIndex(ITable table, params IColumn[] columns)
        {
            if (table == null)
                throw new ArgumentNullException("table");

            if (columns == null || columns.IsEmpty())
                throw new ArgumentNullException("columns");

            this.Table = table;
            this.Columns = columns;
        }

        public string IndexName
        {
            get { return "IX_{0}_{1}".Formato(Table.Name, ColumnSignature()).TryLeft(Connector.Current.MaxNameLength); }
        }

        public string ViewName
        {
            get
            {
                if (string.IsNullOrEmpty(Where))
                    return null;

                if( Schema.Current.Settings.DBMS > DBMS.SqlServer2005 && !ComplexWhereKeywords.Any(Where.Contains))
                    return null;

                return "VIX_{0}_{1}".Formato(Table.Name, ColumnSignature()).TryLeft(Connector.Current.MaxNameLength);
            }
        }

        static List<string> ComplexWhereKeywords = new List<string>(){"OR"};

        string ColumnSignature()
        {
            string columns = Columns.ToString(c => c.Name, "_");
            if (string.IsNullOrEmpty(Where))
                return columns;

            return columns + "__" + StringHashEncoder.Codify(Where);
        }

        public UniqueIndex WhereNotNull(params IColumn[] notNullColumns)
        {
            if (notNullColumns == null || notNullColumns.IsEmpty())
            {
                Where = null;
                return this;
            }

            this.Where = notNullColumns.ToString(c =>
            {
                string res = c.Name.SqlScape() + " IS NOT NULL";
                if (!IsString(c.SqlDbType))
                    return res;

                return res + " AND " + c.Name.SqlScape() + " <> ''";

            }, " AND ");
            return this;
        }

        private bool IsString(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                    return true;
            }

            return false;
        }

        public UniqueIndex WhereNotNull(params Field[] notNullFields)
        {
            if (notNullFields == null || notNullFields.IsEmpty())
            {
                Where = null;
                return this;
            }

            if (notNullFields.OfType<FieldEmbedded>().Any())
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            this.WhereNotNull(notNullFields.Where(a => !IsComplexIB(a)).SelectMany(a => a.Columns()).ToArray());

            if (notNullFields.Any(IsComplexIB))
                this.Where = " AND ".CombineIfNotEmpty(this.Where, notNullFields.Where(IsComplexIB).ToString(f => "({0})".Formato(f.Columns().ToString(c => c.Name.SqlScape() + " IS NOT NULL", " OR ")), " AND "));

            return this;
        }

        static bool IsComplexIB(Field field)
        {
            return field is FieldImplementedBy && ((FieldImplementedBy)field).ImplementationColumns.Count > 1;
        }

        public override string ToString()
        {
            return IndexName;
        }

        static readonly IColumn[] Empty = new IColumn[0];
    }
}
