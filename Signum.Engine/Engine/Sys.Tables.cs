using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.SchemaInfoTables
{
#pragma warning disable 649

    [TableName("objects", SchemaName= "sys")]
    public class SysObjects : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public string type;
        public string type_desc;
        public string name;
    }

    [TableName("servers", SchemaName = "sys")]
    public class SysServers : IView
    {
        [ViewPrimaryKey]
        public int server_id;
        public string name;
    }

    [TableName("databases", SchemaName = "sys")]
    public class SysDatabases : IView
    {
        [ViewPrimaryKey]
        public int database_id;
        public string name;
        public byte[] owner_sid;

        public bool is_broker_enabled;

        public bool snapshot_isolation_state;
        public bool is_read_committed_snapshot_on;
    }


    [TableName("server_principals", SchemaName = "sys")]
    public class SysServerPrincipals : IView
    {
        [ViewPrimaryKey]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [TableName("database_principals", SchemaName = "sys")]
    public class SysDatabasePrincipals : IView
    {
        [ViewPrimaryKey]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [TableName("schemas", SchemaName = "sys")]
    public class SysSchemas : IView
    {
        [ViewPrimaryKey]
        public int schema_id;
        public string name;

        static Expression<Func<SysSchemas, IQueryable<SysTables>>> TablesExpression =
            s => Database.View<SysTables>().Where(t => t.schema_id == s.schema_id);
        [ExpressionField]
        public IQueryable<SysTables> Tables()
        {
            return TablesExpression.Evaluate(this);
        }
    }

    [TableName("tables", SchemaName = "sys")]
    public class SysTables : IView
    {
        public string name;
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;

        static Expression<Func<SysTables, IQueryable<SysColumns>>> ColumnsExpression =
            t => Database.View<SysColumns>().Where(c => c.object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysColumns> Columns()
        {
            return ColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysForeignKeys>>> ForeignKeysExpression =
            t => Database.View<SysForeignKeys>().Where(fk => fk.parent_object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysForeignKeys> ForeignKeys()
        {
            return ForeignKeysExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysKeyConstraints>>> KeyConstraintsExpression =
            t => Database.View<SysKeyConstraints>().Where(fk => fk.parent_object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysKeyConstraints> KeyConstraints()
        {
            return KeyConstraintsExpression.Evaluate(this);
        }


        static Expression<Func<SysTables, IQueryable<SysIndexes>>> IndicesExpression =
            t => Database.View<SysIndexes>().Where(ix => ix.object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysIndexes> Indices()
        {
            return IndicesExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysStats>>> StatsExpression =
            t => Database.View<SysStats>().Where(ix => ix.object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysStats> Stats()
        {
            return StatsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysExtendedProperties>>> ExtendedPropertiesExpression =
            t => Database.View<SysExtendedProperties>().Where(ep => ep.major_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysExtendedProperties> ExtendedProperties()
        {
            return ExtendedPropertiesExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysForeignKeyColumns>>> ForeignKeyColumnsExpression =
            fk => Database.View<SysForeignKeyColumns>().Where(fkc => fkc.parent_object_id == fk.object_id);
        [ExpressionField]
        public IQueryable<SysForeignKeyColumns> ForeignKeyColumns()
        {
            return ForeignKeyColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);
        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("views", SchemaName = "sys")]
    public class SysViews : IView
    {
        public string name;
        [ViewPrimaryKey]
        public int object_id;

        public int schema_id;

        static Expression<Func<SysViews, IQueryable<SysIndexes>>> IndicesExpression =
            v => Database.View<SysIndexes>().Where(ix => ix.object_id == v.object_id);
        [ExpressionField]
        public IQueryable<SysIndexes> Indices()
        {
            return IndicesExpression.Evaluate(this);
        }

        static Expression<Func<SysViews, IQueryable<SysColumns>>> ColumnsExpression =
            t => Database.View<SysColumns>().Where(c => c.object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysColumns> Columns()
        {
            return ColumnsExpression.Evaluate(this);
        }
    }

    [TableName("columns", SchemaName = "sys")]
    public class SysColumns : IView
    {
        public string name;
        public int object_id;
        public int column_id;
        public int default_object_id;
        public bool is_nullable;
        public int user_type_id;
        public int system_type_id;
        public int max_length;
        public int precision;
        public int scale;
        public bool is_identity; 
    }

    [TableName("default_constraints", SchemaName = "sys")]
    public class SysDefaultConstraints : IView
    {
        public string name;
        public int object_id;
        public int parent_object_id;
        public int parent_column_id;
        public string definition;
        public bool is_system_named;
    }

    [TableName("types", SchemaName = "sys")]
    public class SysTypes : IView
    {
        [ViewPrimaryKey]
        public int system_type_id;
        public int user_type_id;
        public string name;
    }

    [TableName("key_constraints", SchemaName = "sys")]
    public class SysKeyConstraints : IView
    {
        public string name;
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public int parent_object_id;
        public string type;

        static Expression<Func<SysKeyConstraints, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);
        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("foreign_keys", SchemaName = "sys")]
    public class SysForeignKeys : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public string name;
        public int parent_object_id;
        public int referenced_object_id;
        public bool is_disabled;
        public bool is_not_trusted; 

        static Expression<Func<SysForeignKeys, IQueryable<SysForeignKeyColumns>>> ForeignKeyColumnsExpression =
            fk => Database.View<SysForeignKeyColumns>().Where(fkc => fkc.constraint_object_id == fk.object_id);
        [ExpressionField]
        public IQueryable<SysForeignKeyColumns> ForeignKeyColumns()
        {
            return ForeignKeyColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysForeignKeys, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);
        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("foreign_key_columns", SchemaName = "sys")]
    public class SysForeignKeyColumns : IView
    {
        public int constraint_object_id;
        public int constraint_column_id;
        public int parent_object_id;
        public int parent_column_id;
        public int referenced_object_id;
        public int referenced_column_id;
    }

    [TableName("indexes", SchemaName = "sys")]
    public class SysIndexes : IView
    {
        [ViewPrimaryKey]
        public int index_id;
        public string name;
        public int object_id;
        public bool is_unique;
        public bool is_primary_key;
        public int type;
        public string filter_definition;

        static Expression<Func<SysIndexes, IQueryable<SysIndexColumn>>> IndexColumnsExpression =
            ix => Database.View<SysIndexColumn>().Where(ixc => ixc.index_id == ix.index_id && ixc.object_id == ix.object_id);
        [ExpressionField]
        public IQueryable<SysIndexColumn> IndexColumns()
        {
            return IndexColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysIndexes, SysTables>> TableExpression =
            i => Database.View<SysTables>().Single(a => a.object_id == i.object_id); 
        [ExpressionField] 
        public SysTables Table()
        {
            return TableExpression.Evaluate(this);
        }

        static Expression<Func<SysIndexes, SysPartitions>> PartitionExpression =
        ix => Database.View<SysPartitions>().SingleOrDefault(au => au.object_id == ix.object_id && au.index_id == ix.index_id);
        [ExpressionField]
        public SysPartitions Partition()
        {
            return PartitionExpression.Evaluate(this);
        }
    }

    [TableName("index_columns", SchemaName = "sys")]
    public class SysIndexColumn : IView
    {
        public int object_id;
        public int index_id;
        public int column_id;
        public int key_ordinal; 
        public bool is_included_column;
        public bool is_descending_key;
    }

    [TableName("stats", SchemaName = "sys")]
    public class SysStats : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int stats_id;
        public string name;
        public bool auto_created;
        public bool user_created;
        public bool no_recompute;

        static Expression<Func<SysStats, IQueryable<SysStatsColumn>>> StatsColumnsExpression =
         ix => Database.View<SysStatsColumn>().Where(ixc => ixc.stats_id == ix.stats_id && ixc.object_id == ix.object_id);
        [ExpressionField]
        public IQueryable<SysStatsColumn> StatsColumns()
        {
            return StatsColumnsExpression.Evaluate(this);
        }
    }

    [TableName("stats_columns", SchemaName = "sys")]
    public class SysStatsColumn : IView
    {
        public int object_id;
        public int stats_id;
        public int stats_column_id;
        public int column_id;
    }

    [TableName("extended_properties", SchemaName = "sys")]
    public class SysExtendedProperties : IView
    {
        public int major_id;
        public string name;
    }

    [TableName("sql_modules", SchemaName = "sys")]
    public class SysSqlModules : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public string definition; 
    }

    [TableName("procedures", SchemaName = "sys")]
    public class SysProcedures : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public string name;
    }

    [TableName("service_queues", SchemaName = "sys")]
    public class SysServiceQueues : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public string name;
        public string activation_procedure;

        static Expression<Func<SysServiceQueues, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);
        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("partitions", SchemaName = "sys")]
    public class SysPartitions : IView
    {
        [ViewPrimaryKey]
        public int partition_id;
        public int object_id;
        public int index_id;
        public int rows;

        static Expression<Func<SysPartitions, IQueryable<SysAllocationUnits>>> AllocationUnitsExpression =
        ix => Database.View<SysAllocationUnits>().Where(au => au.container_id == ix.partition_id);
        [ExpressionField]
        public IQueryable<SysAllocationUnits> AllocationUnits()
        {
            return AllocationUnitsExpression.Evaluate(this);
        }
    }

    [TableName("allocation_units", SchemaName = "sys")]
    public class SysAllocationUnits : IView
    {
        [ViewPrimaryKey]
        public int container_id;
        public int total_pages;
    }

#pragma warning restore 649
}
