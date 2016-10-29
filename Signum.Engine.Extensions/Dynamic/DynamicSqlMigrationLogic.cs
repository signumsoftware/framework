using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Migrations;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicSqlMigrationLogic
    {

        public static StringBuilder CurrentLog = null;
        public static string LastLog;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicSqlMigrationEntity>();

                dqm.RegisterQuery(typeof(DynamicSqlMigrationEntity), () =>
                    from e in Database.Query<DynamicSqlMigrationEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.CreationDate,
                        e.CreatedBy,
                        e.ExecutionDate,
                        e.ExecutedBy,
                        e.Comment,
                    });

                new Graph<DynamicSqlMigrationEntity>.Construct(DynamicSqlMigrationOperation.Create)
                {
                    Construct = args => new DynamicSqlMigrationEntity
                    {
                        CreationDate = TimeZoneManager.Now,
                        CreatedBy = UserEntity.Current.ToLite(),
                    }
                }.Register();

                new Graph<DynamicSqlMigrationEntity>.Execute(DynamicSqlMigrationOperation.Save)
                {
                    CanExecute = a=> a.ExecutionDate == null ? null : DynamicSqlMigrationMessage.TheMigrationIsAlreadyExecuted.NiceToString(),
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<DynamicSqlMigrationEntity>.Execute(DynamicSqlMigrationOperation.Execute)
                {
                    CanExecute = a => a.ExecutionDate == null ? null : DynamicSqlMigrationMessage.TheMigrationIsAlreadyExecuted.NiceToString(),
                    
                    Execute = (e, _) => {

                        if (CurrentLog != null)
                            throw new InvalidOperationException("There is already a migration running");

                        e.ExecutionDate = TimeZoneManager.Now;
                        e.ExecutedBy = UserEntity.Current.ToLite();

                        var oldOut = Console.Out;
                        try
                        {
                            CurrentLog = new StringBuilder();
                            LastLog = null;
                            Console.SetOut(new SyncronizedStringWriter(CurrentLog));

                            string title = e.CreationDate + (e.Comment.HasText() ? " ({0})".FormatWith(e.Comment) : null);

                            SqlMigrationRunner.ExecuteScript(title, e.Script);
                        }
                        finally
                        {
                            LastLog = CurrentLog?.ToString();
                            CurrentLog = null;
                            Console.SetOut(oldOut);
                        }
                    }
                }.Register();

                new Graph<DynamicSqlMigrationEntity>.Delete(DynamicSqlMigrationOperation.Delete)
                {
                    CanDelete = a => a.ExecutionDate == null ? null : DynamicSqlMigrationMessage.TheMigrationIsAlreadyExecuted.NiceToString(),
                    Delete = (e, _) => { e.Delete(); }
                }.Register();
            }
            
        }

        public static string GetLog()
        {
            var ll = LastLog;
            var sb = CurrentLog;
            if (ll != null)
                return ll;

            if (sb != null)
            {
                lock (sb)
                    return sb.ToString();
            }

            return null;
        }

        internal class SyncronizedStringWriter : TextWriter
        {
            private StringBuilder stringBuilder;

            public SyncronizedStringWriter(StringBuilder currentLog)
            {
                this.stringBuilder = currentLog;
            }

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value)
            {
                lock (stringBuilder)
                    base.Write(value);
            }

            public override void WriteLine()
            {
                lock (stringBuilder)
                    base.WriteLine();
            }

            public override void Write(string value)
            {
                lock (stringBuilder)
                    base.Write(value);
            }
        }



    }
}
