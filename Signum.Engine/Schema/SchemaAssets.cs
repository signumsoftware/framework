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
            SqlPreCommand defaultConstraints = CreateDefaultConstraints();

            return SqlPreCommand.Combine(Spacing.Triple, views, procedures, defaultConstraints);
        }

        internal SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            SqlPreCommand views = SyncViews(replacements);
            SqlPreCommand procedures = SyncProcedures(replacements);
            SqlPreCommand defaultConstraints = SyncDefaultConstraints(replacements);

            return SqlPreCommand.Combine(Spacing.Triple, views, procedures, defaultConstraints);
        }

        static string Clean(string command)
        {
            return command.Trim(' ', '\r', '\n', ';');
        }

        #region Views
        public class View
        {
            public ObjectName Name;
            public string Definition;

            public SqlPreCommandSimple CreateView()
            {
                return new SqlPreCommandSimple("CREATE VIEW {0} AS ".Formato(Name) + Definition) { GoBefore = true };
            }

            public SqlPreCommandSimple AlterView()
            {
                return new SqlPreCommandSimple("ALTER VIEW {0} AS ".Formato(Name) + Definition) { GoBefore = true };
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
                using (Administrator.OverrideDatabaseInViews(db))
                {
                    return (from v in Database.View<SysViews>()
                            join s in Database.View<SysSchemas>() on v.schema_id equals s.schema_id
                            join m in Database.View<SysSqlModules>() on v.object_id equals m.object_id
                            select KVP.Create(new ObjectName(new SchemaName(db, s.name), v.name), m.definition)).ToList();
                }
            }).ToDictionary();

            return Synchronizer.SynchronizeScript(
                Views,
                oldView,
                (name, newView) => newView.CreateView(),
                null,
                (name, newDef, oldDef) =>
                    Clean(newDef.CreateView().Sql) == Clean(oldDef) ? null : newDef.AlterView(),
                Spacing.Double);
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
                using (Administrator.OverrideDatabaseInViews(db))
                {
                    return (from p in Database.View<SysObjects>()
                            join s in Database.View<SysSchemas>() on p.schema_id equals s.schema_id
                            where p.type == "P" || p.type == "IF" || p.type == "FN"
                            join m in Database.View<SysSqlModules>() on p.object_id equals m.object_id
                            select KVP.Create(new ObjectName(new SchemaName(db, s.name), p.name), m.definition)).ToList();
                }
            }).ToDictionary();

            return Synchronizer.SynchronizeScript(
                StoreProcedures,
                oldProcedures,
                (name, newProc) => newProc.CreateSql(),
                null,
                (name, newProc, oldProc) =>
                    Clean(newProc.CreateSql().Sql) == Clean(oldProc) ? null : newProc.AlterSql(),
                Spacing.Double);
        }

        public class Procedure
        {
            public string ProcedureType;
            public ObjectName ProcedureName;
            public string ProcedureCodeAndArguments;

            public SqlPreCommandSimple CreateSql()
            {
                return new SqlPreCommandSimple("CREATE {0} {1} ".Formato(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true };
            }

            public SqlPreCommandSimple AlterSql()
            {
                return new SqlPreCommandSimple("ALTER {0} {1} ".Formato(ProcedureType, ProcedureName) + ProcedureCodeAndArguments) { GoBefore = true };
            }
        }
        #endregion

        #region DefaultConstraints
        public class DefaultConstaint
        {
            public string ConstraintName;
            public string DefaultExpression;
        }

        public Dictionary<ObjectName, Dictionary<IColumn, DefaultConstaint>> DefaultContraints = new Dictionary<ObjectName, Dictionary<IColumn, DefaultConstaint>>();
        public void IncludeDefaultConstraint(ITable table, IColumn column, string constraintName, string defaultExpression)
        {
            DefaultContraints.GetOrCreate(table.Name)[column] = new DefaultConstaint
            {
                ConstraintName = constraintName,
                DefaultExpression = defaultExpression
            };
        }

        public void IncludeDefaultConstraint<T>(Expression<Func<T, object>> lambdaToField, string defaultExpression) where T : IdentifiableEntity
        {
            Table table = Schema.Current.Table<T>();

            var column = (IColumn)Schema.Current.Field(lambdaToField);

            IncludeDefaultConstraint(table, column, "DF_{0}_{1}".Formato(table.Name, column.Name), defaultExpression);
        }

        SqlPreCommand CreateDefaultConstraints()
        {
            return (from t in DefaultContraints
                    from c in t.Value
                    select new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} DEFAULT {2} FOR {3}"
                         .Formato(t.Key.Name, c.Value.ConstraintName.SqlEscape(), c.Value.DefaultExpression, c.Key.Name.SqlEscape())))
                        .Combine(Spacing.Double);
        }

        SqlPreCommand SyncDefaultConstraints(Replacements replacements)
        {
            var dbDefaultConstraints = Schema.Current.DatabaseNames().SelectMany(db =>
                {
                    using (Administrator.OverrideDatabaseInViews(db))
                    {
                        return (from t in Database.View<SysTables>()
                                join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                                join c in Database.View<SysColumns>() on t.object_id equals c.object_id
                                join ctr in Database.View<SysDefaultConstraints>() on c.default_object_id equals ctr.object_id
                                where !ctr.is_system_named
                                select new
                                {
                                    table = new ObjectName(new SchemaName(db, s.name), t.name),
                                    column = c.name,
                                    constraint = ctr.name,
                                    definition = ctr.definition,
                                }).ToList();
                    }
                }).AgGroupToDictionary(a => a.table,
                gr => gr.ToDictionary(a => a.column, a => new { constraintName = a.constraint, a.definition }));

            return Synchronizer.SynchronizeScript(
                DefaultContraints,
                dbDefaultConstraints,
                (tn, newDic) => newDic.Select(kvp => SqlBuilder.AlterTableAddDefaultConstraint(tn, kvp.Key.Name, kvp.Value.ConstraintName, kvp.Value.DefaultExpression)).Combine(Spacing.Simple),
                (tn, oldDic) => oldDic.Select(kvp => SqlBuilder.AlterTableDropConstraint(tn, kvp.Value.constraintName)).Combine(Spacing.Simple),
                (tn, newDic, oldDic) =>
                    Synchronizer.SynchronizeScript(
                    newDic.SelectDictionary(c => c.Name, dc => dc),
                    oldDic,
                    (cn, newDC) => SqlBuilder.AlterTableAddDefaultConstraint(tn, cn, newDC.ConstraintName, newDC.DefaultExpression),
                    (cn, oldDC) => SqlBuilder.AlterTableDropConstraint(tn, oldDC.constraintName),
                    (cn, newDC, oldDC) => Clean("(" + newDC.DefaultExpression.ToLower() + ")") == Clean(oldDC.definition) ? null :
                        SqlPreCommand.Combine(Spacing.Simple,
                        SqlBuilder.AlterTableDropConstraint(tn, oldDC.constraintName),
                        SqlBuilder.AlterTableAddDefaultConstraint(tn, cn, newDC.ConstraintName, newDC.DefaultExpression)), Spacing.Simple),
                        Spacing.Double);
        } 
        #endregion
    }
}
