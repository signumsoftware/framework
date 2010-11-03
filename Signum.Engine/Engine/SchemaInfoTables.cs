using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Engine.SchemaInfoTables
{
#pragma warning disable 649

    public static class SqlMethods
    {
        [SqlMethod]
        public static int Object_id(string name)
        {
            return 0;
        }

        [SqlMethod]
        public static int ColumnProperty(int tableID, string column, string property)
        {
            return 0;
        }
    }

    [SqlViewName("sys.indexes")]
    public class SysIndexes : IView
    {
        public int index_id;
        public string name;
        public int object_id;
        public bool is_unique;
        public bool is_primary_key;
    }

    [SqlViewName("sys.index_columns")]
    public class SysIndexColumn : IView
    {
        public int object_id;
        public int index_id;
        public int column_id;
    }

    [SqlViewName("sys.sysobjects")]
    public class SysObjects : IView
    {
        public int id;
        public string name;
    }

    [SqlViewName("sys.columns")]
    public class SysColumns : IView
    {
        public string name;
        public int object_id;
        public int column_id;
        public int default_object_id; 
    }

    [SqlViewName("sys.tables")]
    public class SysTables : IView
    {
        public string name;
        public int object_id;
    }

    [SqlViewName("sys.views")]
    public class SysViews : IView
    {
        public string name;
        public int object_id;
    }

    [SqlViewName("information_schema.TABLE_CONSTRAINTS")]
    public class SchemaTableConstraints : IView
    {
        public string CONSTRAINT_CATALOG;
        public string CONSTRAINT_SCHEMA;
        public string CONSTRAINT_NAME;
        public string TABLE_CATALOG;
        public string TABLE_SCHEMA;
        public string TABLE_NAME;
        public string CONSTRAINT_TYPE;
    }

    [SqlViewName("information_schema.key_column_usage")]
    public class SchemaKeyColumnUsage : IView
    {
        public string CONSTRAINT_CATALOG;
        public string CONSTRAINT_SCHEMA;
        public string CONSTRAINT_NAME;
        public string TABLE_CATALOG;
        public string TABLE_SCHEMA;
        public string TABLE_NAME;
        public string COLUMN_NAME;
        public int ORGINAL_POSITION;
    }


    [SqlViewName("information_schema.tables")]
    public class SchemaTables : IView
    {
        public string TABLE_CATALOG;
        public string TABLE_SCHEMA;
        public string TABLE_NAME;
        public string TABLE_TYPE;
    }

    [SqlViewName("information_schema.columns")]
    public class SchemaColumns : IView
    {
        public string TABLE_CATALOG;
        public string TABLE_SCHEMA;
        public string TABLE_NAME;
        public string COLUMN_NAME;
        public int ORDINAL_POSITION;
        public string IS_NULLABLE;
        public string DATA_TYPE;
        public int? CHARACTER_MAXIMUM_LENGTH;
        public int? NUMERIC_PRECISION;
        public int? NUMERIC_SCALE;
    }
#pragma warning restore 649
}
