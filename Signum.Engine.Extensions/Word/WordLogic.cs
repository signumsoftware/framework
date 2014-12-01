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
        public static ResetLazy<ConcurrentDictionary<Lite<WordReportTemplateDN>, WordReportTemplateDN>> Templates;

        public static ResetLazy<Dictionary<TypeDN, List<Lite<WordReportTemplateDN>>>> TemplatesByType; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<WordReportTemplateDN>();

                dqm.RegisterQuery(typeof(WordReportTemplateDN), ()=>
                    from e in Database.Query<WordReportTemplateDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        e.Query,
                        e.Template.Entity.FileName
                    });

                new Graph<WordReportTemplateDN>.Execute(WordReportOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                TemplatesByType = sb.GlobalLazy(() => Database.Query<WordReportTemplateDN>()
                    .Select(r => KVP.Create(r.Type, r.ToLite()))
                    .GroupToDictionary(a => a.Key, a => a.Value),
                    new InvalidateWith(typeof(WordReportTemplateDN)));

                Templates = sb.GlobalLazy(() => new ConcurrentDictionary<Lite<WordReportTemplateDN>, WordReportTemplateDN>(), 
                    new InvalidateWith(typeof(WordReportTemplateDN)));
            }
        }

        public static WordReportTemplateDN GetTemplate(this Lite<WordReportTemplateDN> report)
        {
            return Templates.Value.GetOrAdd(report, r =>
            {
                var template = r.Retrieve();
                template.Template.Retrieve();
                return template;
            });
        }

        public static byte[] GenerateTemplate(Lite<WordReportTemplateDN> word, Lite<Entity> entity)
        {
            WordReportTemplateDN template = GetTemplate(word);

            object queryName = template.Query.ToQueryName(); 

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            new WordTemplateParser(qd, null).ParseDocument(template.Template.Entity.BinaryFile);

            return null;
        }
    }
}
