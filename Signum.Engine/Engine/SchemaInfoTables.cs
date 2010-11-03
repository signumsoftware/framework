using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Engine.SchemaInfoTables
{
#pragma warning disable 649

   

    [SqlViewName("sys.objects")]
    public class SysObjects : IView
    {
        public int object_id;
        public string name;
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

    [SqlViewName("sys.columns")]
    public class SysColumns : IView
    {
        public string name;
        public int object_id;
        public int column_id;
        public int default_object_id;
        public bool is_nullable;
        public int user_type_id;
        public int max_length;
        public int precision;
        public int scale;
        public bool is_identity; 
    }

    [SqlViewName("sys.types")]
    public class SysTypes : IView
    {
        public int user_type_id;
        public string name;
    }

    [SqlViewName("sys.foreign_keys")]
    public class SysForeignKeys : IView
    {
        public int object_id;
        public string name;
        public int parent_object_id; 
    }

    [SqlViewName("sys.foreign_key_columns")]
    public class SysForeignKeyColumns : IView
    {
        public int constraint_object_id;
        public int parent_object_id;
        public int parent_column_id;
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

#pragma warning restore 649
}
