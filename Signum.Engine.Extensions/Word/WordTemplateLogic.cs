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
using Signum.Engine.UserAssets;
using Signum.Engine.Templating;

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
                    var list = Database.Query<WordTemplateEntity>().Select(r => KVP.Create(r.Query, r.ToLite())).ToList();

                    return (from kvp in list
                            let imp = dqm.GetEntityImplementations(kvp.Key.ToQueryName())
                            where !imp.IsByAll
                            from t in imp.Types
                            group kvp.Value by t into g
                            select KVP.Create(g.Key.ToTypeEntity(), g.ToList())).ToDictionary();

                }, new InvalidateWith(typeof(WordTemplateEntity)));

                WordTemplatesLazy = sb.GlobalLazy(() => Database.Query<WordTemplateEntity>()
                   .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(WordTemplateEntity)));

                Validator.PropertyValidator((WordTemplateEntity e) => e.Template).StaticPropertyValidation += ValidateTemplate;
            }
        }

        static string ValidateTemplate(WordTemplateEntity template, PropertyInfo pi)
        {
            if (template.Template == null)
                return null;

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(template.Query.ToQueryName());

                using (var memory = new MemoryStream())
                {
                    memory.WriteAllBytes(template.Template.Retrieve().BinaryFile);

                    using (WordprocessingDocument document = WordprocessingDocument.Open(memory, true))
                    {
                        Dump(document, "0.Original.txt");

                        var parser = new WordTemplateParser(document, qd, template.SystemWordTemplate.ToType());
                        parser.ParseDocument(); Dump(document, "1.Match.txt");
                        parser.CreateNodes(); Dump(document, "2.BaseNode.txt");
                        parser.AssertClean();

                        if (parser.Errors.IsEmpty())
                            return null;

                        return parser.Errors.ToString(e => e.Message, "\r\n");
                    }
                }
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

            if (systemWordTemplate != null && template.SystemWordTemplate.FullClassName != systemWordTemplate.GetType().FullName)
                throw new ArgumentException("systemWordTemplate should be a {0} instead of {1}".FormatWith(template.SystemWordTemplate.FullClassName, systemWordTemplate.GetType().FullName));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(template.Query.ToQueryName());

                 using (var memory = new MemoryStream())
                 {
                     memory.WriteAllBytes(template.Template.Retrieve().BinaryFile);

                     using (WordprocessingDocument document = WordprocessingDocument.Open(memory, true))
                     {
                         Dump(document, "0.Original.txt");

                         var parser = new WordTemplateParser(document, qd, template.SystemWordTemplate.ToType());
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

        static SqlPreCommand Schema_Synchronize_Tokens(Replacements replacements)
        {
            StringDistance sd = new StringDistance();

            var emailTemplates = Database.Query<WordTemplateEntity>().ToList();

            var table = Schema.Current.Table(typeof(WordTemplateEntity));

            SqlPreCommand cmd = emailTemplates.Select(uq => SynchronizeWordTemplate(replacements, table, uq, sd)).Combine(Spacing.Double);

            return cmd;
        }

        internal static SqlPreCommand SynchronizeWordTemplate(Replacements replacements, Table table, WordTemplateEntity template, StringDistance sd)
        {
            try
            {
                var queryName = QueryLogic.ToQueryName(template.Query.Key);

                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "WordTemplate: " + template.Name);
                Console.WriteLine(" Query: " + template.Query.Key);

             
                SyncronizationContext sc = new SyncronizationContext
                {
                    ModelType = template.SystemWordTemplate.ToType(),
                    QueryDescription = qd,
                    Replacements = replacements,
                    StringDistance = sd,
                    IsClean = false,
                };

                try
                {
                    using (var memory = new MemoryStream())
                    {
                        memory.WriteAllBytes(template.Template.Retrieve().BinaryFile);

                        using (WordprocessingDocument document = WordprocessingDocument.Open(memory, true))
                        {
                            Dump(document, "0.Original.txt");

                            var parser = new WordTemplateParser(document, qd, template.SystemWordTemplate.ToType());
                            parser.ParseDocument(); Dump(document, "1.Match.txt");
                            parser.CreateNodes(); Dump(document, "2.BaseNode.txt");
                            parser.AssertClean();


                            foreach (var node in document.MainDocumentPart.Document.Descendants<BaseNode>())
	                        {
                                node.Synchronize(sc);
	                        }

                        }
                    }

                    return table.UpdateSqlSync(template, includeCollections: true);
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return table.DeleteSqlSync(template);

                    throw new InvalidOperationException("Unexcpected {0}".FormatWith(ex.Result));
                }
                finally
                {
                    Console.Clear();
                }
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".FormatWith(template.BaseToString(), e.Message));
            }
        }
    }
}
