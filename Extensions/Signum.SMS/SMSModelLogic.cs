using Signum.Engine.Sync;
using System.Collections.Frozen;
using System.Globalization;

namespace Signum.SMS;

public interface ISMSModel
{
    Entity UntypedEntity { get; }

    List<Filter> GetFilters(QueryDescription qd);
    Pagination GetPagination();
    List<Order> GetOrders(QueryDescription queryDescription);
}

public abstract class SMSModel<T> : ISMSModel
    where T : Entity
{
    public SMSModel(T entity)
    {
        this.Entity = entity;
    }

    public T Entity { get; set; }

    Entity ISMSModel.UntypedEntity
    {
        get { return Entity; }
    }

    public virtual List<Filter> GetFilters(QueryDescription qd)
    {
        var imp = qd.Columns.SingleEx(a => a.IsEntity).Implementations!.Value;

        if (imp.IsByAll && typeof(Entity).IsAssignableFrom(typeof(T)) || imp.Types.Contains(typeof(T)))
            return new List<Filter>
            {
                new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, Entity.ToLite())
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


public static class SMSModelLogic
{
    class SMSModelInfo
    {
        public object QueryName;
        public Func<SMSTemplateEntity>? DefaultTemplateConstructor;

        public SMSModelInfo(object queryName)
        {
            QueryName = queryName;
        }
    }

    static ResetLazy<FrozenDictionary<Lite<SMSModelEntity>, List<SMSTemplateEntity>>> SMSModelToTemplates = null!;
    static Dictionary<Type, SMSModelInfo> registeredModels = new Dictionary<Type, SMSModelInfo>();
    static ResetLazy<FrozenDictionary<Type, SMSModelEntity>> typeToEntity = null!;
    static ResetLazy<FrozenDictionary<SMSModelEntity, Type>> entityToType = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.Generating += Schema_Generating;
        sb.Schema.Synchronizing += Schema_Synchronizing;
        sb.Include<SMSModelEntity>()
            .WithQuery(() => model => new
            {
                Entity = model,
                model.Id,
                model.FullClassName,
            });


        new Graph<SMSTemplateEntity>.ConstructFrom<SMSModelEntity>(SMSTemplateOperation.CreateSMSTemplateFromModel)
        {
            Construct = (model, _) => CreateDefaultTemplate(model)
        }.Register();

        SMSModelToTemplates = sb.GlobalLazy(() => (
            from et in Database.Query<SMSTemplateEntity>()
            where et.Model != null
            select KeyValuePair.Create(et.Model!.ToLite(), et))
            .GroupToFrozenDictionary(),
            new InvalidateWith(typeof(SMSTemplateEntity), typeof(SMSModelEntity)));

        typeToEntity = sb.GlobalLazy(() =>
        {
            var dbModels = Database.RetrieveAll<SMSModelEntity>();
            return EnumerableExtensions.JoinRelaxed(
                dbModels,
                registeredModels.Keys,
                entity => entity.FullClassName,
                type => type.FullName!,
                (entity, type) => KeyValuePair.Create(type, entity),
                "caching " + nameof(SMSModelEntity))
                .ToFrozenDictionary();
        }, new InvalidateWith(typeof(SMSModelEntity)));


        sb.Schema.Initializing += () => typeToEntity.Load();

        entityToType = sb.GlobalLazy(() => typeToEntity.Value.Inverse().ToFrozenDictionary(),
            new InvalidateWith(typeof(SMSModelEntity)));
    }

    static readonly string SMSModelReplacementKey = "SMSModel";

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        Table table = Schema.Current.Table<SMSModelEntity>();

        Dictionary<string, SMSModelEntity> should = GenerateSMSModelEntities().ToDictionary(s => s.FullClassName);
        Dictionary<string, SMSModelEntity> old = Administrator.TryRetrieveAll<SMSModelEntity>(replacements).ToDictionary(c =>
            c.FullClassName);

        replacements.AskForReplacements(
            old.Keys.ToHashSet(),
            should.Keys.ToHashSet(), SMSModelReplacementKey);

        Dictionary<string, SMSModelEntity> current = replacements.ApplyReplacementsToOld(old, SMSModelReplacementKey);

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: (tn, s) => table.InsertSqlSync(s),
                removeOld: (tn, c) => table.DeleteSqlSync(c, se => se.FullClassName == c.FullClassName),
                mergeBoth: (tn, s, c) =>
                {
                    var oldClassName = c.FullClassName;
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c, se => se.FullClassName == c.FullClassName, comment: oldClassName);
                });
    }

    public static void RegisterSMSModel<T>(Func<SMSTemplateEntity> defaultTemplateConstructor, object? queryName = null)
      where T : ISMSModel
    {
        RegisterSMSModel(typeof(T), defaultTemplateConstructor, queryName);
    }

    public static void RegisterSMSModel(Type model, Func<SMSTemplateEntity> defaultTemplateConstructor, object? queryName = null)
    {
        if (defaultTemplateConstructor == null)
            throw new ArgumentNullException(nameof(defaultTemplateConstructor));

        registeredModels[model] = new SMSModelInfo(queryName ?? GetEntityType(model))
        {
            DefaultTemplateConstructor = defaultTemplateConstructor,
        };
    }

    public static Type GetEntityType(Type model)
    {
        var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(SMSModel<>)));

        if (baseType != null)
        {
            return baseType.GetGenericArguments()[0];
        }

        throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSMSModel".FormatWith(model.TypeName()));
    }

    internal static List<SMSModelEntity> GenerateSMSModelEntities()
    {
        var list = (from type in registeredModels.Keys
                    select new SMSModelEntity
                    {
                        FullClassName = type.FullName!
                    }).ToList();
        return list;
    }

    static SqlPreCommand? Schema_Generating()
    {
        Table table = Schema.Current.Table<SMSModelEntity>();

        return (from ei in GenerateSMSModelEntities()
                select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
    }


    public static SMSModelEntity GetSMSModelEntity<T>() where T : ISMSModel
    {
        return ToSMSModelEntity(typeof(T));
    }

    public static SMSModelEntity GetSMSModelEntity(string fullClassName)
    {
        return typeToEntity.Value.Where(x => x.Key.FullName == fullClassName).FirstOrDefault().Value;
    }

    public static SMSModelEntity ToSMSModelEntity(Type smsModelType)
    {
        return typeToEntity.Value.GetOrThrow(smsModelType, "The SMSModel {0} was not registered");
    }

    public static Type ToType(this SMSModelEntity smsModelEntity)
    {
        return entityToType.Value.GetOrThrow(smsModelEntity, "The SMSModel {0} was not registered");
    }

    public static void SendSMS(this ISMSModel smsModel, CultureInfo? forceCultureInfo = null)
    {
        var result = smsModel.CreateSMSMessage(forceCultureInfo);
        SMSLogic.SendSMS(result);
    }

    public static void SendAsyncSMS(this ISMSModel smsModel, CultureInfo? forceCultureInfo = null)
    {
        var result = smsModel.CreateSMSMessage(forceCultureInfo);
        SMSLogic.SendAsyncSMS(result);
    }

    public static SMSMessageEntity CreateSMSMessage(this ISMSModel smsModel, CultureInfo? forceCultureInfo = null)
    {
        if (smsModel.UntypedEntity == null)
            throw new InvalidOperationException("Entity property not set on SMSModel");

        using (ExecutionMode.SetIsolation(smsModel.UntypedEntity))
        {
            var smsModelEntity = ToSMSModelEntity(smsModel.GetType());
            var template = GetDefaultTemplate(smsModelEntity);

            return SMSLogic.CreateSMSMessage(template.ToLite(), smsModel.UntypedEntity, smsModel, forceCultureInfo);
        }
    }

    private static SMSTemplateEntity GetDefaultTemplate(SMSModelEntity smsModelEntity)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<SMSTemplateEntity>(userInterface: false);

        var templates = SMSModelToTemplates.Value.TryGetC(smsModelEntity.ToLite()).EmptyIfNull();
        
        if (templates.IsNullOrEmpty())
        {
            using (ExecutionMode.Global())
            using (OperationLogic.AllowSave<SMSTemplateEntity>())
            using (var tr = Transaction.ForceNew())
            {
                var template = CreateDefaultTemplate(smsModelEntity);

                template.Save();

                return tr.Commit(template);
            }
        }

        templates = templates.Where(isAllowed);
        return templates.Where(t => t.IsActive).SingleEx(() => "Active EmailTemplates for SystemEmail {0}".FormatWith(smsModelEntity));
    }

    internal static SMSTemplateEntity CreateDefaultTemplate(SMSModelEntity smsModel)
    {
        SMSModelInfo info = registeredModels.GetOrThrow(entityToType.Value.GetOrThrow(smsModel));

        if (info.DefaultTemplateConstructor == null)
            throw new InvalidOperationException($"No EmailTemplate for {smsModel} found and DefaultTemplateConstructor = null");

        SMSTemplateEntity template = info.DefaultTemplateConstructor.Invoke();

        if (template.Name == null)
            template.Name = smsModel.FullClassName;

        template.Model = smsModel;
        template.Query = QueryLogic.GetQueryEntity(info.QueryName);

        template.ParseData(QueryLogic.Queries.QueryDescription(info.QueryName));
      
        return template;
    }

    public static void GenerateAllTemplates()
    {
        foreach (var smsModelType in registeredModels.Keys)
        {
            var smsModelEntity = ToSMSModelEntity(smsModelType);

            var template = Database.Query<SMSTemplateEntity>().SingleOrDefaultEx(t => t.Model.Is(smsModelEntity));

            if (template == null)
            {
                template = CreateDefaultTemplate(smsModelEntity);

                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<SMSTemplateEntity>())
                    template.Save();
            }
        }
    }

    public static bool RequiresExtraParameters(SMSModelEntity smsModelEntity)
    {
        return GetEntityConstructor(entityToType.Value.GetOrThrow(smsModelEntity)) == null;
    }

    internal static bool HasDefaultTemplateConstructor(SMSModelEntity smsModelEntity)
    {
        SMSModelInfo info = registeredModels.GetOrThrow(smsModelEntity.ToType());
        
        return info.DefaultTemplateConstructor != null;
    }

    public static ISMSModel CreateModel(SMSModelEntity model, ModifiableEntity? entity)
    {
        return (ISMSModel)SMSModelLogic.GetEntityConstructor(model.ToType())!.Invoke(new[] { entity });
    }

    public static ConstructorInfo? GetEntityConstructor(Type smsModel)
    {
        var entityType = GetEntityType(smsModel);

        return (from ci in smsModel.GetConstructors()
                let pi = ci.GetParameters().Only()
                where pi != null && pi.ParameterType == entityType
                select ci).SingleOrDefaultEx();            
    }
}
