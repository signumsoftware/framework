using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Isolation;
using Signum.Entities.Templating;
using Signum.Engine.UserAssets;

namespace Signum.Engine.Mailing
{
    public interface ISystemEmail
    {
        ModifiableEntity UntypedEntity { get; }
        List<EmailOwnerRecipientData> GetRecipients();

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

    public abstract class SystemEmail<T> : ISystemEmail
        where T : ModifiableEntity
    {
        public SystemEmail(T entity)
        {
            this.Entity = entity;
        }

        public T Entity { get; set; }

        ModifiableEntity ISystemEmail.UntypedEntity
        {
            get { return Entity; }
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
            var imp = qd.Columns.SingleEx(a => a.IsEntity).Implementations.Value;

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

    public class MultiEntityEmailTemplate : SystemEmail<MultiEntityModel>
    {
        public MultiEntityEmailTemplate(MultiEntityModel entity) : base(entity)
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

    public class QueryEmailTemplate : SystemEmail<QueryModel>
    {
        public QueryEmailTemplate(QueryModel entity) : base(entity)
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

    public static class SystemEmailLogic
    {
        class SystemEmailInfo
        {
            public Func<EmailTemplateEntity> DefaultTemplateConstructor;
            public object QueryName;
        }

        static ResetLazy<Dictionary<Lite<SystemEmailEntity>, List<EmailTemplateEntity>>> SystemEmailsToEmailTemplates;
        static Dictionary<Type, SystemEmailInfo> systemEmails = new Dictionary<Type, SystemEmailInfo>();
        static ResetLazy<Dictionary<Type, SystemEmailEntity>> systemEmailToEntity;
        static ResetLazy<Dictionary<SystemEmailEntity, Type>> systemEmailToType;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Include<SystemEmailEntity>()
                    .WithQuery(() => se => new
                    {
                        Entity = se,
                        se.Id,
                        se.FullClassName,
                    });

                UserAssetsImporter.RegisterName<EmailTemplateEntity>("EmailTemplate");


                new Graph<EmailTemplateEntity>.ConstructFrom<SystemEmailEntity>(EmailTemplateOperation.CreateEmailTemplateFromSystemEmail)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemEmailsToEmailTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<EmailTemplateEntity>()
                    where et.SystemEmail != null
                    select new { se = et.SystemEmail, et })
                    .GroupToDictionary(pair => pair.se.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemEmailEntity), typeof(EmailTemplateEntity)));

                systemEmailToEntity = sb.GlobalLazy(() =>
                {
                    var dbSystemEmails = Database.RetrieveAll<SystemEmailEntity>();
                    return EnumerableExtensions.JoinRelaxed(
                        dbSystemEmails,
                        systemEmails.Keys,
                        systemEmail => systemEmail.FullClassName,
                        type => type.FullName,
                        (systemEmail, type) => KVP.Create(type, systemEmail),
                        "caching " + nameof(SystemEmailEntity))
                        .ToDictionary();
                }, new InvalidateWith(typeof(SystemEmailEntity)));


                sb.Schema.Initializing += () => systemEmailToEntity.Load();

                systemEmailToType = sb.GlobalLazy(() => systemEmailToEntity.Value.Inverse(),
                    new InvalidateWith(typeof(SystemEmailEntity)));
            }
        }

        static readonly string systemTemplatesReplacementKey = "SystemEmail";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<SystemEmailEntity>();

            Dictionary<string, SystemEmailEntity> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, SystemEmailEntity> old = Administrator.TryRetrieveAll<SystemEmailEntity>(replacements).ToDictionary(c =>
                c.FullClassName);

            replacements.AskForReplacements(
                old.Keys.ToHashSet(),
                should.Keys.ToHashSet(), systemTemplatesReplacementKey);

            Dictionary<string, SystemEmailEntity> current = replacements.ApplyReplacementsToOld(old, systemTemplatesReplacementKey);

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

        public static void RegisterSystemEmail<T>(Func<EmailTemplateEntity> defaultTemplateConstructor, object queryName = null)
          where T : ISystemEmail
        {
            RegisterSystemEmail(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemEmail(Type model, Func<EmailTemplateEntity> defaultTemplateConstructor, object queryName = null)
        {
            if (defaultTemplateConstructor == null)
                throw new ArgumentNullException(nameof(defaultTemplateConstructor));

            systemEmails[model] = new SystemEmailInfo
            {
                DefaultTemplateConstructor = defaultTemplateConstructor,
                QueryName = queryName ?? GetEntityType(model),
            };
        }

        public static Type GetEntityType(Type model)
        {
            var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(SystemEmail<>)));

            if (baseType != null)
            {
                return baseType.GetGenericArguments()[0];
            }

            throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSystemEmail".FormatWith(model.TypeName()));
        }

        internal static List<SystemEmailEntity> GenerateTemplates()
        {
            var list = (from type in systemEmails.Keys
                        select new SystemEmailEntity
                        {
                             FullClassName = type.FullName
                        }).ToList();
            return list;
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemEmailEntity>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }


        public static SystemEmailEntity GetSystemEmailEntity<T>() where T : ISystemEmail
        {
            return ToSystemEmailEntity(typeof(T));
        }

        public static SystemEmailEntity GetSystemEmailEntity(string fullClassName)
        {
            return systemEmailToEntity.Value.Where(x => x.Key.FullName == fullClassName).FirstOrDefault().Value;
        }

        public static SystemEmailEntity ToSystemEmailEntity(Type type)
        {
            return systemEmailToEntity.Value.GetOrThrow(type, "The system email {0} was not registered");
        }

        public static Type ToType(this SystemEmailEntity systemEmail)
        {
            if (systemEmail == null)
                return null;

            return systemEmailToType.Value.GetOrThrow(systemEmail, "The system email {0} was not registered");
        }

        public static IEnumerable<EmailMessageEntity> CreateEmailMessage(this ISystemEmail systemEmail)
        {
            if (systemEmail.UntypedEntity == null)
                throw new InvalidOperationException("Entity property not set on SystemEmail");

            using (IsolationEntity.Override((systemEmail.UntypedEntity as Entity)?.TryIsolation()))
            {
                var systemEmailEntity = ToSystemEmailEntity(systemEmail.GetType());
                var template = GetDefaultTemplate(systemEmailEntity, systemEmail.UntypedEntity as Entity);

                return EmailTemplateLogic.CreateEmailMessage(template.ToLite(), systemEmail: systemEmail);
            }
        }

  

        private static EmailTemplateEntity GetDefaultTemplate(SystemEmailEntity systemEmailEntity, Entity entity)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<EmailTemplateEntity>(userInterface: false);

            var templates = SystemEmailsToEmailTemplates.Value.TryGetC(systemEmailEntity.ToLite()).EmptyIfNull();
            
            if (templates.IsNullOrEmpty())
            {
                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<EmailTemplateEntity>())
                using (Transaction tr = Transaction.ForceNew())
                {
                    var template = CreateDefaultTemplate(systemEmailEntity);

                    template.Save();

                    return tr.Commit(template);
                }
            }

            templates = templates.Where(isAllowed);
            return templates.Where(t => t.IsApplicable(entity)).SingleEx(() => "Active EmailTemplates for SystemEmail {0}".FormatWith(systemEmailEntity));
        }

        internal static EmailTemplateEntity CreateDefaultTemplate(SystemEmailEntity systemEmail)
        {
            SystemEmailInfo info = systemEmails.GetOrThrow(systemEmailToType.Value.GetOrThrow(systemEmail));

            EmailTemplateEntity template = info.DefaultTemplateConstructor();
            if (template.MasterTemplate != null)
                template.MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate();

            if (template.Name == null)
                template.Name = systemEmail.FullClassName;

            template.SystemEmail = systemEmail;
            template.Query = QueryLogic.GetQueryEntity(info.QueryName);

            template.ParseData(QueryLogic.Queries.QueryDescription(info.QueryName));

            return template;
        }

        public static void GenerateAllTemplates()
        {
            foreach (var systemEmail in systemEmails.Keys)
            {
                var systemEmailEntity = ToSystemEmailEntity(systemEmail);

                var template = Database.Query<EmailTemplateEntity>().SingleOrDefaultEx(t =>
                    t.SystemEmail == systemEmailEntity);

                if (template == null)
                {
                    template = CreateDefaultTemplate(systemEmailEntity);

                    using (ExecutionMode.Global())
                    using (OperationLogic.AllowSave<EmailTemplateEntity>())
                        template.Save();
                }
            }
        }

        public static bool RequiresExtraParameters(SystemEmailEntity systemEmailEntity)
        {
            return GetEntityConstructor(systemEmailToType.Value.GetOrThrow(systemEmailEntity)) == null;
        }

        internal static bool HasDefaultTemplateConstructor(SystemEmailEntity systemEmailTemplate)
        {
            SystemEmailInfo info = systemEmails.GetOrThrow(systemEmailTemplate.ToType());
            
            return info.DefaultTemplateConstructor != null;
        }

        public static ISystemEmail CreateSystemEmail(SystemEmailEntity systemEmail, ModifiableEntity model)
        {
            return (ISystemEmail)SystemEmailLogic.GetEntityConstructor(systemEmail.ToType()).Invoke(new[] { model });
        }

        public static ConstructorInfo GetEntityConstructor(Type systemEmail)
        {
            var entityType = GetEntityType(systemEmail);

            return (from ci in systemEmail.GetConstructors()
                    let pi = ci.GetParameters().Only()
                    where pi != null && pi.ParameterType == entityType
                    select ci).SingleOrDefaultEx();            
        }
    }
}
