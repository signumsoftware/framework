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
                sb.Include<DynamicRenameEntity>()
                      .WithQuery(dqm, e => new
                      {
                          Entity = e,
                          e.Id,
                          e.CreationDate,
                          e.ReplacementKey,
                          e.OldName,
                          e.NewName,
                      });

                sb.Include<DynamicSqlMigrationEntity>()
                    .WithQuery(dqm, e => new
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
                    Construct = args => 
                    {
                        var old = Replacements.AutoReplacement;

                        var maxExecutionDate = Database.Query<DynamicSqlMigrationEntity>().Max(a => a.ExecutionDate);

                        var lastRenames = Database.Query<DynamicRenameEntity>()
                        .Where(a => maxExecutionDate == null || a.CreationDate > maxExecutionDate)
                        .OrderBy(a => a.CreationDate)
                        .ToList();

                        try
                        {
                            if (Replacements.AutoReplacement == null)
                                Replacements.AutoReplacement = ctx =>
                                {
                                    if (ctx.ReplacementKey.Contains(":"))
                                        return DynamicAutoReplacementsComplex(ctx, lastRenames);
                                    else
                                        return DynamicAutoReplacementsSimple(ctx, lastRenames);

                                };

                            var script = Schema.Current.SynchronizationScript(interactive: false, replaceDatabaseName: SqlMigrationRunner.DatabaseNameReplacement);

                            return new DynamicSqlMigrationEntity
                            {
                                CreationDate = TimeZoneManager.Now,
                                CreatedBy = UserEntity.Current.ToLite(),
                                Script = script?.ToString(),
                            };
                        }
                        finally
                        {
                            Replacements.AutoReplacement = old;
                        }
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

        public static void AddDynamicRename(string replacementKey, string oldName, string newName)
        {
            new DynamicRenameEntity { ReplacementKey = replacementKey, OldName = oldName, NewName = newName }.Save();
        }

        public static Replacements.Selection? DynamicAutoReplacementsSimple(Replacements.AutoReplacementContext ctx, List<DynamicRenameEntity> lastRenames)
        {
            var list = lastRenames.Where(a => a.ReplacementKey == ctx.ReplacementKey).ToList();

            var currentName = ctx.OldValue.TryAfterLast(".") ?? ctx.OldValue;
            foreach (var r in list)
            {
                if (r.OldName == currentName)
                    currentName = r.NewName;
            }

            if (ctx.NewValues.Contains(currentName))
                return new Replacements.Selection(ctx.OldValue, currentName);

            var best = ctx.NewValues.Where(a => a.TryAfterLast(".") == currentName).Only();
            if(best != null)
                return new Replacements.Selection(ctx.OldValue, currentName);

            return new Replacements.Selection(ctx.OldValue, null);
        }

        public static Replacements.Selection? DynamicAutoReplacementsComplex(Replacements.AutoReplacementContext ctx, List<DynamicRenameEntity> lastRenames)
        {
            var prefix = ctx.ReplacementKey.Before(":");
            var tableNames = GetTableRenames(ctx.ReplacementKey.After(":"), lastRenames);

            var list = lastRenames.Where(a => tableNames.Any(tn => a.ReplacementKey == prefix + ":" + tn)).ToList();

            var currentName = ctx.OldValue;
            foreach (var r in list)
            {
                if (r.OldName == currentName)
                    currentName = r.NewName;
            }

            if (ctx.NewValues.Contains(currentName))
                return new Replacements.Selection(ctx.OldValue, currentName);

            return new Replacements.Selection(ctx.OldValue, null);
        }

        private static List<string> GetTableRenames(string lastTableName, List<DynamicRenameEntity> lastRenames)
        {
            var currentTableName = lastTableName;
            var tableRenames = lastRenames.Where(a => a.ReplacementKey == Replacements.KeyTables);

            List<string> allTableNames = new List<string> { currentTableName };
            foreach (var item in tableRenames.Reverse())
            {
                if (item.OldName == currentTableName)
                {
                    currentTableName = item.NewName;
                    allTableNames.Add(currentTableName);
                }
            }

            return allTableNames;
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
