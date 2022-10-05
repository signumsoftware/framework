using Signum.Entities.Mailing;
using Signum.Entities.Isolation;
using Signum.Entities.Templating;
using Signum.Engine.UserAssets;
using Signum.Engine.Authorization;
using System.Globalization;
using Signum.Entities.Authorization;

namespace Signum.Engine.Mailing;

public interface IEmailModel
{
    ModifiableEntity UntypedEntity { get; }
    List<EmailOwnerRecipientData> GetRecipients();
    
    EmailOwnerData? GetFrom() { return null; }

    List<Filter> GetFilters(QueryDescription qd);
    Pagination GetPagination();
    List<Order> GetOrders(QueryDescription queryDescription);
}

public class EmailOwnerRecipientData
{
    public EmailOwnerRecipientData(EmailOwnerData ownerData)
    {
        this.OwnerData = ownerData;
    }

    public readonly EmailOwnerData OwnerData;
    public EmailRecipientKind Kind;
}

public abstract class EmailModel<T> : IEmailModel
    where T : ModifiableEntity
{
    public EmailModel(T entity)
    {
        this.Entity = entity;
    }

    public T Entity { get; set; }

    ModifiableEntity IEmailModel.UntypedEntity
    {
        get { return Entity; }
    }

    public virtual EmailOwnerData? GetFrom()
    {
        return null;
    }

    public virtual List<EmailOwnerRecipientData> GetRecipients()
    {
        return new List<EmailOwnerRecipientData>();
    }

    protected static List<EmailOwnerRecipientData> SendTo(EmailOwnerData ownerData)
    {
        return new List<EmailOwnerRecipientData> { new EmailOwnerRecipientData(ownerData) };
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

public class MultiEntityEmail : EmailModel<MultiEntityModel>
{
    public MultiEntityEmail(MultiEntityModel entity) : base(entity)
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

public class QueryEmail : EmailModel<QueryModel>
{
    public QueryEmail(QueryModel entity) : base(entity)
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

public static class EmailModelLogic
{
    class EmailModelInfo
    {
        public object QueryName;
        public Func<EmailTemplateEntity>? DefaultTemplateConstructor;

        public EmailModelInfo(object queryName)
        {
            QueryName = queryName;
        }
    }

    static ResetLazy<Dictionary<Lite<EmailModelEntity>, List<EmailTemplateEntity>>> EmailModelToTemplates = null!;
    static Dictionary<Type, EmailModelInfo> registeredModels = new Dictionary<Type, EmailModelInfo>();
    static ResetLazy<Dictionary<Type, EmailModelEntity>> typeToEntity = null!;
    static ResetLazy<Dictionary<EmailModelEntity, Type>> entityToType = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Schema.Generating += Schema_Generating;
            sb.Schema.Synchronizing += Schema_Synchronizing;
            sb.Include<EmailModelEntity>()
                .WithQuery(() => se => new
                {
                    Entity = se,
                    se.Id,
                    se.FullClassName,
                });

            UserAssetsImporter.Register<EmailTemplateEntity>("EmailTemplate", EmailTemplateOperation.Save);


            new Graph<EmailTemplateEntity>.ConstructFrom<EmailModelEntity>(EmailTemplateOperation.CreateEmailTemplateFromModel)
            {
                Construct = (se, _) => CreateDefaultTemplateInternal(se)
            }.Register();

            EmailModelToTemplates = sb.GlobalLazy(() => (
                from et in Database.Query<EmailTemplateEntity>()
                where et.Model != null
                select new { se = et.Model, et })
                .GroupToDictionary(pair => pair.se!.ToLite(), pair => pair.et!), /*CSBUG*/ 
                new InvalidateWith(typeof(EmailModelEntity), typeof(EmailTemplateEntity)));

            typeToEntity = sb.GlobalLazy(() =>
            {
                var dbModels = Database.RetrieveAll<EmailModelEntity>();
                return EnumerableExtensions.JoinRelaxed(
                    dbModels,
                    registeredModels.Keys,
                    entity => entity.FullClassName,
                    type => type.FullName!,
                    (entity, type) => KeyValuePair.Create(type, entity),
                    "caching " + nameof(EmailModelEntity))
                    .ToDictionary();
            }, new InvalidateWith(typeof(EmailModelEntity)));


            sb.Schema.Initializing += () => typeToEntity.Load();

            entityToType = sb.GlobalLazy(() => typeToEntity.Value.Inverse(),
                new InvalidateWith(typeof(EmailModelEntity)));
        }
    }

    static readonly string EmailModelReplacementKey = "EmailModel";

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        Table table = Schema.Current.Table<EmailModelEntity>();

        Dictionary<string, EmailModelEntity> should = GenerateEmailModelEntities().ToDictionary(s => s.FullClassName);
        Dictionary<string, EmailModelEntity> old = Administrator.TryRetrieveAll<EmailModelEntity>(replacements).ToDictionary(c =>
            c.FullClassName);

        replacements.AskForReplacements(
            old.Keys.ToHashSet(),
            should.Keys.ToHashSet(), EmailModelReplacementKey);

        Dictionary<string, EmailModelEntity> current = replacements.ApplyReplacementsToOld(old, EmailModelReplacementKey);

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: (tn, s) => table.InsertSqlSync(s),
                removeOld: (tn, c) => table.DeleteSqlSync(c, se => se.FullClassName == c.FullClassName),
                mergeBoth: (tn, s, c) =>
                {
                    var oldClassName = c.FullClassName;
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c, se => se.FullClassName == oldClassName);
                });
    }

    public static void RegisterEmailModel<T>(Func<EmailTemplateEntity>? defaultTemplateConstructor, object? queryName = null)
      where T : IEmailModel
    {
        RegisterEmailModel(typeof(T), defaultTemplateConstructor, queryName);
    }

    public static void RegisterEmailModel(Type model, Func<EmailTemplateEntity>? defaultTemplateConstructor, object? queryName = null)
    {
        registeredModels[model] = new EmailModelInfo(queryName ?? GetEntityType(model))
        { 
            DefaultTemplateConstructor = defaultTemplateConstructor,
        };
    }

    public static Type GetEntityType(Type model)
    {
        var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(EmailModel<>)));

        if (baseType != null)
        {
            return baseType.GetGenericArguments()[0];
        }

        throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterEmailModel".FormatWith(model.TypeName()));
    }

    internal static List<EmailModelEntity> GenerateEmailModelEntities()
    {
        var list = (from type in registeredModels.Keys
                    select new EmailModelEntity
                    {
                         FullClassName = type.FullName!
                    }).ToList();
        return list;
    }

    static SqlPreCommand? Schema_Generating()
    {
        Table table = Schema.Current.Table<EmailModelEntity>();

        return (from ei in GenerateEmailModelEntities()
                select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
    }


    public static EmailModelEntity GetEmailModelEntity<T>() where T : IEmailModel
    {
        return ToEmailModelEntity(typeof(T));
    }

    public static EmailModelEntity GetEmailModelEntity(string fullClassName)
    {
        return typeToEntity.Value.Where(x => x.Key.FullName == fullClassName).FirstOrDefault().Value;
    }

    public static EmailModelEntity ToEmailModelEntity(Type emailModelType)
    {
        return typeToEntity.Value.GetOrThrow(emailModelType, "The EmailModel {0} was not registered");
    }

    public static Type ToType(this EmailModelEntity modelEntity)
    {
        return entityToType.Value.GetOrThrow(modelEntity, "The EmailModel {0} was not registered");
    }

    public static IEnumerable<EmailMessageEntity> CreateEmailMessage(this IEmailModel emailModel, CultureInfo? cultureInfo = null)
    {
        if (emailModel.UntypedEntity == null)
            throw new InvalidOperationException("Entity property not set on EmailModel");

        using (IsolationEntity.Override((emailModel.UntypedEntity as Entity)?.TryIsolation()))
        {
            var emailModelEntity = ToEmailModelEntity(emailModel.GetType());
            var template = GetCurrentTemplate(emailModelEntity, emailModel.UntypedEntity as Entity);

            return EmailTemplateLogic.CreateEmailMessage(template.ToLite(), model: emailModel, cultureInfo: cultureInfo);
        }
    }

    public static EmailTemplateEntity GetCurrentTemplate<M>() where M : IEmailModel
    {
        var emailModelEntity = ToEmailModelEntity(typeof(M));

        return GetCurrentTemplate(emailModelEntity, null);
    }


    private static EmailTemplateEntity GetCurrentTemplate(EmailModelEntity emailModelEntity, Entity? entity)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<EmailTemplateEntity>(userInterface: false);

        var templates = EmailModelToTemplates.Value.TryGetC(emailModelEntity.ToLite()).EmptyIfNull();
        templates = templates.Where(isAllowed);

        if (templates.IsNullOrEmpty())
            return CreateDefaultEmailTemplate(emailModelEntity);
     
        return templates.Where(t => t.IsApplicable(entity)).SingleEx(() => "Active EmailTemplates for EmailModel {0}".FormatWith(emailModelEntity));
    }

    public static EmailTemplateEntity CreateDefaultEmailTemplate(EmailModelEntity emailModelEntity)
    {
        using (AuthLogic.Disable())
        using (OperationLogic.AllowSave<EmailTemplateEntity>())
        using (var tr = Transaction.ForceNew())
        {
            var template = CreateDefaultTemplateInternal(emailModelEntity);

            template.Save();

            return tr.Commit(template);
        }
    }

    internal static EmailTemplateEntity CreateDefaultTemplateInternal(EmailModelEntity emailModel)
    {
        EmailModelInfo info = registeredModels.GetOrThrow(entityToType.Value.GetOrThrow(emailModel));

        if (info.DefaultTemplateConstructor == null)
            throw new InvalidOperationException($"No EmailTemplate for {emailModel} found and DefaultTemplateConstructor = null");

        EmailTemplateEntity template = info.DefaultTemplateConstructor.Invoke();
        if (template.MasterTemplate == null)
            template.MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate();

        if (template.Name == null)
            template.Name = emailModel.FullClassName;

        template.Model = emailModel;
        template.Query = QueryLogic.GetQueryEntity(info.QueryName);

        template.ParseData(QueryLogic.Queries.QueryDescription(info.QueryName));
      
        return template;
    }

    public static void GenerateAllTemplates()
    {
        foreach (var emailModelType in registeredModels.Keys)
        {
            var emailModelEntity = ToEmailModelEntity(emailModelType);

            var template = Database.Query<EmailTemplateEntity>().SingleOrDefaultEx(t => t.Model.Is(emailModelEntity));

            if (template == null)
            {
                template = CreateDefaultTemplateInternal(emailModelEntity);

                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<EmailTemplateEntity>())
                    template.Save();
            }
        }
    }

    public static bool RequiresExtraParameters(EmailModelEntity emailModelEntity)
    {
        return GetEntityConstructor(entityToType.Value.GetOrThrow(emailModelEntity)) == null;
    }

    internal static bool HasDefaultTemplateConstructor(EmailModelEntity emailModelEntity)
    {
        EmailModelInfo info = registeredModels.GetOrThrow(emailModelEntity.ToType());
        
        return info.DefaultTemplateConstructor != null;
    }

    public static IEmailModel CreateModel(EmailModelEntity model, ModifiableEntity? entity)
    {
        return (IEmailModel)EmailModelLogic.GetEntityConstructor(model.ToType())!.Invoke(new[] { entity });
    }

    public static ConstructorInfo? GetEntityConstructor(Type emailModel)
    {
        var entityType = GetEntityType(emailModel);

        return (from ci in emailModel.GetConstructors()
                let pi = ci.GetParameters().Only()
                where pi != null && pi.ParameterType == entityType
                select ci).SingleOrDefaultEx();            
    }
}
