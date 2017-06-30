using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Engine.SchemaInfoTables;
using System.Linq.Expressions;
using Signum.Entities;

namespace Signum.Engine.Maps
{
    public class SchemaAssets
    {
        internal SqlPreCommand Schema_Generating()
        {
            SqlPreCommand views = GenerateViews();
            SqlPreCommand procedures = GenerateProcedures();

            return SqlPreCommand.Combine(Spacing.Triple, views, procedures);
        }

        internal SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            SqlPreCommand views = SyncViews(replacements);
            SqlPreCommand procedures = SyncProcedures(replacements);

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

            public SqlPreCommandSimple CreateView()
            {
                return new SqlPreCommandSimple("CREATE VIEW {0} ".FormatWith(Name) + Definition) { GoBefore = true, GoAfter = true };
            }

            public SqlPreCommandSimple AlterView()
            {
                return new SqlPreCommandSimple("ALTER VIEW {0} ".FormatWith(Name) + Definition) { GoBefore = true, GoAfter = true };
            } 
        }

        public View IncludeView(string viewName, string viewDefinition)
        {
            return IncludeView(new ObjectName(SchemaName.Default, viewName), viewDefinition);
        }

        public Dictionary<ObjectName, View> Views = new Dictionary<ObjectName, View>();
        public View IncludeView(ObjectName viewName, string viewDefinition)
        {
            return Views[viewName] = new View
            {
                Name = viewName,
                Definition = viewDefinition
            };
        }

        SqlPreCommand GenerateViews()
        {
            return Views.Values.Select(v => v.CreateView()).Combine(Spacing.Double);
        }

        SqlPreCommand SyncViews(Replacements replacements)
        {
            var oldView = Schema.Current.DatabaseNames().SelectMany(db =>
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    return (from v in Database.View<SysViews>()
                            join s in Database.View<SysSchemas>() on v.schema_id equals s.schema_id
                            join m in Database.View<SysSqlModules>() on v.object_id equals m.object_id
                            select KVP.Create(new ObjectName(new SchemaName(db, s.name), v.name), m.definition)).ToList();
                }
            }).ToDictionary();

            using (replacements.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScript(Spacing.Double,
                    Views,
                    oldView,
                    createNew: (name, newView) => newView.CreateView(),
                    removeOld: null,
                    mergeBoth: (name, newDef, oldDef) => Clean(newDef.CreateView().Sql) == Clean(oldDef) ? null : newDef.AlterView()
                );
        }
        #endregion

        #region Procedures
        public Dictionary<ObjectName, Procedure> StoreProcedures = new Dictionary<ObjectName, Procedure>();
        public Procedure IncludeStoreProcedure(string procedureName, string procedureCodeAndArguments)
        {
            return IncludeStoreProcedure(new ObjectName(SchemaName.Default, procedureName), procedureCodeAndArguments);
        }

        public Procedure IncludeStoreProcedure(ObjectName procedureName, string procedureCodeAndArguments)
        {
            return StoreProcedures[procedureName] =  new Procedure
            {
                ProcedureName = procedureName,
                ProcedureCodeAndArguments = procedureCodeAndArguments,
                ProcedureType = "PROCEDURE"
            };
        }

        public Procedure IncludeUserDefinedFunction(string functionName, string functionCodeAndArguments)
        {
            return IncludeUserDefinedFunction(new ObjectName(SchemaName.Default, functionName), functionCodeAndArguments);
        }

        public Procedure IncludeUserDefinedFunction(ObjectName functionName, string functionCodeAndArguments)
        {
            return StoreProcedures[functionName] = new Procedure
            {
                ProcedureName = functionName,
                ProcedureCodeAndArguments = functionCodeAndArguments,
                ProcedureType = "FUNCTION"
            };
        }

        SqlPreCommand GenerateProcedures()
        {
            return StoreProcedures.Select(p => p.Value.CreateSql()).Combine(Spacing.Double);
        }

        SqlPreCommand SyncProcedures(Replacements replacements)
        {
            var oldProcedures = Schema.Current.DatabaseNames().SelectMany(db =>
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    return (from p in Database.View<SysObjects>()
                            join s in Database.View<SysSchemas>() on p.schema_id equals s.schema_id
                            where p.type == "P" || p.type == "IF" || p.type == "FN"
                            join m in Database.View<SysSqlModules>() on p.object_id equals m.object_id
                            select KVP.Create(new ObjectName(new SchemaName(db, s.name), p.name), m.definition)).ToList();
                }
            }).ToDictionary();

            return Synchronizer.SynchronizeScript(
                Spacing.Double,
                StoreProcedures,
                oldProcedures,
                createNew: (name, newProc) => newProc.CreateSql(),
                removeOld: null,
                mergeBoth: (name, newProc, oldProc) => Clean(newProc.CreateSql().Sql) == Clean(oldProc) ? null : newProc.AlterSql()
                );
        }

        public class Procedure
        {
            public string ProcedureType;
            public ObjectName ProcedureName;
            public string ProcedureCodeAndArguments;

            public SqlPreCommandSimple CreateSql()
            {
                return new SqlPreCommandSimple("CREATE {0} {1} ".FormatWith(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true, GoAfter = true };
            }

            public SqlPreCommandSimple AlterSql()
            {
                return new SqlPreCommandSimple("ALTER {0} {1} ".FormatWith(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true, GoAfter = true };
            }
        }
        #endregion
    }
}
