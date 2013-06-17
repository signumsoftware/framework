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

namespace Signum.Engine.Mailing
{
    public interface ISystemEmail
    {
        IdentifiableEntity UntypedEntity { get; }
        List<EmailOwnerRecipientData> GetRecipients();

        List<Filter> GetFilters(QueryDescription qd);

        object DefaultQueryName { get; }
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

        protected static List<EmailOwnerRecipientData> To(EmailOwnerData ownerData)
        {
            return new List<EmailOwnerRecipientData> { new EmailOwnerRecipientData(ownerData) };
        }

        public virtual List<Filter> GetFilters(QueryDescription qd)
        {
            return new List<Filter>
            {
                new Filter(QueryUtils.Parse("Entity", qd, false), FilterOperation.EqualTo, Entity.ToLite())
            };
        }

        public object DefaultQueryName
        {
            get { return typeof(T); }
        }
    }

    public static class SystemEmailLogic
    {
        static Dictionary<Type, Func<EmailTemplateDN>> systemEmails = new Dictionary<Type, Func<EmailTemplateDN>>();
        static Dictionary<Type, SystemEmailDN> systemEmailToDN;
        static Dictionary<SystemEmailDN, Type> systemEmailToType;
     
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += Schema_Initializing;
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;
            }
        }

        static void Schema_Initializing()
        {
            var dbTemplates = Database.RetrieveAll<SystemEmailDN>();

            systemEmailToDN = EnumerableExtensions.JoinStrict(
                dbTemplates, systemEmails.Keys, typeDN => typeDN.FullClassName, type => type.FullName,
                (typeDN, type) => KVP.Create(type, typeDN), "caching EmailTemplates").ToDictionary();

            systemEmailToType = systemEmailToDN.Inverse();
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
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c);
                },
                Spacing.Double);
        }

        public static void RegisterSystemEmail<T>(Func<EmailTemplateDN> defaultTemplateConstructor = null)
          where T : ISystemEmail
        {
            RegisterSystemEmail(typeof(T), defaultTemplateConstructor);
        }

        public static void RegisterSystemEmail(Type model, Func<EmailTemplateDN> defaultTemplateConstructor = null)
        {
            systemEmails[model] = defaultTemplateConstructor;
        }

        internal static List<SystemEmailDN> GenerateTemplates()
        {
            var lista = (from type in systemEmails.Keys
                         select new SystemEmailDN
                         {
                             FullClassName = type.FullName
                         }).ToList();
            return lista;
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemEmailDN>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        public static SystemEmailDN ToSystemEmailDN(Type type)
        {
            return systemEmailToDN.GetOrThrow(type, "The system email {0} was not registered");
        }

        public static Type ToType(this SystemEmailDN systemEmail)
        {
            if (systemEmail == null)
                return null;

            return systemEmailToType.GetOrThrow(systemEmail, "The system email {0} was not registered");
        }

        public static EmailMessageDN CreateEmailMessage(this ISystemEmail systemEmail)
        {
            var systemEmailDN = ToSystemEmailDN(systemEmail.GetType());

            var template = Database.Query<EmailTemplateDN>().SingleOrDefaultEx(t =>
                t.IsActiveNow() == true &&
                t.SystemEmail == systemEmailDN);

            if (template == null)
            {
                template = systemEmails.GetOrThrow(systemEmail.GetType())();
                template.SystemEmail = systemEmailDN;
                template.Active = true;

                if (template.Query == null)
                {
                    var emailModelType = systemEmail.DefaultQueryName;
                    if (emailModelType == null)
                        throw new Exception("Query not specified for {0}".Formato(systemEmail));

                    template.Query = QueryLogic.GetQuery(emailModelType);
                }

                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<EmailTemplateDN>())
                    template.Save();
            }

            return EmailTemplateLogic.CreateEmailMessage(template, systemEmail.UntypedEntity, systemEmail);
        }

    }
}
