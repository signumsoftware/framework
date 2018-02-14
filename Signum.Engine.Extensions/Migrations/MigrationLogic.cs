using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Migrations;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Basics;

namespace Signum.Engine.Migrations
{
    public static class MigrationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SqlMigrationEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.VersionNumber,
                    });

                sb.Include<CSharpMigrationEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.UniqueName,
                        e.ExecutionDate,
                    });

                sb.Include<LoadMethodLogEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Start,
                        e.Duration,
                        e.ClassName,
                        e.MethodName,
                        e.Description,
                    });
            }
        }

        public static void EnsureMigrationTable<T>() where T : Entity
        {
            using (Transaction tr = new Transaction())
            {
                if (Administrator.ExistsTable<T>())
                    return;

                var table = Schema.Current.Table<T>();

                if (!table.Name.Schema.IsDefault() && !Database.View<SysSchemas>().Any(s => s.name == table.Name.Schema.Name))
                    SqlBuilder.CreateSchema(table.Name.Schema).ExecuteLeaves();

                SqlBuilder.CreateTableSql(table).ExecuteNonQuery();

                foreach (var i in table.GeneratAllIndexes())
                {
                    SqlBuilder.CreateIndex(i).ExecuteLeaves();
                }

                SafeConsole.WriteLineColor(ConsoleColor.White, "Table " + table.Name + " auto-generated...");
         
                tr.Commit();
            }
        }

        public static Exception ExecuteLoadProcess(Action action, string description)
        {
            string showDescription = description ?? action.Method.Name.SpacePascal(true);
            Console.WriteLine("------- Executing {0} ".FormatWith(showDescription).PadRight(Console.WindowWidth - 2, '-'));

            var log = !Schema.Current.Tables.ContainsKey(typeof(LoadMethodLogEntity)) ? null : new LoadMethodLogEntity
            {
                Start = TimeZoneManager.Now,
                ClassName = action.Method.DeclaringType.FullName,
                MethodName = action.Method.Name,
                Description = description,
            };

            try
            {
                action();
                if (log != null)
                {
                    log.End = TimeZoneManager.Now;
                    log.Save();
                }
                Console.WriteLine("------- Executed {0} (took {1})".FormatWith(showDescription, (log.End.Value - log.Start).NiceToString()).PadRight(Console.WindowWidth - 2, '-'));

                return null;
            }
            catch (Exception e)
            {                
                Console.WriteLine();

                SafeConsole.WriteColor(ConsoleColor.Red, e.GetType() + ": ");
                SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.Message);
                SafeConsole.WriteSameLineColor(ConsoleColor.DarkRed, e.StackTrace);
                if (log != null)
                {
                    var exLog = e.LogException();
                    log.Exception = exLog.ToLite();
                    log.End = TimeZoneManager.Now;
                    log.Save();
                }

                return e;
            }
        }
    }

   
    [Serializable]
    public class MigrationException : Exception
    {
        public MigrationException() { }
        public MigrationException(string message) : base(message) { }
        public MigrationException(string message, Exception inner) : base(message, inner) { }
        protected MigrationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
