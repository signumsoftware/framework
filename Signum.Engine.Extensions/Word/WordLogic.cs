using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Word;
using Signum.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Word
{
    public static class WordReportLogic
    {
        public static ResetLazy<ConcurrentDictionary<Lite<WordReportTemplateEntity>, WordReportTemplateEntity>> Templates;

        public static ResetLazy<Dictionary<TypeEntity, List<Lite<WordReportTemplateEntity>>>> TemplatesByType; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<WordReportTemplateEntity>();

                dqm.RegisterQuery(typeof(WordReportTemplateEntity), ()=>
                    from e in Database.Query<WordReportTemplateEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        e.Query,
                        e.Template.Entity.FileName
                    });

                new Graph<WordReportTemplateEntity>.Execute(WordReportTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                TemplatesByType = sb.GlobalLazy(() => Database.Query<WordReportTemplateEntity>()
                    .Select(r => KVP.Create(r.Type, r.ToLite()))
                    .GroupToDictionary(a => a.Key, a => a.Value),
                    new InvalidateWith(typeof(WordReportTemplateEntity)));

                Templates = sb.GlobalLazy(() => new ConcurrentDictionary<Lite<WordReportTemplateEntity>, WordReportTemplateEntity>(), 
                    new InvalidateWith(typeof(WordReportTemplateEntity)));
            }
        }

        public static WordReportTemplateEntity GetTemplate(this Lite<WordReportTemplateEntity> report)
        {
            return Templates.Value.GetOrAdd(report, r =>
            {
                var template = r.Retrieve();
                template.Template.Retrieve();
                return template;
            });
        }

        public static byte[] GenerateTemplate(Lite<WordReportTemplateEntity> word, Lite<Entity> entity)
        {
            WordReportTemplateEntity template = GetTemplate(word);

            object queryName = template.Query.ToQueryName(); 

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            new WordTemplateParser(qd, null).ParseDocument(template.Template.Entity.BinaryFile);

            return null;
        }
    }
}
