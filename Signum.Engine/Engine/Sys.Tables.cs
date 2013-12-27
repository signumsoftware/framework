using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Engine.SchemaInfoTables
{
#pragma warning disable 649

   

    [SqlViewName("sys", "objects")]
    public class SysObjects : IView
    {
        [SqlViewColumn(PrimaryKey=true)]
        public int object_id;
        public int schema_id;
        public string type;
        public string type_desc;
        public string name;
    }


    [SqlViewName("sys", "servers")]
    public class SysServers : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int server_id;
        public string name;
    }

    [SqlViewName("sys", "databases")]
    public class SysDatabases : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int database_id;
        public string name;
        public byte[] owner_sid;

        public bool is_broker_enabled;

        public bool snapshot_isolation_state;
        public bool is_read_committed_snapshot_on;
    }


    [SqlViewName("sys", "server_principals")]
    public class SysServerPrincipals : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [SqlViewName("sys", "database_principals")]
    public class SysDatabasePrincipals : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [SqlViewName("sys", "schemas")]
    public class SysSchemas : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int schema_id;
        public string name;


        static Expression<Func<SysSchemas, IQueryable<SysTables>>> TablesExpression =
            s => Database.View<SysTables>().Where(t => t.schema_id == s.schema_id);
        public IQueryable<SysTables> Tables()
        {
            return TablesExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "tables")]
    public class SysTables : IView
    {
        public string name;
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public int schema_id;

        static Expression<Func<SysTables, IQueryable<SysColumns>>> ColumnsExpression =
            t => Database.View<SysColumns>().Where(c => c.object_id == t.object_id);
        public IQueryable<SysColumns> Columns()
        {
            return ColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysForeignKeys>>> ForeignKeysExpression =
            t => Database.View<SysForeignKeys>().Where(fk => fk.parent_object_id == t.object_id);
        public IQueryable<SysForeignKeys> ForeignKeys()
        {
            return ForeignKeysExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysIndexes>>> IndicesExpression =
            t => Database.View<SysIndexes>().Where(ix => ix.object_id == t.object_id);
        public IQueryable<SysIndexes> Indices()
        {
            return IndicesExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysStats>>> StatsExpression =
            t => Database.View<SysStats>().Where(ix => ix.object_id == t.object_id);
        public IQueryable<SysStats> Stats()
        {
            return StatsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysExtendedProperties>>> ExtendedPropertiesExpression =
            t => Database.View<SysExtendedProperties>().Where(ep => ep.major_id == t.object_id);
        public IQueryable<SysExtendedProperties> ExtendedProperties()
        {
            return ExtendedPropertiesExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysForeignKeyColumns>>> ForeignKeyColumnsExpression =
            fk => Database.View<SysForeignKeyColumns>().Where(fkc => fkc.parent_object_id == fk.object_id);
        public IQueryable<SysForeignKeyColumns> ForeignKeyColumns()
        {
            return ForeignKeyColumnsExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "views")]
    public class SysViews : IView
    {
        public string name;
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;

        public int schema_id;

        static Expression<Func<SysViews, IQueryable<SysIndexes>>> IndicesExpression =
            v => Database.View<SysIndexes>().Where(ix => ix.object_id == v.object_id);
        public IQueryable<SysIndexes> Indices()
        {
            return IndicesExpression.Evaluate(this);
        }

        static Expression<Func<SysViews, IQueryable<SysColumns>>> ColumnsExpression =
            t => Database.View<SysColumns>().Where(c => c.object_id == t.object_id);
        public IQueryable<SysColumns> Columns()
        {
            return ColumnsExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "columns")]
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

    [SqlViewName("sys", "default_constraints")]
    public class SysDefaultConstraints : IView
    {
        public string name;
        public int object_id;
        public int parent_object_id;
        public int parent_column_id;
        public string definition;
        public bool is_system_named;
    }

    [SqlViewName("sys", "types")]
    public class SysTypes : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int system_type_id;
        public int user_type_id;
        public string name;
    }

    [SqlViewName("sys", "foreign_keys")]
    public class SysForeignKeys : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public string name;
        public int parent_object_id;
        public int referenced_object_id;
        public bool is_disabled;
        public bool is_not_trusted; 

        static Expression<Func<SysForeignKeys, IQueryable<SysForeignKeyColumns>>> ForeignKeyColumnsExpression =
            fk => Database.View<SysForeignKeyColumns>().Where(fkc => fkc.constraint_object_id == fk.object_id);
        public IQueryable<SysForeignKeyColumns> ForeignKeyColumns()
        {
            return ForeignKeyColumnsExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "foreign_key_columns")]
    public class SysForeignKeyColumns : IView
    {
        public int constraint_object_id;
        public int constraint_column_id;
        public int parent_object_id;
        public int parent_column_id;
        public int referenced_object_id;
        public int referenced_column_id;
    }

    [SqlViewName("sys", "indexes")]
    public class SysIndexes : IView
    {
        public int index_id;
        public string name;
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public bool is_unique;
        public bool is_primary_key;

        static Expression<Func<SysIndexes, IQueryable<SysIndexColumn>>> IndexColumnsExpression =
            ix => Database.View<SysIndexColumn>().Where(ixc => ixc.index_id == ix.index_id && ixc.object_id == ix.object_id);
        public IQueryable<SysIndexColumn> IndexColumns()
        {
            return IndexColumnsExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "index_columns")]
    public class SysIndexColumn : IView
    {
        public int object_id;
        public int index_id;
        public int column_id;
        public int key_ordinal; 
        public bool is_included_column;
        public bool is_descending_key;
    }

    [SqlViewName("sys", "stats ")]
    public class SysStats : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public int stats_id;
        public string name;
        public bool auto_created;
        public bool user_created;
        public bool no_recompute;

        static Expression<Func<SysStats, IQueryable<SysStatsColumn>>> StatsColumnsExpression =
         ix => Database.View<SysStatsColumn>().Where(ixc => ixc.stats_id == ix.stats_id && ixc.object_id == ix.object_id);
        public IQueryable<SysStatsColumn> StatsColumns()
        {
            return StatsColumnsExpression.Evaluate(this);
        }
    }

    [SqlViewName("sys", "stats_columns")]
    public class SysStatsColumn : IView
    {
        public int object_id;
        public int stats_id;
        public int stats_column_id;
        public int column_id;
    }

    [SqlViewName("sys", "extended_properties")]
    public class SysExtendedProperties : IView
    {
        public int major_id;
        public string name;
    }

    [SqlViewName("sys", "sql_modules")]
    public class SysSqlModules : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public string definition; 
    }

    [SqlViewName("sys", "procedures")]
    public class SysProcedures : IView
    {
        [SqlViewColumn(PrimaryKey = true)]
        public int object_id;
        public string name;
    }

#pragma warning restore 649
}
