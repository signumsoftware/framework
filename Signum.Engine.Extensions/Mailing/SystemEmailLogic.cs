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
        IdentifiableEntity UntypedEntity { get; }
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
        where T : IdentifiableEntity
    {
        public T Entity { get; set; }

        IdentifiableEntity ISystemEmail.UntypedEntity
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
            public Func<EmailTemplateDN> DefaultTemplateConstructor;
            public object QueryName; 
        }

        static ResetLazy<Dictionary<Lite<SystemEmailDN>, List<EmailTemplateDN>>> SystemEmailsToEmailTemplates;
        static Dictionary<Type, SystemEmailInfo> systemEmails = new Dictionary<Type, SystemEmailInfo>();
        static ResetLazy<Dictionary<Type, SystemEmailDN>> systemEmailToDN;
        static ResetLazy<Dictionary<SystemEmailDN, Type>> systemEmailToType;
     
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                dqm.RegisterQuery(typeof(SystemEmailDN), () =>
                    (from se in Database.Query<SystemEmailDN>()
                     select new
                     {
                         Entity = se,
                         se.Id,
                         se.FullClassName,
                     }));

                new Graph<EmailTemplateDN>.ConstructFrom<SystemEmailDN>(EmailTemplateOperation.CreateEmailTemplateFromSystemEmail)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemEmailsToEmailTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<EmailTemplateDN>()
                    where et.SystemEmail != null
                        && (et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    select new { se = et.SystemEmail, et })
                    .GroupToDictionary(pair => pair.se.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemEmailDN), typeof(EmailTemplateDN)));

                systemEmailToDN = sb.GlobalLazy(() =>
                {
                    var dbSystemEmails = Database.RetrieveAll<SystemEmailDN>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemEmails, systemEmails.Keys, typeDN => typeDN.FullClassName, type => type.FullName,
                        (typeDN, type) => KVP.Create(type, typeDN), "caching EmailTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemEmailDN)));

                systemEmailToType = sb.GlobalLazy(() => systemEmailToDN.Value.Inverse(),
                    new InvalidateWith(typeof(SystemEmailDN)));
            }
        }

        static readonly string systemTemplatesReplacementKey = "EmailTemplates";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<SystemEmailDN>();

            Dictionary<string, SystemEmailDN> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, SystemEmailDN> old = Administrator.TryRetrieveAll<SystemEmailDN>(replacements).ToDictionary(c =>
                c.FullClassName);

            replacements.AskForReplacements(
                old.Keys.ToHashSet(),
                should.Keys.ToHashSet(), systemTemplatesReplacementKey);

            Dictionary<string, SystemEmailDN> current = replacements.ApplyReplacementsToOld(old, systemTemplatesReplacementKey);

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

        public static void RegisterSystemEmail<T>(Func<EmailTemplateDN> defaultTemplateConstructor, object queryName = null)
          where T : ISystemEmail
        {
            RegisterSystemEmail(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemEmail(Type model, Func<EmailTemplateDN> defaultTemplateConstructor, object queryName = null)
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

            throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSystemEmail".Formato(model.TypeName()));
        }

        internal static List<SystemEmailDN> GenerateTemplates()
        {
            var list = (from type in systemEmails.Keys
                         select new SystemEmailDN
                         {
                             FullClassName = type.FullName
                         }).ToList();
            return list;
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemEmailDN>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        public static SystemEmailDN ToSystemEmailDN(Type type)
        {
            return systemEmailToDN.Value.GetOrThrow(type, "The system email {0} was not registered");
        }

        public static Type ToType(this SystemEmailDN systemEmail)
        {
            if (systemEmail == null)
                return null;

            return systemEmailToType.Value.GetOrThrow(systemEmail, "The system email {0} was not registered");
        }

        public static IEnumerable<EmailMessageDN> CreateEmailMessage(this ISystemEmail systemEmail)
        {
            if (systemEmail.UntypedEntity == null)
                throw new InvalidOperationException("Entity property not set on SystemEmail");

            var systemEmailDN = ToSystemEmailDN(systemEmail.GetType());
            var template = GetDefaultTemplate(systemEmailDN);

            return EmailTemplateLogic.CreateEmailMessage(template.ToLite(), systemEmail.UntypedEntity, systemEmail);
        }

        private static EmailTemplateDN GetDefaultTemplate(SystemEmailDN systemEmailDN)
        {
            var list = SystemEmailsToEmailTemplates.Value.TryGetC(systemEmailDN.ToLite()); 

            if(list.IsNullOrEmpty())
            {
                using (Transaction tr = Transaction.ForceNew())
                {
                    var template = CreateDefaultTemplate(systemEmailDN);

                    using (ExecutionMode.Global())
                    using (OperationLogic.AllowSave<EmailTemplateDN>())
                        template.Save();

                    return tr.Commit(template);
                }
            }

            return list.Where(t => t.IsActiveNow()).SingleEx(() => "Active EmailTemplates for SystemEmail {0}".Formato(systemEmailDN));
        }

        internal static EmailTemplateDN CreateDefaultTemplate(SystemEmailDN systemEmailDN)
        {
            SystemEmailInfo info = systemEmails.GetOrThrow(systemEmailToType.Value.GetOrThrow(systemEmailDN));

            EmailTemplateDN template = info.DefaultTemplateConstructor();
            if (template.MasterTemplate != null)
                template.MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate();

            if (template.Name == null)
                template.Name = systemEmailDN.FullClassName;

            template.SystemEmail = systemEmailDN;
            template.Active = true;
            template.Query = QueryLogic.GetQuery(info.QueryName);

            template.ParseData(DynamicQueryManager.Current.QueryDescription(info.QueryName));

            return template;
        }

        public static void GenerateAllTemplates()
        {
            foreach (var systemEmail in systemEmails.Keys)
            {
                var systemEmailDN = ToSystemEmailDN(systemEmail);

                var template = Database.Query<EmailTemplateDN>().SingleOrDefaultEx(t =>
                    t.IsActiveNow() == true &&
                    t.SystemEmail == systemEmailDN);

                if (template == null)
                {
                    template = CreateDefaultTemplate(systemEmailDN);

                    using (ExecutionMode.Global())
                    using (OperationLogic.AllowSave<EmailTemplateDN>())
                        template.Save();
                }
            }
        }
    }
}
