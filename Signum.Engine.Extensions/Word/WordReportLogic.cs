using DocumentFormat.OpenXml.Packaging;
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
        public static ResetLazy<Dictionary<Lite<WordTemplateEntity>, WordTemplateEntity>> WordTemplatesLazy;

        public static ResetLazy<Dictionary<TypeEntity, List<Lite<WordTemplateEntity>>>> TemplatesByType; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<WordTemplateEntity>();

                dqm.RegisterQuery(typeof(WordTemplateEntity), ()=>
                    from e in Database.Query<WordTemplateEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        e.Query,
                        e.Template.Entity.FileName
                    });

                new Graph<WordTemplateEntity>.Execute(WordTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                TemplatesByType = sb.GlobalLazy(() => Database.Query<WordTemplateEntity>()
                    .Select(r => KVP.Create(r.Type, r.ToLite()))
                    .GroupToDictionary(a => a.Key, a => a.Value),
                    new InvalidateWith(typeof(WordTemplateEntity)));

                WordTemplatesLazy = sb.GlobalLazy(() => Database.Query<WordTemplateEntity>()
                   .Where(et => et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                   .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(WordTemplateEntity)));
            }
        }

        private static WordTemplateEntity GetTemplate(Lite<WordTemplateEntity> word)
        {
            var result = WordTemplatesLazy.Value.GetOrThrow(word);
            result.Template.Retrieve();
            return result; 
        }

        public static byte[] CreateWordReport(this Lite<WordTemplateEntity> liteTemplate, Entity entity, ISystemWordTemplate systemWordTemplate = null)
        {
            WordTemplateEntity template = WordTemplatesLazy.Value.GetOrThrow(liteTemplate, "Word report template {0} not in cache".FormatWith(liteTemplate));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = template.Query.ToQueryName();

                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                 using (var memory = new MemoryStream())
                 {
                     memory.WriteAllBytes(template.Template.Entity.BinaryFile);

                     using (WordprocessingDocument document = WordprocessingDocument.Open(memory, true))
                     {
                         var parser = new WordTemplateParser(document, qd, systemWordTemplate.GetType());
                         parser.ParseDocument();
                         parser.CreateNodes();

                         var renderer = new WordTemplateRenderer(document, qd, entity, template.Culture.ToCultureInfo(), systemWordTemplate);
                         renderer.MakeQuery();
                         renderer.RenderNodes();
                     }

                     return memory.ToArray();
                 }
            }
        }
    }
}
