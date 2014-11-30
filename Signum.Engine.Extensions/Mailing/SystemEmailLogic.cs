using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Mailing
{
    public interface ISystemEmail
    {
        Entity UntypedEntity { get; }
        List<EmailOwnerRecipientData> GetRecipients();

        List<Filter> GetFilters(QueryDescription qd);
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
        where T : Entity
    {
        public T Entity { get; set; }

        Entity ISystemEmail.UntypedEntity
        {
            get { return Entity; }
        }

        public abstract List<EmailOwnerRecipientData> GetRecipients();

        protected static List<EmailOwnerRecipientData> SendTo(EmailOwnerData ownerData)
        {
            return new List<EmailOwnerRecipientData> { new EmailOwnerRecipientData(ownerData) };
        }

        public virtual List<Filter> GetFilters(QueryDescription qd)
        {
            return new List<Filter>
            {
                new Filter(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, Entity.ToLite())
            };
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
     
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                dqm.RegisterQuery(typeof(SystemEmailEntity), () =>
                    (from se in Database.Query<SystemEmailEntity>()
                     select new
                     {
                         Entity = se,
                         se.Id,
                         se.FullClassName,
                     }));

                new Graph<EmailTemplateEntity>.ConstructFrom<SystemEmailEntity>(EmailTemplateOperation.CreateEmailTemplateFromSystemEmail)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemEmailsToEmailTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<EmailTemplateEntity>()
                    where et.SystemEmail != null
                        && (et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    select new { se = et.SystemEmail, et })
                    .GroupToDictionary(pair => pair.se.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemEmailEntity), typeof(EmailTemplateEntity)));

                systemEmailToEntity = sb.GlobalLazy(() =>
                {
                    var dbSystemEmails = Database.RetrieveAll<SystemEmailEntity>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemEmails, systemEmails.Keys, typeEntity => typeEntity.FullClassName, type => type.FullName,
                        (typeEntity, type) => KVP.Create(type, typeEntity), "caching EmailTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemEmailEntity)));

                systemEmailToType = sb.GlobalLazy(() => systemEmailToEntity.Value.Inverse(),
                    new InvalidateWith(typeof(SystemEmailEntity)));
            }
        }

        static readonly string systemTemplatesReplacementKey = "EmailTemplates";

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

            return Synchronizer.SynchronizeScript(should, current,
                (tn, s) => table.InsertSqlSync(s),
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s, c) =>
                {
                    var oldClassName = c.FullClassName;
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c, comment: oldClassName);
                },
                Spacing.Double);
        }

        public static void RegisterSystemEmail<T>(Func<EmailTemplateEntity> defaultTemplateConstructor, object queryName = null)
          where T : ISystemEmail
        {
            RegisterSystemEmail(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemEmail(Type model, Func<EmailTemplateEntity> defaultTemplateConstructor, object queryName = null)
        {
            if (defaultTemplateConstructor == null)
                throw new ArgumentNullException("defaultTemplateConstructor"); 

            systemEmails[model] = new SystemEmailInfo
            {
                DefaultTemplateConstructor = defaultTemplateConstructor,
                QueryName = queryName ?? GetDefaultQueryName(model),
            };
        }

        static object GetDefaultQueryName(Type model)
        {
            var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(SystemEmail<>)));

            if(baseType != null)
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

            var systemEmailEntity = ToSystemEmailEntity(systemEmail.GetType());
            var template = GetDefaultTemplate(systemEmailEntity);

            return EmailTemplateLogic.CreateEmailMessage(template.ToLite(), systemEmail.UntypedEntity, systemEmail);
        }

        private static EmailTemplateEntity GetDefaultTemplate(SystemEmailEntity systemEmailEntity)
        {
            var list = SystemEmailsToEmailTemplates.Value.TryGetC(systemEmailEntity.ToLite()); 

            if(list.IsNullOrEmpty())
            {
                using (Transaction tr = Transaction.ForceNew())
                {
                    var template = CreateDefaultTemplate(systemEmailEntity);

                    using (ExecutionMode.Global())
                    using (OperationLogic.AllowSave<EmailTemplateEntity>())
                        template.Save();

                    return tr.Commit(template);
                }
            }

            return list.Where(t => t.IsActiveNow()).SingleEx(() => "Active EmailTemplates for SystemEmail {0}".FormatWith(systemEmailEntity));
        }

        internal static EmailTemplateEntity CreateDefaultTemplate(SystemEmailEntity systemEmailEntity)
        {
            SystemEmailInfo info = systemEmails.GetOrThrow(systemEmailToType.Value.GetOrThrow(systemEmailEntity));

            EmailTemplateEntity template = info.DefaultTemplateConstructor();
            if (template.MasterTemplate != null)
                template.MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate();

            if (template.Name == null)
                template.Name = systemEmailEntity.FullClassName;

            template.SystemEmail = systemEmailEntity;
            template.Active = true;
            template.Query = QueryLogic.GetQuery(info.QueryName);

            template.ParseData(DynamicQueryManager.Current.QueryDescription(info.QueryName));

            return template;
        }

        public static void GenerateAllTemplates()
        {
            foreach (var systemEmail in systemEmails.Keys)
            {
                var systemEmailEntity = ToSystemEmailEntity(systemEmail);

                var template = Database.Query<EmailTemplateEntity>().SingleOrDefaultEx(t =>
                    t.IsActiveNow() == true &&
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
    }
}
