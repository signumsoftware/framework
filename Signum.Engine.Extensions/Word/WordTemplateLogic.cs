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
    public static class WordTemplateLogic
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
                        e.Query,
                        e.Template.Entity.FileName
                    });

                new Graph<WordTemplateEntity>.Execute(WordTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                TemplatesByType = sb.GlobalLazy(() =>
                {
                    var list = Database.Query<WordTemplateEntity>().Select(r => KVP.Create(r.Query.ToQueryName(), r.ToLite())).ToList();

                    return (from kvp in list
                            let imp = dqm.GetEntityImplementations(kvp.Key)
                            where !imp.IsByAll
                            from t in imp.Types
                            group kvp.Value by t into g
                            select KVP.Create(g.Key.ToTypeEntity(), g.ToList())).ToDictionary();

                }, new InvalidateWith(typeof(WordTemplateEntity)));

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

        public static string DumpFileFolder;

        public static byte[] CreateWordReport(this Lite<WordTemplateEntity> liteTemplate, Entity entity, ISystemWordTemplate systemWordTemplate = null)
        {
            WordTemplateEntity template = WordTemplatesLazy.Value.GetOrThrow(liteTemplate, "Word report template {0} not in cache".FormatWith(liteTemplate));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = template.Query.ToQueryName();

                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                 using (var memory = new MemoryStream())
                 {
                     memory.WriteAllBytes(template.Template.Retrieve().BinaryFile);

                     using (WordprocessingDocument document = WordprocessingDocument.Open(memory, true))
                     {
                         Dump(document, "0.Original.txt");

                         var parser = new WordTemplateParser(document, qd, systemWordTemplate.Try(swt => swt.GetType()));
                         parser.ParseDocument(); Dump(document, "1.Match.txt");
                         parser.CreateNodes(); Dump(document, "2.BaseNode.txt");
                         parser.AssertClean();

                         var renderer = new WordTemplateRenderer(document, qd, entity, template.Culture.ToCultureInfo(), systemWordTemplate);
                         renderer.MakeQuery();
                         renderer.RenderNodes(); Dump(document, "3.Replaced.txt");
                         renderer.AssertClean();
                     }

                     return memory.ToArray();
                 }
            }
        }

        private static void Dump(WordprocessingDocument document, string fileName)
        {
            if (DumpFileFolder == null)
                return;

            string fullFileName = Path.Combine(DumpFileFolder, fileName);

            File.WriteAllText(fullFileName, document.MainDocumentPart.Document.NiceToString());
        }
    }
}
