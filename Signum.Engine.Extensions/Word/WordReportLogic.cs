using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Files;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Entities.Word;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Word
{
    public static class WordReportLogic
    {
        static Expression<Func<WordTemplateEntity, IQueryable<WordReportLogEntity>>> GeneratedReportsExpression =
            e => Database.Query<WordReportLogEntity>().Where(a => a.Template.RefersTo(e));
        public static IQueryable<WordReportLogEntity> GeneratedReports(this WordTemplateEntity e)
        {
            return GeneratedReportsExpression.Evaluate(e);
        }

        static Expression<Func<Entity, IQueryable<WordReportLogEntity>>> WordReportLogsExpression =
            e => Database.Query<WordReportLogEntity>().Where(a => a.Target.RefersTo(e));
        public static IQueryable<WordReportLogEntity> WordReportLogs(this Entity e)
        {
            return WordReportLogsExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                WordTemplateLogic.Start(sb, dqm);

                sb.Include<WordReportLogEntity>();

                dqm.RegisterQuery(typeof(WordReportLogEntity), () =>
                    from e in Database.Query<WordReportLogEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Template,
                        e.Target,
                        e.User,
                        e.Start,
                        e.Exception,
                    });

                FilePathLogic.Register(WordReportFileType.DefaultWordReport, new FileTypeAlgorithm { CalculateSufix = FileTypeAlgorithm.Year_GuidExtension_Sufix });

                dqm.RegisterExpression((WordTemplateEntity e) => e.GeneratedReports());
                dqm.RegisterExpression((Entity e) => e.WordReportLogs());

                new Graph<WordReportLogEntity>.ConstructFrom<Entity>(WordReportLogOperation.CreateWordReportFromEntity)
                {
                    Construct = (entity, args) => CreateReport(args.GetArg<Lite<WordTemplateEntity>>(), entity.ToLiteFat())
                }.Register();


                new Graph<WordReportLogEntity>.ConstructFrom<WordTemplateEntity>(WordReportLogOperation.CreateWordReportFromTemplate)
                {
                    Construct = (template, args) => CreateReport(template.ToLite(), args.GetArg<Lite<Entity>>())
                }.Register();
            }
        }

        private static WordReportLogEntity CreateReport(Lite<WordTemplateEntity> template, Lite<Entity> entity, ISystemWordTemplate systemTemplate = null)
        {
            WordReportLogEntity log = new WordReportLogEntity
            {
                Template = template,
                Start = TimeZoneManager.Now,
                User = UserHolder.Current.ToLite(),
                Target = entity,
            };

            try
            {
                using (Transaction tr = new Transaction())
                {
                    var templateEntity = WordTemplateLogic.WordTemplatesLazy.Value[template];

                    var bytes = template.CreateWordReport(entity.RetrieveAndForget(), systemTemplate);

                    FilePathEntity file = new FilePathEntity(templateEntity.FileType , "report.docx", bytes);

                    log.Target = entity;
                    log.End = TimeZoneManager.Now;

                    using (ExecutionMode.Global())
                        log.Save();

                    return tr.Commit(log);
                }
            }
            catch (Exception ex)
            {
                ex.Data["entity"] = entity;
                ex.Data["template"] = template;

                if (Transaction.InTestTransaction)
                    throw;

                var exLog = ex.LogException();

                using (Transaction tr2 = Transaction.ForceNew())
                {
                    log.Target = entity.IsNew ? null : entity;
                    log.Exception = exLog.ToLite();

                    using (ExecutionMode.Global())
                        log.Save();

                    tr2.Commit();
                }

                throw;
            }
        }
    }
}