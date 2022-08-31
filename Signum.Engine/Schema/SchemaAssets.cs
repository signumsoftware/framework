using Signum.Engine.SchemaInfoTables;
using Signum.Engine.PostgresCatalog;

namespace Signum.Engine.Maps;

public class SchemaAssets
{
    internal SqlPreCommand? Schema_Generating()
    {
        SqlPreCommand? views = GenerateViews();
        SqlPreCommand? procedures = GenerateProcedures();

        return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
    }

    internal SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        SqlPreCommand? views = SyncViews(replacements, drop: false);
        SqlPreCommand? procedures = SyncProcedures(replacements, drop: false);

        return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
    }

    internal SqlPreCommand? Schema_SynchronizingDrop(Replacements replacements)
    {
        SqlPreCommand? views = SyncViews(replacements, drop: true);
        SqlPreCommand? procedures = SyncProcedures(replacements, drop: true);

        return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
    }

    static string Clean(string command)
    {
        return command.Replace("\r", "").Trim(' ', '\n', ';');
    }

    #region Views
    public class View
    {
        public ObjectName Name;
        public string Definition;

        public View(ObjectName name, string definition)
        {
            Name = name;
            Definition = definition;
        }

        public SqlPreCommandSimple CreateView()
        {
            return new SqlPreCommandSimple("CREATE VIEW {0} ".FormatWith(Name) + Definition) { GoBefore = true, GoAfter = true };
        }

        public SqlPreCommandSimple AlterView()
        {
            return new SqlPreCommandSimple("ALTER VIEW {0} ".FormatWith(Name) + Definition) { GoBefore = true, GoAfter = true };
        }

        public SqlPreCommandSimple DropView()
        {
            return new SqlPreCommandSimple("DROP VIEW {0} ".FormatWith(Name)) { GoBefore = true, GoAfter = true };
        }
    }

    public View IncludeView(string viewName, string viewDefinition)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return IncludeView(new ObjectName(SchemaName.Default(isPostgres), viewName, isPostgres), viewDefinition);
    }

    public Dictionary<ObjectName, View> Views = new Dictionary<ObjectName, View>();
    public View IncludeView(ObjectName viewName, string viewDefinition)
    {
        return Views[viewName] = new View(viewName, viewDefinition);
    }

    SqlPreCommand? GenerateViews()
    {
        return Views.Values.Select(v => v.CreateView()).Combine(Spacing.Double);
    }

    SqlPreCommand? SyncViews(Replacements replacements, bool drop)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var oldView = Schema.Current.DatabaseNames().SelectMany(db =>
        {
            if (isPostgres)
            {
                if (db != null)
                    throw new InvalidOperationException("Multi-database not supported in postgress");

                return (from p in Database.View<PgClass>()
                        where p.relkind == RelKind.View
                        let ns = p.Namespace()
                        where !ns.IsInternal()
                        let definition = PostgresFunctions.pg_get_viewdef(p.oid)
                        select KeyValuePair.Create(new ObjectName(new SchemaName(db, ns.nspname, isPostgres), p.relname, isPostgres), definition)).ToList();
            }
            else
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    return (from v in Database.View<SysViews>()
                            join s in Database.View<SysSchemas>() on v.schema_id equals s.schema_id
                            join m in Database.View<SysSqlModules>() on v.object_id equals m.object_id
                            select KeyValuePair.Create(new ObjectName(new SchemaName(db, s.name, isPostgres), v.name, isPostgres), m.definition)).ToList();
                }
            }
        }).ToDictionary();

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(Spacing.Double,
                Views,
                oldView,
                createNew: (name, newView) => newView.CreateView(),
                removeOld: null,
                mergeBoth: (name, newDef, oldDef) => Clean(newDef.CreateView().Sql) == Clean(oldDef) ? null :
                drop ? newDef.DropView() : newDef.CreateView()
            ); ;
    }
    #endregion

    #region Procedures
    public Dictionary<ObjectName, Procedure> StoreProcedures = new Dictionary<ObjectName, Procedure>();
    public Procedure IncludeStoreProcedure(string procedureName, string procedureCodeAndArguments)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return IncludeStoreProcedure(new ObjectName(SchemaName.Default(isPostgres), procedureName, isPostgres), procedureCodeAndArguments);
    }

    public Procedure IncludeStoreProcedure(ObjectName procedureName, string procedureCodeAndArguments)
    {
        return StoreProcedures[procedureName] = new Procedure("PROCEDURE", procedureName, procedureCodeAndArguments);
    }

    public Procedure IncludeUserDefinedFunction(string functionName, string functionCodeAndArguments)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return IncludeUserDefinedFunction(new ObjectName(SchemaName.Default(isPostgres), functionName, isPostgres), functionCodeAndArguments);
    }

    public Procedure IncludeUserDefinedFunction(ObjectName functionName, string functionCodeAndArguments)
    {
        return StoreProcedures[functionName] = new Procedure("FUNCTION", functionName, functionCodeAndArguments);
    }

    SqlPreCommand? GenerateProcedures()
    {
        return StoreProcedures.Select(p => p.Value.CreateSql()).Combine(Spacing.Double);
    }

    SqlPreCommand? SyncProcedures(Replacements replacements, bool drop)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var oldProcedures = Schema.Current.DatabaseNames().SelectMany(db =>
        {
            if (isPostgres)
            {
                if (db != null)
                    throw new InvalidOperationException("Multi-database not supported in postgress");

                return (from v in Database.View<PgProc>()
                        let ns = v.Namespace()
                        where !ns.IsInternal()
                        let definition = PostgresFunctions.pg_get_viewdef(v.oid)
                        select KeyValuePair.Create(new ObjectName(new SchemaName(db, ns.nspname, isPostgres), v.proname, isPostgres), definition)).ToList();
            }
            else
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    return (from p in Database.View<SysObjects>()
                            join s in Database.View<SysSchemas>() on p.schema_id equals s.schema_id
                            where p.type == "P" || p.type == "IF" || p.type == "FN"
                            join m in Database.View<SysSqlModules>() on p.object_id equals m.object_id
                            select KeyValuePair.Create(new ObjectName(new SchemaName(db, s.name, isPostgres), p.name, isPostgres), m.definition)).ToList();
                }
            }
        }).ToDictionary();

        return Synchronizer.SynchronizeScript(
            Spacing.Double,
            StoreProcedures,
            oldProcedures,
            createNew: (name, newProc) => drop ? null : newProc.CreateSql(),
            removeOld: null,
            mergeBoth: (name, newProc, oldProc) => Clean(newProc.CreateSql().Sql) == Clean(oldProc) ? null :
            drop ? newProc.DropSql() : newProc.CreateSql()
            );
    }

    public class Procedure
    {
        public string ProcedureType;
        public ObjectName ProcedureName;
        public string ProcedureCodeAndArguments;

        public Procedure(string procedureType, ObjectName procedureName, string procedureCodeAndArguments)
        {
            ProcedureType = procedureType;
            ProcedureName = procedureName;
            ProcedureCodeAndArguments = procedureCodeAndArguments;
        }

        public SqlPreCommandSimple CreateSql()
        {
            return new SqlPreCommandSimple("CREATE {0} {1} ".FormatWith(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true, GoAfter = true };
        }

        public SqlPreCommandSimple AlterSql()
        {
            return new SqlPreCommandSimple("ALTER {0} {1} ".FormatWith(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true, GoAfter = true };
        }

        public SqlPreCommandSimple DropSql()
        {
            return new SqlPreCommandSimple("DROP {0} {1} ".FormatWith(ProcedureType, ProcedureName)) { GoBefore = true, GoAfter = true };
        }
    }
    #endregion
}
