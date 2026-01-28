using System.Collections.Frozen;
using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using Signum.Templating;

namespace Signum.Word;

public class WordTemplateParameters : TemplateParameters
{
    public WordTemplateParameters(IEntity? entity, CultureInfo culture,QueryContext? queryContext, WordTemplateEntity template, IWordModel? wordModel, OpenXmlPackage document) : 
          base(entity, culture, queryContext)
    {
        this.Template = template;
        this.Model = wordModel;
        this.Document = document;
    }

    public IWordModel? Model;

    public WordTemplateEntity Template;

    public OpenXmlPackage Document;

    public TokenNode? CurrentTokenNode; //To copy properties

    public override object GetModel()
    {
        if (Model == null)
            throw new ArgumentException("There is no Model set");

        return Model;
    }
}

public interface IWordModel
{
    ModifiableEntity UntypedEntity { get; }

    Entity? ApplicableTo { get; }
    
    List<Filter> GetFilters(QueryDescription qd);
    Pagination GetPagination();
    List<Order> GetOrders(QueryDescription queryDescription);
}

public abstract class WordModel<T> : IWordModel
   where T : ModifiableEntity
{
    public WordModel(T entity)
    {
        this.Entity = entity;
    }

    public T Entity { get; set; }

    public virtual Entity? ApplicableTo => this.Entity as Entity;

    ModifiableEntity IWordModel.UntypedEntity
    {
        get { return Entity; }
    }

    public virtual List<Filter> GetFilters(QueryDescription qd)
    {
        var imp = qd.Columns.SingleEx(a => a.IsEntity).Implementations!.Value;

        if (imp.IsByAll && typeof(Entity).IsAssignableFrom(typeof(T)) || imp.Types.Contains(typeof(T)))
            return new List<Filter>
            {
                new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, ((Entity)(ModifiableEntity)Entity).ToLite())
            };

        throw new InvalidOperationException($"Since {typeof(T).Name} is not in {imp}, it's necessary to override ${nameof(GetFilters)} in ${this.GetType().Name}");
    }

    public virtual List<Order> GetOrders(QueryDescription queryDescription)
    {
        return new List<Order>();
    }

    public virtual Pagination GetPagination()
    {
        return new Pagination.All();
    }        
}

public class MultiEntityWord : WordModel<MultiEntityModel>
{
    public MultiEntityWord(MultiEntityModel entity) : base(entity)
    {
    }

    public override List<Filter> GetFilters(QueryDescription qd)
    {
        return new List<Filter>
        {
            new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.IsIn, this.Entity.Entities.ToList())
        };
    }
}

public class QueryWord : WordModel<QueryModel>
{
    public QueryWord(QueryModel entity) : base(entity)
    {
    }

    public override List<Filter> GetFilters(QueryDescription qd)
    {
        return this.Entity.Filters;
    }

    public override Pagination GetPagination()
    {
        return this.Entity.Pagination;
    }

    public override List<Order> GetOrders(QueryDescription queryDescription)
    {
        return this.Entity.Orders;
    }
}

public static class WordModelLogic
{
    class WordModelInfo
    {
        public object QueryName;
        public Func<WordTemplateEntity>? DefaultTemplateConstructor;

        public WordModelInfo(object queryName)
        {
            QueryName = queryName;
        }
    }

    static ResetLazy<FrozenDictionary<Lite<WordModelEntity>, List<Lite<WordTemplateEntity>>>> WordModelToTemplates = null!;
    static Dictionary<Type, WordModelInfo> registeredWordModels = new Dictionary<Type, WordModelInfo>();
    public static ResetLazy<FrozenDictionary<Type, WordModelEntity>> WordModelTypeToEntity = null!;
    public static ResetLazy<FrozenDictionary<WordModelEntity, Type>> WordModelEntityToType = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.Generating += Schema_Generating;
        sb.Schema.Synchronizing += Schema_Synchronizing;
        sb.Include<WordModelEntity>()
            .WithQuery(() => se => new
            {
                Entity = se,
                se.Id,
                se.FullClassName,
            });

        RegisterWordModel<MultiEntityWord>(null);
        RegisterWordModel<QueryWord>(null);

        new Graph<WordTemplateEntity>.ConstructFrom<WordModelEntity>(WordTemplateOperation.CreateWordTemplateFromWordModel)
        {
            CanConstruct = se => HasDefaultTemplateConstructor(se) ? null : WordTemplateMessage.NoDefaultTemplateDefined.NiceToString(),
            Construct = (se, _) => CreateDefaultTemplate(se)!.Save()
        }.Register();

        WordModelToTemplates = sb.GlobalLazy(() => (
            from et in Database.Query<WordTemplateEntity>()
            where et.Model != null
            select new { swe = et.Model, et = et.ToLite() })
            .GroupToDictionary(pair => pair.swe!.ToLite(), pair => pair.et!)
            .ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(WordModelEntity), typeof(WordTemplateEntity)));

        WordModelTypeToEntity = sb.GlobalLazy(() =>
        {
            var dbWordModels = Database.RetrieveAll<WordModelEntity>();
            return EnumerableExtensions.JoinRelaxed(
                dbWordModels, 
                registeredWordModels.Keys, 
                swr => swr.FullClassName, 
                type => type.FullName!,
                (swr, type) => KeyValuePair.Create(type, swr), 
                "caching " + nameof(WordModelEntity)).ToFrozenDictionaryEx();
        }, new InvalidateWith(typeof(WordModelEntity)));

        sb.Schema.Initializing += () => WordModelTypeToEntity.Load();

        WordModelEntityToType = sb.GlobalLazy(() => WordModelTypeToEntity.Value.Inverse().ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(WordModelEntity)));
    }

    internal static bool HasDefaultTemplateConstructor(WordModelEntity wordModel)
    {
        WordModelInfo info = registeredWordModels.GetOrThrow(wordModel.ToType());

        return info.DefaultTemplateConstructor != null;
    }

    internal static WordTemplateEntity? CreateDefaultTemplate(WordModelEntity wordModel)
    {
        WordModelInfo info = registeredWordModels.GetOrThrow(wordModel.ToType());

        if (info.DefaultTemplateConstructor == null)
            return null;

        WordTemplateEntity template = info.DefaultTemplateConstructor();

        if (template.Name == null)
            template.Name = wordModel.FullClassName;

        template.Model = wordModel;
        template.Query = QueryLogic.GetQueryEntity(info.QueryName);
        template.ParseData(QueryLogic.Queries.QueryDescription(info.QueryName));

        return template;
    }

 

    public static byte[] CreateReport(this IWordModel model, bool avoidConversion = false)
    {
        return model.CreateReport(out WordTemplateEntity rubish, avoidConversion);
    }

    public static byte[] CreateReport(this IWordModel model, out WordTemplateEntity template, bool avoidConversion = false, WordTemplateLogic.FileNameBox? fileNameBox = null)
    {
        WordModelEntity wordModel = GetWordModelEntity(model.GetType());

        template = GetDefaultTemplate(wordModel, model.UntypedEntity as Entity);

        return template.ToLite().CreateReport(null, model, avoidConversion, fileNameBox); 
    }

    public static FileContent CreateReportFileContent(this IWordModel model, bool avoidConversion = false)
    {
        var box = new WordTemplateLogic.FileNameBox();
        var bytes = model.CreateReport(out WordTemplateEntity rubish, avoidConversion, box);
        return new FileContent(box.FileName!, bytes);
    }

    public static WordModelEntity GetWordModelEntity(string fullClassName)
    {
        return WordModelTypeToEntity.Value.Where(x => x.Key.FullName == fullClassName).FirstOrDefault().Value;
    }

    public static WordModelEntity GetWordModelEntity(Type type)
    {
        return WordModelTypeToEntity.Value.GetOrThrow(type);
    }

    public static WordTemplateEntity GetDefaultTemplate(WordModelEntity model, Entity? entity)
    {
        var templates = WordModelToTemplates.Value.TryGetC(model.ToLite()).EmptyIfNull().Select(a => WordTemplateLogic.WordTemplatesLazy.Value.GetOrThrow(a));
        if (templates.IsNullOrEmpty() && HasDefaultTemplateConstructor(model))
        {
            using (ExecutionMode.Global())
            using (OperationLogic.AllowSave<WordTemplateEntity>())
            using (var tr = Transaction.ForceNew())
            {
                var template = CreateDefaultTemplate(model)!;
                
                template.Save();

                return tr.Commit(template);
            }
        }

        var ci = WordTemplateLogic.GetCultureInfo?.Invoke(entity) ?? CultureInfo.CurrentCulture;
        
        var isAllowed = Schema.Current.GetInMemoryFilter<WordTemplateEntity>(userInterface: false);
        var candidates = templates.Where(isAllowed).Where(t => t.IsApplicable(entity));
        return GetTemplate(candidates, model, ci) ??
            GetTemplate(candidates, model, CultureInfo.CurrentCulture.Parent) ??
            candidates.SingleEx(
                () => $"No active WordTemplate for {model} in {CultureInfo.CurrentCulture} or {CultureInfo.CurrentCulture.Parent}",
                () => $"More than one active WordTemplate for {model} in {CultureInfo.CurrentCulture} or {CultureInfo.CurrentCulture.Parent}");
    }

    private static WordTemplateEntity? GetTemplate(IEnumerable<WordTemplateEntity> candidates, WordModelEntity model, CultureInfo culture)
    {
        return candidates
            .Where(a => a.Culture.Name == culture.Name)
            .SingleOrDefaultEx(() => $"More than one active WordTemplate for WordModel {model} in {culture.Name} found");
    }

    static SqlPreCommand? Schema_Generating()
    {
        Table table = Schema.Current.Table<WordModelEntity>();

        return (from ei in GenerateTemplates()
                select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
    }

    internal static List<WordModelEntity> GenerateTemplates()
    {
        var list = (from type in registeredWordModels.Keys
                    select new WordModelEntity { FullClassName = type.FullName! }).ToList();
        return list;
    }

    static readonly string wordModelReplacementKey = "WordModel";

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        Table table = Schema.Current.Table<WordModelEntity>();

        Dictionary<string, WordModelEntity> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
        Dictionary<string, WordModelEntity> old = Administrator.TryRetrieveAll<WordModelEntity>(replacements).ToDictionary(c =>
            c.FullClassName);

        replacements.AskForReplacements(
            old.Keys.ToHashSet(),
            should.Keys.ToHashSet(), wordModelReplacementKey);

        Dictionary<string, WordModelEntity> current = replacements.ApplyReplacementsToOld(old, wordModelReplacementKey);

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: (tn, s) => table.InsertSqlSync(s),
                removeOld: (tn, c) => table.DeleteSqlSync(c, swt => swt.FullClassName == c.FullClassName),
                mergeBoth: (tn, s, c) =>
                {
                    var oldClassName = c.FullClassName;
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c, swt => swt.FullClassName == oldClassName);
                });
    }

    public static void RegisterWordModel<T>(Func<WordTemplateEntity>? defaultTemplateConstructor, object? queryName = null)
     where T : IWordModel
    {
        RegisterWordModel(typeof(T), defaultTemplateConstructor, queryName);
    }

    public static void RegisterWordModel(Type wordModelType, Func<WordTemplateEntity>? defaultTemplateConstructor = null, object? queryName = null)
    {
        registeredWordModels[wordModelType] = new WordModelInfo(queryName ?? GetEntityType(wordModelType))
        {
            DefaultTemplateConstructor = defaultTemplateConstructor,
        };
    }

    public static Type GetEntityType(Type wordModelType)
    {
        var baseType = wordModelType.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(WordModel<>)));

        if (baseType != null)
        {
            return baseType.GetGenericArguments()[0];
        }

        throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterWordModel".FormatWith(wordModelType.TypeName()));
    }

    public static Type ToType(this WordModelEntity modelEntity)
    {
        return WordModelEntityToType.Value.GetOrThrow(modelEntity);
    }

    public static bool RequiresExtraParameters(WordModelEntity modelEntity)
    {
        return GetEntityConstructor(modelEntity.ToType()) == null;
    }

    public static ConstructorInfo? GetEntityConstructor(Type wordModelType)
    {
        var entityType = GetEntityType(wordModelType);

        return (from ci in wordModelType.GetConstructors()
                let pi = ci.GetParameters().Only()
                where pi != null && pi.ParameterType == entityType
                select ci).SingleOrDefaultEx();
    }

    public static IWordModel CreateDefaultWordModel(WordModelEntity wordModel, ModifiableEntity? entity)
    {
        try
        {
            return (IWordModel)WordModelLogic.GetEntityConstructor(wordModel.ToType())!.Invoke(new[] { entity });
        }
        catch (TargetInvocationException e)
        {
            e.InnerException!.PreserveStackTrace();

            throw e.InnerException!;
        }
    }
}
