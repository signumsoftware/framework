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

    [TableName("sys.objects")]
    public class SysObjects : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public string type;
        public string type_desc;
        public string name;
    }

    [TableName("sys.servers")]
    public class SysServers : IView
    {
        [ViewPrimaryKey]
        public int server_id;
        public string name;
    }

    [TableName("sys.databases")]
    public class SysDatabases : IView
    {
        [ViewPrimaryKey]
        public int database_id;
        public string name;
        public byte[] owner_sid;
        public string collation_name;
        public bool is_broker_enabled;

        public bool snapshot_isolation_state;
        public bool is_read_committed_snapshot_on;
    }


    [TableName("sys.server_principals")]
    public class SysServerPrincipals : IView
    {
        [ViewPrimaryKey]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [TableName("sys.database_principals")]
    public class SysDatabasePrincipals : IView
    {
        [ViewPrimaryKey]
        public int principal_id;
        public string name;
        public byte[] sid;
        public string type_desc;
    }

    [TableName("sys.schemas")]
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

    public enum SysTableTemporalType
    {
        None = 0,
        HistoryTable = 1, 
        SystemVersionTemporalTable = 2
    }

    [TableName("sys.periods")]
    public class SysPeriods : IView
    {
        [ViewPrimaryKey]
        public int object_id;

        public int start_column_id;
        public int end_column_id;
    }


    [TableName("sys.tables")]
    public class SysTables : IView
    {
        public string name;
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;

        [ColumnName("temporal_type")]
        public SysTableTemporalType temporal_type;
        public int? history_table_id;


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
            t => Database.View<SysForeignKeyColumns>().Where(fkc => fkc.parent_object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysForeignKeyColumns> ForeignKeyColumns()
        {
            return ForeignKeyColumnsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, IQueryable<SysPeriods>>> PeriodsExpression =
            t => Database.View<SysPeriods>().Where(p => p.object_id == t.object_id);
        [ExpressionField]
        public IQueryable<SysPeriods> Periods()
        {
            return PeriodsExpression.Evaluate(this);
        }

        static Expression<Func<SysTables, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);

        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("sys.views")]
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

    [TableName("sys.columns")]
    public class SysColumns : IView
    {
        public string name;
        [ViewPrimaryKey]
        public int object_id;
        public int column_id;
        public int default_object_id;
        public string collation_name;
        public bool is_nullable;
        public int user_type_id;
        public int system_type_id;
        public int max_length;
        public int precision;
        public int scale;
        public bool is_identity;

        [ColumnName("generated_always_type")]
        public GeneratedAlwaysType generated_always_type;

        static Expression<Func<SysColumns, SysTypes>> TypeExpression =
            c => Database.View<SysTypes>().SingleOrDefaultEx(a => a.system_type_id == c.system_type_id);
        [ExpressionField]
        public SysTypes Type()
        {
            return TypeExpression.Evaluate(this);
        }
    }

    [TableName("sys.default_constraints")]
    public class SysDefaultConstraints : IView
    {
        public string name;
        public int object_id;
        public int parent_object_id;
        public int parent_column_id;
        public string definition;
        public bool is_system_named;
    }

    [TableName("sys.types")]
    public class SysTypes : IView
    {
        [ViewPrimaryKey]
        public int system_type_id;
        public int user_type_id;
        public string name;
    }

    [TableName("sys.key_constraints")]
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

    [TableName("sys.foreign_keys")]
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

    [TableName("sys.foreign_key_columns")]
    public class SysForeignKeyColumns : IView
    {
        public int constraint_object_id;
        public int constraint_column_id;
        public int parent_object_id;
        public int parent_column_id;
        public int referenced_object_id;
        public int referenced_column_id;
    }

    [TableName("sys.indexes")]
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

    [TableName("sys.index_columns")]
    public class SysIndexColumn : IView
    {
        public int object_id;
        public int index_id;
        public int column_id;
        public int index_column_id;
        public int key_ordinal; 
        public bool is_included_column;
        public bool is_descending_key;
    }

    [TableName("sys.stats")]
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

    [TableName("sys.stats_columns")]
    public class SysStatsColumn : IView
    {
        public int object_id;
        public int stats_id;
        public int stats_column_id;
        public int column_id;
    }

    [TableName("sys.extended_properties")]
    public class SysExtendedProperties : IView
    {
        public int major_id;
        public string name;
    }

    [TableName("sys.sql_modules")]
    public class SysSqlModules : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public string definition; 
    }

    [TableName("sys.procedures")]
    public class SysProcedures : IView
    {
        [ViewPrimaryKey]
        public int object_id;
        public int schema_id;
        public string name;


        static Expression<Func<SysProcedures, SysSchemas>> SchemaExpression =
            i => Database.View<SysSchemas>().Single(a => a.schema_id == i.schema_id);
        [ExpressionField]
        public SysSchemas Schema()
        {
            return SchemaExpression.Evaluate(this);
        }
    }

    [TableName("sys.service_queues")]
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

    [TableName("sys.partitions")]
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

    [TableName("sys.allocation_units")]
    public class SysAllocationUnits : IView
    {
        [ViewPrimaryKey]
        public int container_id;
        public int total_pages;
    }

#pragma warning restore 649
}
