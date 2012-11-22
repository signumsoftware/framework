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
            public string Name;
            public string Definition;

            public SqlPreCommandSimple CreateView()
            {
                return new SqlPreCommandSimple("CREATE VIEW {0} AS ".Formato(Name.SqlScape()) + Definition + SqlPreCommand.GO);
            }

            public SqlPreCommandSimple AlterView()
            {
                return new SqlPreCommandSimple("ALTER VIEW {0} AS ".Formato(Name.SqlScape()) + Definition + SqlPreCommand.GO);
            } 
        }


        public Dictionary<string, View> Views = new Dictionary<string, View>();
        public void IncludeView(string viewName, string viewDefinition)
        {
            Views[viewName] = new View { Name = viewName, Definition = viewDefinition };
        }

        SqlPreCommand GenerateViews()
        {
            return Views.Values.Select(v => v.CreateView()).Combine(Spacing.Double);
        }

        SqlPreCommand SyncViews(Replacements replacements)
        {
            var oldView = (from v in Database.View<SysViews>()
                           join m in Database.View<SysSqlModules>() on v.object_id equals m.object_id
                           select KVP.Create(v.name, m.definition)).ToDictionary();

            return Synchronizer.SynchronizeScript(
                Views,
                oldView,
                (name, newView) => newView.CreateView(),
                null,
                (name, newDef, oldDef) =>
                    Clean(newDef.CreateView().Sql.RemoveRight(SqlPreCommand.GO.Length)) == Clean(oldDef) ? null : newDef.AlterView(),
                Spacing.Double);
        }
        #endregion

        #region Procedures
        public Dictionary<string, Procedure> StoreProcedures = new Dictionary<string, Procedure>();
        public void IncludeStoreProcedure(string procedureName, string procedureCodeAndArguments)
        {
            StoreProcedures[procedureName] = new Procedure
            {
                ProcedureName = procedureName,
                ProcedureCodeAndArguments = procedureCodeAndArguments,
                ProcedureType = "PROCEDURE"
            }; 
        }

        public void IncludeUserDefinedFunction(string functionName, string functionCodeAndArguments)
        {
            StoreProcedures[functionName] = new Procedure
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
            var oldProcedures = (from p in Database.View<SysObjects>()
                                 where p.type == "P" || p.type == "F"
                                 join m in Database.View<SysSqlModules>() on p.object_id equals m.object_id
                                 select new { p.name, p.type, m.definition }).ToDictionary(a => a.name);

            return Synchronizer.SynchronizeScript(
                StoreProcedures,
                oldProcedures,
                (name, newProc) => newProc.CreateSql(),
                null,
                (name, newProc, oldProc) =>
                    Clean(newProc.CreateSql().Sql.RemoveRight(SqlPreCommand.GO.Length)) == Clean(oldProc.definition) ? null : newProc.AlterSql(),
                Spacing.Double);
        }

        public class Procedure
        {
            public string ProcedureType;
            public string ProcedureName;
            public string ProcedureCodeAndArguments;

            public SqlPreCommandSimple CreateSql()
            {
                return new SqlPreCommandSimple("CREATE {0} {1} ".Formato(ProcedureType, ProcedureName.SqlScape()) + ProcedureCodeAndArguments + SqlPreCommand.GO);
            }

            public SqlPreCommandSimple AlterSql()
            {
                return new SqlPreCommandSimple("ALTER {0} {1} ".Formato(ProcedureType, ProcedureName.SqlScape()) + ProcedureCodeAndArguments + SqlPreCommand.GO);
            }
        }
        #endregion

        #region DefaultConstraints
        public class DefaultConstaint
        {
            public string ConstraintName;
            public string DefaultExpression;
        }

        public Dictionary<ITable, Dictionary<IColumn, DefaultConstaint>> DefaultContraints = new Dictionary<ITable, Dictionary<IColumn, DefaultConstaint>>();
        public void IncludeDefaultConstraint(ITable table, IColumn column, string constraintName, string defaultExpression)
        {
            DefaultContraints.GetOrCreate(table)[column] = new DefaultConstaint
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
                         .Formato(t.Key.Name.SqlScape(), c.Value.ConstraintName.SqlScape(), c.Value.DefaultExpression, c.Key.Name.SqlScape())))
                        .Combine(Spacing.Double);
        }

        SqlPreCommand SyncDefaultConstraints(Replacements replacements)
        {
            var oldConstraints = (from t in Database.View<SysTables>()
                                  join c in Database.View<SysColumns>() on t.object_id equals c.object_id
                                  join ctr in Database.View<SysDefaultConstraints>() on c.default_object_id equals ctr.object_id
                                  select new { table = t.name, column = c.name, constraint = ctr.name, definition = ctr.definition })
                                 .AgGroupToDictionary(a => a.table,
                                    gr => gr.ToDictionary(a => a.column, a => new { constraintName = a.constraint, a.definition }));

            return Synchronizer.SynchronizeScript(
                DefaultContraints.SelectDictionary(t => t.Name, dic => dic),
                oldConstraints,
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
