using Signum.Engine.Sync;
using Signum.Engine.Sync.Postgres;
using Signum.Engine.Sync.SqlServer;
using System.IO;

namespace Signum.Engine.Maps;

public class SchemaAssets
{
    internal SqlPreCommand? Schema_GeneratingBeforeTables()
    {
        SqlPreCommand? procedures = GenerateProcedures(beforeTables: true);

        return procedures;
    }

    internal SqlPreCommand? Schema_Generating()
    {
        SqlPreCommand? views = GenerateViews();
        SqlPreCommand? procedures = GenerateProcedures(beforeTables: false);

        return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
    }

    internal SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        SqlPreCommand? views = SyncViews(replacements, beforeTables: false);
        SqlPreCommand? procedures = SyncProcedures(replacements, beforeTables: false);

        return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
    }

    internal SqlPreCommand? Schema_SynchronizingBeforeTables(Replacements replacements)
    {
        return SyncProcedures(replacements, beforeTables: true);
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

    SqlPreCommand? SyncViews(Replacements replacements, bool beforeTables)
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
                createNew: (name, newView) => beforeTables ? null : newView.CreateView(),
                removeOld: null,
                mergeBoth: (name, newDef, oldDef) => Clean(newDef.CreateView().Sql) == Clean(oldDef) ? null :
                beforeTables ? newDef.DropView() : newDef.CreateView()
            ); ;
    }
    #endregion

    #region Procedures
    public Dictionary<ObjectName, Procedure> StoreProcedures = new Dictionary<ObjectName, Procedure>();
    public Procedure IncludeStoreProcedure(string procedureName, string procedureCodeAndArguments, bool beforeTables = false)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return IncludeStoreProcedure(new ObjectName(SchemaName.Default(isPostgres), procedureName, isPostgres), procedureCodeAndArguments, beforeTables);
    }

    public Procedure IncludeStoreProcedure(ObjectName procedureName, string procedureCodeAndArguments, bool beforeTables = false)
    {
        return StoreProcedures[procedureName] = new Procedure("PROCEDURE", procedureName, procedureCodeAndArguments) { BeforeTables = beforeTables};
    }

    public Procedure IncludeUserDefinedFunction(string functionName, string functionCodeAndArguments, bool beforeTables = false)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        return IncludeUserDefinedFunction(new ObjectName(SchemaName.Default(isPostgres), functionName, isPostgres), functionCodeAndArguments, beforeTables);
    }

    public Procedure IncludeUserDefinedFunction(ObjectName functionName, string functionCodeAndArguments, bool beforeTables = false)
    {
        return StoreProcedures[functionName] = new Procedure("FUNCTION", functionName, functionCodeAndArguments) { BeforeTables = true};
    }

    SqlPreCommand? GenerateProcedures(bool beforeTables)
    {
        return StoreProcedures.Where(a=>a.Value.BeforeTables == beforeTables).Select(p => p.Value.CreateSql()).Combine(Spacing.Double);
    }

    SqlPreCommand? SyncProcedures(Replacements replacements, bool beforeTables)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var oldProcedures = Schema.Current.DatabaseNames().SelectMany(db =>
        {
            if (isPostgres)
            {
                if (db != null)
                    throw new InvalidOperationException("Multi-database not supported in postgress");

                return  (from v in Database.View<PgProc>()
                        let ns = v.Namespace()
                        where !ns.IsInternal()
                        where v.Extension() == null
                        let definition = PostgresFunctions.pg_get_functiondef(v.oid)
                        select KeyValuePair.Create(new ObjectName(new SchemaName(db, ns.nspname, isPostgres), v.proname, isPostgres), definition)).ToList();                
            }
            else
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    return (from p in Database.View<SysObjects>()
                            join s in Database.View<SysSchemas>() on p.schema_id equals s.schema_id
                            where new[] {"P", "IF", "FN", "TF" }.Contains(p.type)
                            join m in Database.View<SysSqlModules>() on p.object_id equals m.object_id
                            select KeyValuePair.Create(new ObjectName(new SchemaName(db, s.name, isPostgres), p.name, isPostgres), m.definition)).ToList();
                }
            }
        }).ToDictionaryEx();

        if (beforeTables)
        {
            return Synchronizer.SynchronizeScript(
               Spacing.Double,
               StoreProcedures,
               oldProcedures,
               createNew: (name, newProc) => newProc.BeforeTables ? newProc.CreateSql() : null,
               removeOld: null,
               mergeBoth: (name, newProc, oldProc) => Clean(newProc.ProcedureCodeAndArguments) == Clean("(" + oldProc.After("(")) ? null :
               new[]{
                   newProc.DropSql(),
                   newProc.BeforeTables ? newProc.CreateSql() : null,
               }.Combine(Spacing.Double));
        }
        else
        {
            return Synchronizer.SynchronizeScript(
               Spacing.Double,
               StoreProcedures,
               oldProcedures,
               createNew: (name, newProc) => !newProc.BeforeTables ? newProc.CreateSql() : null,
               removeOld: null,
               mergeBoth: (name, newProc, oldProc) => Clean(newProc.ProcedureCodeAndArguments) == Clean("(" + oldProc.After("(")) ? null :
               newProc.BeforeTables ? null :
               newProc.CreateSql()
           );
        }
    }

    internal void CreateInitialAssets(SchemaBuilder sb)
    {
        if (sb.IsPostgres && sb.Schema.GetDatabaseTables().Any(a => a.SystemVersioned != null))
        {
            var file = Schema.Current.Settings.PostresVersioningFunctionNoChecks ?
                "versioning_function_nochecks.sql" :
                "versioning_function.sql";

            var text = new StreamReader(typeof(Schema).Assembly.GetManifestResourceStream($"Signum.Engine.Sync.Postgres.{file}")!).Using(a => a.ReadToEnd());

            IncludeUserDefinedFunction("versioning", text.After("versioning"), beforeTables: true);
        }
    }

    public class Procedure
    {
        public string ProcedureType;
        public ObjectName ProcedureName;
        public string ProcedureCodeAndArguments;
        public bool BeforeTables;

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
