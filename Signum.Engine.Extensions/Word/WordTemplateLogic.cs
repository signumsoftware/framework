using DocumentFormat.OpenXml.Packaging;
using Signum.Entities.Word;
using System.IO;
using Signum.Engine.UserAssets;
using Signum.Engine.Templating;
using Signum.Entities.Files;
using Signum.Utilities.DataStructures;
using DocumentFormat.OpenXml;
using W = DocumentFormat.OpenXml.Wordprocessing;
using System.Data;
using System.Globalization;
using Signum.Entities.Reflection;
using Signum.Entities.Templating;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Workflow;
using Signum.Entities.Mailing;
using Signum.Entities.UserAssets;

namespace Signum.Engine.Word;


public interface IWordDataTableProvider
{
    string? Validate(string suffix, WordTemplateEntity template);
    DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext context);
}

public static class WordTemplateLogic
{
    public static bool AvoidSynchronize = false;

    public static ResetLazy<Dictionary<Lite<WordTemplateEntity>, WordTemplateEntity>> WordTemplatesLazy = null!;

    public static ResetLazy<Dictionary<object, List<WordTemplateEntity>>> TemplatesByQueryName = null!;
    public static ResetLazy<Dictionary<Type, List<WordTemplateEntity>>> TemplatesByEntityType = null!;

    public static Dictionary<WordTransformerSymbol, Action<WordContext, OpenXmlPackage>> Transformers = new Dictionary<WordTransformerSymbol, Action<WordContext, OpenXmlPackage>>();
    public static Dictionary<WordConverterSymbol, Func<WordContext, byte[], byte[]>> Converters = new Dictionary<WordConverterSymbol, Func<WordContext, byte[], byte[]>>();

    public static Dictionary<string, IWordDataTableProvider> ToDataTableProviders = new Dictionary<string, IWordDataTableProvider>();

    public static Func<Entity?, CultureInfo>? GetCultureInfo;

    [AutoExpressionField]
    public static IQueryable<WordTemplateEntity> WordTemplates(this WordModelEntity e) => 
        As.Expression(() => Database.Query<WordTemplateEntity>().Where(a => a.Model.Is(e)));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            
            sb.Include<WordTemplateEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.Query,
                    e.Culture,
                    e.Template!.Entity.FileName
                });

            new Graph<WordTemplateEntity>.Execute(WordTemplateOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (wt, _) => {
                    if (!wt.IsNew)
                    {
                        var oldFile = wt.InDB(t => t.Template);
                        if (oldFile != null && !wt.Template.Is(oldFile))
                            Transaction.PreRealCommit += dic => oldFile.Delete();
                    }
                },
            }.Register();
            
            new Graph<WordTemplateEntity>.Delete(WordTemplateOperation.Delete)
            {
                Delete = (e, _) => e.Delete(),
            }.Register();

            sb.Schema.EntityEvents<WordTemplateEntity>().Retrieved += WordTemplateLogic_Retrieved;

            UserAssetsImporter.Register<WordTemplateEntity>("WordTemplate", WordTemplateOperation.Save);
            PermissionAuthLogic.RegisterPermissions(WordTemplatePermission.GenerateReport);

            WordModelLogic.Start(sb);

            SymbolLogic<WordTransformerSymbol>.Start(sb, () => Transformers.Keys.ToHashSet());
            SymbolLogic<WordConverterSymbol>.Start(sb, () => Converters.Keys.ToHashSet());

            sb.Include<WordTransformerSymbol>()
                .WithQuery(() => f => new
                {
                    Entity = f,
                    f.Key
                });

            sb.Include<WordConverterSymbol>()
                .WithQuery(() => f => new
                {
                    Entity = f,
                    f.Key
                });


            sb.Schema.Table<WordModelEntity>().PreDeleteSqlSync += e =>
                Administrator.UnsafeDeletePreCommand(Database.Query<WordTemplateEntity>()
                    .Where(a => a.Model.Is(e)));

            ToDataTableProviders.Add("Model", new ModelDataTableProvider());
            ToDataTableProviders.Add("UserQuery", new UserQueryDataTableProvider());
            ToDataTableProviders.Add("UserChart", new UserChartDataTableProvider());

            QueryLogic.Expressions.Register((WordModelEntity e) => e.WordTemplates(), () => typeof(WordTemplateEntity).NiceName());

            
            new Graph<WordTemplateEntity>.Execute(WordTemplateOperation.CreateWordReport)
            {
                CanExecute = et =>
                {
                    if (et.Model != null && WordModelLogic.RequiresExtraParameters(et.Model))
                        return WordTemplateMessage._01RequiresExtraParameters.NiceToString(typeof(WordModelEntity).NiceName(), et.Model);

                    return null;
                },
                Execute = (et, args) =>
                {
                    throw new InvalidOperationException("UI-only operation");
                }
            }.Register();

            WordTemplatesLazy = sb.GlobalLazy(() => Database.Query<WordTemplateEntity>()
               .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(WordTemplateEntity)));

            

            TemplatesByQueryName = sb.GlobalLazy(() =>
            {
                return WordTemplatesLazy.Value.Values.SelectCatch(w => KeyValuePair.Create(w.Query.ToQueryName(), w)).GroupToDictionary();
            }, new InvalidateWith(typeof(WordTemplateEntity)));

            TemplatesByEntityType = sb.GlobalLazy(() =>
            {
                return (from pair in WordTemplatesLazy.Value.Values.SelectCatch(wr => new { wr, imp = QueryLogic.Queries.GetEntityImplementations(wr.Query.ToQueryName()) })
                        where !pair.imp.IsByAll
                        from t in pair.imp.Types
                        select KeyValuePair.Create(t, pair.wr))
                        .GroupToDictionary();
            }, new InvalidateWith(typeof(WordTemplateEntity)));

            Schema.Current.Synchronizing += Schema_Synchronize_Tokens;

            Validator.PropertyValidator((WordTemplateEntity e) => e.Template).StaticPropertyValidation += ValidateTemplate;
            Validator.PropertyValidator((WordTemplateEntity e) => e.FileName).StaticPropertyValidation += ValidateFileName;
        }
    }

    private static void WordTemplateLogic_Retrieved(WordTemplateEntity template, PostRetrievingContext ctx)
    {
        ParseData(template);
    }

    public static WordTemplateEntity ParseData(this WordTemplateEntity template)
    {
        object? queryName = template.Query.ToQueryNameCatch();
        if (queryName != null)
        {
            QueryDescription description = QueryLogic.Queries.QueryDescription(queryName);

            template.ParseData(description);
        }

        return template;
    }

    public static Dictionary<Type, WordTemplateVisibleOn> VisibleOnDictionary = new Dictionary<Type, WordTemplateVisibleOn>()
    {
        { typeof(MultiEntityModel), WordTemplateVisibleOn.Single | WordTemplateVisibleOn.Multiple},
        { typeof(QueryModel), WordTemplateVisibleOn.Single | WordTemplateVisibleOn.Multiple| WordTemplateVisibleOn.Query},
    };

    public static bool IsVisible(WordTemplateEntity wt, WordTemplateVisibleOn visibleOn)
    {
        if (wt.Model == null)
            return visibleOn == WordTemplateVisibleOn.Single;

        if (WordModelLogic.HasDefaultTemplateConstructor(wt.Model))
            return false;

        var entityType = WordModelLogic.GetEntityType(wt.Model.ToType());

        if (entityType.IsEntity())
            return visibleOn == WordTemplateVisibleOn.Single;

        var should = VisibleOnDictionary.TryGet(entityType, WordTemplateVisibleOn.Single);

        return ((should & visibleOn) != 0);
    }

    public static List<Lite<WordTemplateEntity>> GetApplicableWordTemplates(object queryName, Entity? entity, WordTemplateVisibleOn visibleOn)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<WordTemplateEntity>(userInterface: false);
        return TemplatesByQueryName.Value.TryGetC(queryName).EmptyIfNull()
            .Where(a => isAllowed(a) && IsVisible(a, visibleOn))
            .Where(a => a.IsApplicable(entity))
            .Select(a => a.ToLite())
            .ToList();
    }

    public static void RegisterTransformer(WordTransformerSymbol transformerSymbol, Action<WordContext, OpenXmlPackage> transformer)
    {
        if (transformerSymbol == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(WordTransformerSymbol), nameof(transformerSymbol));

        Transformers.Add(transformerSymbol, transformer);
    }

    public class WordContext
    {
        public WordTemplateEntity Template;
        public Entity? Entity;
        public IWordModel? Model;

        public WordContext(WordTemplateEntity template, Entity? entity, IWordModel? model)
        {
            Template = template;
            Entity = entity;
            Model = model;
        }

        public Entity? GetEntity() => Entity ?? Model?.UntypedEntity as Entity;
    }

    public static void RegisterConverter(WordConverterSymbol converterSymbol, Func<WordContext, byte[], byte[]> converter)
    {
        if (converterSymbol == null)
            throw new ArgumentNullException(nameof(converterSymbol));

        Converters.Add(converterSymbol, converter);
    }

    static string? ValidateTemplate(WordTemplateEntity template, PropertyInfo pi)
    {
        if (template.Template == null)
            return null;

        using (template.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            QueryDescription qd = QueryLogic.Queries.QueryDescription(template.Query.ToQueryName());

            string? error = null;
            template.ProcessOpenXmlPackage(document =>
            {
                Dump(document, "0.Original.txt");

                var parser = new WordTemplateParser(document, qd, template.Model?.ToType(), template);
                parser.ParseDocument(); Dump(document, "1.Match.txt");
                parser.CreateNodes(); Dump(document, "2.BaseNode.txt");
                parser.AssertClean();

                error = parser.Errors.IsEmpty() ? null :
                    parser.Errors.ToString(e => e.Message, "\r\n");
            });

            return error;
        }
    }

    static string? ValidateFileName(WordTemplateEntity template, PropertyInfo pi)
    {
        if (template.FileName == null)
            return null;

        using (template.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            QueryDescription qd = QueryLogic.Queries.QueryDescription(template.Query.ToQueryName());

            TextTemplateParser.TryParse(template.FileName, qd, template.Model?.ToType(), out var errors);

            return errors.DefaultToNull();
        }
    }

    public class FileNameBox
    {
        public string? FileName { get; set; }
    }

    public static FileContent CreateReportFileContent(this Lite<WordTemplateEntity> liteTemplate, ModifiableEntity? modifiableEntity = null, IWordModel? model = null, bool avoidConversion = false)
    {
        var box = new FileNameBox { FileName = null  };
        var bytes = liteTemplate.GetFromCache().CreateReport(modifiableEntity, model, avoidConversion, box);
        return new FileContent(box.FileName!, bytes);
    }

    public static byte[] CreateReport(this Lite<WordTemplateEntity> liteTemplate, ModifiableEntity? modifiableEntity = null, IWordModel? model = null, bool avoidConversion = false, FileNameBox? fileNameBox = null)
    {
        return liteTemplate.GetFromCache().CreateReport(modifiableEntity, model, avoidConversion, fileNameBox);
    }

    public static WordTemplateEntity GetFromCache(this Lite<WordTemplateEntity> liteTemplate)
    {
        WordTemplateEntity template = WordTemplatesLazy.Value.GetOrThrow(liteTemplate, "Word report template {0} not in cache".FormatWith(liteTemplate));

        return template;
    }

    public static FileContent CreateReportFileContent(this WordTemplateEntity template, ModifiableEntity? modifiableEntity = null, IWordModel? model = null, bool avoidConversion = false)
    {
        var box = new FileNameBox { FileName = null };
        var bytes = template.CreateReport(modifiableEntity, model, avoidConversion, box);
        return new FileContent(box.FileName!, bytes);
    }

    public static string? DumpFileFolder = null;
    public static byte[] CreateReport(this WordTemplateEntity template, ModifiableEntity? modifiableEntity = null, IWordModel? model = null, bool avoidConversion = false, FileNameBox? fileNameBox = null)
    {
        using (HeavyProfiler.Log("CreateReport", () => $"{template.Name} {modifiableEntity?.ToString()} {model?.UntypedEntity.ToString()}"))
        {
            try
            {
                WordTemplatePermission.GenerateReport.AssertAuthorized();

                Entity? entity = null;
                if (template.Model != null)
                {
                    if (model == null)
                        model = WordModelLogic.CreateDefaultWordModel(template.Model, modifiableEntity);
                    else if (template.Model.ToType() != model.GetType())
                        throw new ArgumentException("model should be a {0} instead of {1}".FormatWith(template.Model.FullClassName, model.GetType().FullName));
                }
                else
                {
                    entity = modifiableEntity as Entity ?? throw new InvalidOperationException("Model should be an Entity");
                }

                using (template.DisableAuthorization ? ExecutionMode.Global() : null)
                using (CultureInfoUtils.ChangeBothCultures(template.Culture.ToCultureInfo()))
                {
                    QueryDescription qd = QueryLogic.Queries.QueryDescription(template.Query.ToQueryName());

                    using (var p = HeavyProfiler.Log("ProcessOpenXmlPackage"))
                    {
                        var array = template.ProcessOpenXmlPackage(document =>
                        {
                            Dump(document, "0.Original.txt");

                            var parser = new WordTemplateParser(document, qd, template.Model?.ToType(), template);
                            p.Switch("ParseDocument");
                            parser.ParseDocument(); Dump(document, "1.Match.txt");

                            p.Switch("CreateNodes");
                            parser.CreateNodes(); Dump(document, "2.BaseNode.txt");

                            p.Switch("AssertClean");
                            parser.AssertClean();

                            if (parser.Errors.Any())
                                throw new InvalidOperationException("Error in template {0}:\r\n".FormatWith(template) + parser.Errors.ToString(e => e.Message, "\r\n"));

                            var parsedFileName = fileNameBox != null ? TextTemplateParser.Parse(template.FileName, qd, template.Model?.ToType()) : null;

                            var renderer = new WordTemplateRenderer(document, qd, template.Culture.ToCultureInfo(), template, model, entity, parsedFileName);

                            p.Switch("MakeQuery");
                            renderer.ExecuteQuery();

                            p.Switch("RenderNodes");
                            renderer.RenderNodes(); Dump(document, "3.Replaced.txt");

                            p.Switch("AssertClean");
                            renderer.AssertClean();

                            p.Switch("FixDocument");
                            FixDocument(document); Dump(document, "4.Fixed.txt");

                            if (fileNameBox != null)
                            {
                                p.Switch("RenderFileName");
                                fileNameBox.FileName = renderer.RenderFileName();
                            }

                            if (template.WordTransformer != null)
                            {
                                p.Switch("WordTransformer");
                                Transformers.GetOrThrow(template.WordTransformer)(new WordContext(template, entity, model), document);
                            }
                        });

                        if (!avoidConversion && template.WordConverter != null)
                        {
                            p.Switch("WordConverter");
                            array = Converters.GetOrThrow(template.WordConverter)(new WordContext(template, entity, model), array);
                        }

                        return array;
                    }
                }
            }
            catch (Exception e)
            {
                e.Data["WordTemplate"] = template.ToLite();
                e.Data["ModifiableEntity"] = modifiableEntity;
                e.Data["Model"] = model;
                throw;
            }
        }
    }

    private static void FixDocument(OpenXmlPackage document)
    {
        foreach (var root in document.AllRootElements())
        {
            foreach (var cell in root.Descendants<W.TableCell>().ToList())
            {
                if (!cell.ChildElements.Any(c => !(c is W.TableCellProperties)))
                    cell.AppendChild(new W.Paragraph());
            }
        }
    }

    private static void Dump(OpenXmlPackage document, string fileName)
    {
        if (DumpFileFolder == null)
            return;

        if (!Directory.Exists(DumpFileFolder))
            Directory.CreateDirectory(DumpFileFolder);

        foreach (var part in AllParts(document).Where(p => p.RootElement != null))
        {
            string fullFileName = Path.Combine(DumpFileFolder, part.Uri.ToString().Replace("/", "_") + "." + fileName);

            File.WriteAllText(fullFileName, part.RootElement!.NiceToString());
        }
    }

    static SqlPreCommand? Schema_Synchronize_Tokens(Replacements replacements)
    {
        if (AvoidSynchronize)
            return null;

        StringDistance sd = new StringDistance();

        var wordTemplates = Database.Query<WordTemplateEntity>().ToList();
        var table = Schema.Current.Table(typeof(WordTemplateEntity));

        SqlPreCommand? cmd = wordTemplates.Select(wt => SynchronizeWordTemplateFile(replacements, wt, sd, table)).Combine(Spacing.Double);

        return cmd;
    }


    internal static SqlPreCommand? SynchronizeWordTemplateFile(Replacements replacements, WordTemplateEntity wt, StringDistance sd, Table table)
    {
        if (!replacements.Interactive)
            return null;

        Console.Write(".");

        try
        {
            var queryName = QueryLogic.ToQueryName(wt.Query.Key);

            QueryDescription qd = QueryLogic.Queries.QueryDescription(queryName);
            var file = wt.Template.RetrieveAndRemember();

            SqlPreCommand DeleteWorkTemplateAndFile()
            {
                return SqlPreCommand.Combine(Spacing.Simple,
                    Schema.Current.Table<WordTemplateEntity>().DeleteSqlSync(wt, wt => wt.Name == wt.Name),
                    Schema.Current.Table<FileEntity>().DeleteSqlSync(file, f => f.Hash == file.Hash)
                )!;
            }

            SqlPreCommand? RegenerateTemplateAndFile()
            {
                var newTemplate = wt.Model == null ? null : WordModelLogic.CreateDefaultTemplate(wt.Model);
                if (newTemplate == null)
                    return null;

                newTemplate.SetId(wt.IdOrNull);
                newTemplate.SetIsNew(false);
                newTemplate.Ticks = wt.Ticks;
                newTemplate.Template = wt.Template;

                using (file.AllowChanges())
                {
                    file.BinaryFile = newTemplate.Template.Entity.BinaryFile;
                    file.FileName = newTemplate.Template.Entity.FileName;

                    using (replacements?.WithReplacedDatabaseName())
                    {
                        return SqlPreCommand.Combine(Spacing.Simple,
                            Schema.Current.Table<WordTemplateEntity>().UpdateSqlSync(newTemplate, f => f.Name == wt.Name, comment: "WordTemplate File Regenerated: " + wt.Name),
                            Schema.Current.Table<FileEntity>().UpdateSqlSync(file, f => f.Hash == file.Hash, comment: "WordTemplate File Regenerated: " + wt.Name)
                        );
                    }

                }
            }

            SqlPreCommand? wordTemplateSync;
            using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "WordTemplate: " + wt.Name)))
            using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + wt.Query.Key)))
            {
                SqlPreCommand? fileSync;

                var oldHash = file.Hash;
                try
                {
                    var sc = new TemplateSynchronizationContext(replacements, sd, qd, wt.Model?.ToType());

                    var bytes = wt.ProcessOpenXmlPackage(document =>
                    {
                        Dump(document, "0.Original.txt");

                        var parser = new WordTemplateParser(document, qd, wt.Model?.ToType(), wt);
                        parser.ParseDocument(); Dump(document, "1.Match.txt");
                        parser.CreateNodes(); Dump(document, "2.BaseNode.txt");
                        parser.AssertClean();

                        foreach (var root in document.AllRootElements())
                        {
                            foreach (var node in root.Descendants<BaseNode>().ToList())
                            {
                                node.Synchronize(sc);
                            }
                        }

                        if (sc.HasChanges)
                        {
                            Dump(document, "3.Synchronized.txt");
                            var variables = new ScopedDictionary<string, ValueProviderBase>(null);
                            foreach (var root in document.AllRootElements())
                            {
                                foreach (var node in root.Descendants<BaseNode>().ToList())
                                {
                                    node.RenderTemplate(variables);
                                }
                            }

                            Dump(document, "4.Rendered.txt");
                        }
                    });

                    if (!sc.HasChanges)
                        fileSync = null;
                    else
                    {
                        file.AllowChange = true;
                        file.BinaryFile = bytes;

                        using (replacements.WithReplacedDatabaseName())
                            fileSync = Schema.Current.Table<FileEntity>().UpdateSqlSync(file, f => f.Hash == oldHash, comment: "WordTemplate: " + wt.Name);
                    }
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return DeleteWorkTemplateAndFile();

                    if (ex.Result == FixTokenResult.RegenerateEntity)
                        return RegenerateTemplateAndFile();

                    throw new InvalidOperationException("Unexcpected {0}".FormatWith(ex.Result));
                }

                if (wt.Filters.Any())
                {
                    using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                    {
                        foreach (var item in wt.Filters.ToList())
                        {
                            QueryTokenEmbedded token = item.Token!;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Filters", allowRemoveToken: false, allowReCreate: wt.Model != null))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: return DeleteWorkTemplateAndFile();
                                case FixTokenResult.RemoveToken: wt.Filters.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                case FixTokenResult.RegenerateEntity: return RegenerateTemplateAndFile();
                                default: break;
                            }
                        }
                    }
                }

                if (wt.Orders.Any())
                {
                    using (DelayedConsole.Delay(() => Console.WriteLine(" Orders:")))
                    {
                        foreach (var item in wt.Orders.ToList())
                        {
                            QueryTokenEmbedded token = item.Token!;
                            switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Orders", allowRemoveToken: false, allowReCreate: wt.Model != null))
                            {
                                case FixTokenResult.Nothing: break;
                                case FixTokenResult.DeleteEntity: return DeleteWorkTemplateAndFile();
                                case FixTokenResult.RemoveToken: wt.Orders.Remove(item); break;
                                case FixTokenResult.SkipEntity: return null;
                                case FixTokenResult.Fix: item.Token = token; break;
                                case FixTokenResult.RegenerateEntity: return RegenerateTemplateAndFile();
                                default: break;
                            }
                        }
                    }
                }


                try
                {

                    var sc = new TemplateSynchronizationContext(replacements, sd, qd, wt.Model?.ToType());

                    wt.FileName = TextTemplateParser.Synchronize(wt.FileName, sc);

                    using (replacements.WithReplacedDatabaseName())

                        wordTemplateSync = table.UpdateSqlSync(wt, e => e.Name == wt.Name, includeCollections: true, comment: "WordTempalte: " + wt.Name);
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return DeleteWorkTemplateAndFile();

                    if (ex.Result == FixTokenResult.RegenerateEntity)
                        return RegenerateTemplateAndFile();

                    throw new UnexpectedValueException(ex.Result);
                }

            
                return SqlPreCommand.Combine(Spacing.Simple, wordTemplateSync, fileSync);
            }
        }
        catch (Exception e)
        {
            return new SqlPreCommandSimple("-- Exception on {0}\r\n{1}".FormatWith(wt.BaseToString(), e.Message.Indent(2, '-')));
        }
    }

    public static byte[] ProcessOpenXmlPackage(this WordTemplateEntity template, Action<OpenXmlPackage> processPackage)
    {
        var file = template.Template.RetrieveAndRemember();

        using (var memory = new MemoryStream())
        {
            memory.WriteAllBytes(file.BinaryFile);

            var ext = Path.GetExtension(file.FileName).ToLower();

            var document = 
                ext == ".docx" ? (OpenXmlPackage)WordprocessingDocument.Open(memory, true) :
                ext == ".pptx" ? (OpenXmlPackage)PresentationDocument.Open(memory, true) :
                ext == ".xlsx" ? (OpenXmlPackage)SpreadsheetDocument.Open(memory, true) :
                throw new InvalidOperationException("Extension '{0}' not supported".FormatWith(ext));

            using (document)
            {
                processPackage(document);
            }

            return memory.ToArray();
        }
    }
    
    public static bool Regenerate(WordTemplateEntity wt)
    {
        var newTemplate = wt.Model == null ? null : WordModelLogic.CreateDefaultTemplate(wt.Model);
        if (newTemplate == null)
            return false;

        var file = wt.Template.RetrieveAndRemember();

        newTemplate.SetId(wt.IdOrNull);
        newTemplate.SetIsNew(false);
        newTemplate.Ticks = wt.Ticks;
        newTemplate.Template = wt.Template;

        using (file.AllowChanges())
        {
            file.BinaryFile = newTemplate.Template.Entity.BinaryFile;
            file.FileName = newTemplate.Template.Entity.FileName;
        }

        if (GraphExplorer.IsGraphModified(newTemplate))
        {
            newTemplate.Save();
            return true;
        }
        return false;
    }

    public static IEnumerable<OpenXmlPartRootElement> AllRootElements(this OpenXmlPartContainer document)
    {
        return AllParts(document).Select(p => p.RootElement).NotNull();
    }

    public static IEnumerable<OpenXmlPart> AllParts(this OpenXmlPartContainer container)
    {
        var roots = container.Parts.Select(a => a.OpenXmlPart);
        var result = DirectedGraph<OpenXmlPart>.Generate(roots, c => c.Parts.Select(a => a.OpenXmlPart));
        return result;
    }

    public static void GenerateDefaultTemplates()
    {
        var wordModels = Database.Query<WordModelEntity>().Where(se => !se.WordTemplates().Any()).ToList();

        List<string> exceptions = new List<string>();

        foreach (var se in wordModels)
        {
            try
            {
                var defaultTemplate = WordModelLogic.CreateDefaultTemplate(se);
                if (defaultTemplate != null)
                    defaultTemplate.Save();
            }
            catch (Exception ex)
            {
                exceptions.Add("{0} in {1}:\r\n{2}".FormatWith(ex.GetType().Name, se.FullClassName, ex.Message.Indent(4)));
            }
        }

        if (exceptions.Any())
            throw new Exception(exceptions.ToString("\r\n\r\n"));
    }

    public static void OverrideWordTemplatesConsole()
    {
        var wordTemplates = Database.Query<WordTemplateEntity>().Where(a=>a.Model != null).GroupToDictionary(a => a.Model!);

        var wordModels = Database.Query<WordModelEntity>().ToList();

        List<string> exceptions = new List<string>();
        bool? rememberedAnswer = null;

        foreach (var se in wordModels)
        {
            try
            {
                var defaultTemplate = WordModelLogic.CreateDefaultTemplate(se);
                if (defaultTemplate != null && defaultTemplate.Template != null)
                {
                    var already = wordTemplates.TryGetC(se);

                    if (already == null)
                    {
                        defaultTemplate.Save();
                        SafeConsole.WriteLineColor(ConsoleColor.Green, $"Created {se.FullClassName}");
                    }
                    else
                    {
                        var toModify = already.Only() ?? already.ChooseConsole();

                        if (toModify != null)
                        {
                            if (toModify.Template == null)
                            {
                                toModify.Template = defaultTemplate.Template;
                                toModify.Save();
                                SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Initialized {se.FullClassName}");
                            }
                            else if (MemoryExtensions.SequenceEqual<byte>(toModify.Template.Retrieve().BinaryFile, defaultTemplate.Template.Entity.BinaryFile))
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"Identical {se.FullClassName}");
                            }
                            else
                            {
                                if (SafeConsole.Ask(ref rememberedAnswer, $"Override {se.FullClassName}?"))
                                {
                                    toModify.Template = defaultTemplate.Template;
                                    toModify.Save();
                                    SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Overriden {se.FullClassName}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Red, se.FullClassName);
                SafeConsole.WriteLineColor(ConsoleColor.Red, "{0} in {1}:\r\n{2}".FormatWith(ex.GetType().Name, se.FullClassName, ex.Message.Indent(4)));
            }
        }
    }
}
