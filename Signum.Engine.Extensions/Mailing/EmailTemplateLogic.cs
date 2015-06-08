using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.UserQueries;
using System.Net.Configuration;
using System.Globalization;
using System.Configuration;
using Signum.Engine.UserQueries;
using System.Linq.Expressions;
using Signum.Entities.Translation;
using Signum.Engine.Translation;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using Signum.Engine.Templating;
using System.Net.Mail;

namespace Signum.Engine.Mailing
{
    public static class EmailTemplateLogic
    {
        public static bool AvoidSynchronize = false;

        public static EmailTemplateMessageEntity GetCultureMessage(this EmailTemplateEntity template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }
     
        static Expression<Func<SystemEmailEntity, IQueryable<EmailTemplateEntity>>> EmailTemplatesExpression =
            se => Database.Query<EmailTemplateEntity>().Where(et => et.SystemEmail == se);
        public static IQueryable<EmailTemplateEntity> EmailTemplates(this SystemEmailEntity se)
        {
            return EmailTemplatesExpression.Evaluate(se);
        }
        
        public static ResetLazy<Dictionary<Lite<EmailTemplateEntity>, EmailTemplateEntity>> EmailTemplatesLazy;

        public static Func<EmailTemplateEntity, SmtpConfigurationEntity> GetSmtpConfiguration;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<EmailTemplateEntity, SmtpConfigurationEntity> getSmtpConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                GetSmtpConfiguration = getSmtpConfiguration;

                sb.Include<EmailTemplateEntity>();       

                EmailTemplatesLazy = sb.GlobalLazy(() => Database.Query<EmailTemplateEntity>()
                    .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(EmailTemplateEntity)));

                SystemEmailLogic.Start(sb, dqm);
                EmailMasterTemplateLogic.Start(sb, dqm);

                dqm.RegisterQuery(typeof(EmailTemplateEntity), () =>
                    from t in Database.Query<EmailTemplateEntity>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        Active = t.IsActiveNow(),
                        t.IsBodyHtml
                    });

                sb.Schema.EntityEvents<EmailTemplateEntity>().PreSaving += new PreSavingEventHandler<EmailTemplateEntity>(EmailTemplate_PreSaving);
                sb.Schema.EntityEvents<EmailTemplateEntity>().Retrieved += EmailTemplateLogic_Retrieved;

                Validator.OverridePropertyValidator((EmailTemplateMessageEntity m) => m.Text).StaticPropertyValidation +=
                    EmailTemplateMessageText_StaticPropertyValidation;

                Validator.OverridePropertyValidator((EmailTemplateMessageEntity m) => m.Subject).StaticPropertyValidation +=
                    EmailTemplateMessageSubject_StaticPropertyValidation;

                EmailTemplateGraph.Register();

                GlobalValueProvider.RegisterGlobalVariable("UrlLeft", _ => EmailLogic.Configuration.UrlLeft);
                GlobalValueProvider.RegisterGlobalVariable("Now", _ => TimeZoneManager.Now);
                GlobalValueProvider.RegisterGlobalVariable("Today", _ => TimeZoneManager.Now.Date, "d");

                sb.Schema.Synchronizing += Schema_Synchronize_Tokens;
                sb.Schema.Synchronizing += Schema_Syncronize_DefaultTemplates;

                sb.Schema.Table<SystemEmailEntity>().PreDeleteSqlSync += EmailTemplateLogic_PreDeleteSqlSync;

                Validator.PropertyValidator<EmailTemplateEntity>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (et.Active && !et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
        }

        static SqlPreCommand EmailTemplateLogic_PreDeleteSqlSync(Entity arg)
        {
            SystemEmailEntity systemEmail = (SystemEmailEntity)arg;

            var emailTemplates = Administrator.UnsafeDeletePreCommand(Database.Query<EmailTemplateEntity>().Where(et => et.SystemEmail == systemEmail));

            return emailTemplates;
        }

        public static EmailTemplateEntity ParseData(this EmailTemplateEntity emailTemplate)
        {
            if (!emailTemplate.IsNew || emailTemplate.queryName == null)
                throw new InvalidOperationException("emailTemplate should be new and have queryName");

            emailTemplate.Query = QueryLogic.GetQueryEntity(emailTemplate.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(emailTemplate.queryName);

            emailTemplate.ParseData(description);

            return emailTemplate;
        }

        static void EmailTemplateLogic_Retrieved(EmailTemplateEntity emailTemplate)
        {
            using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(emailTemplate.Query.Key);
                QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

                using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
                    emailTemplate.ParseData(description);
            }
        }

        static string EmailTemplateMessageText_StaticPropertyValidation(EmailTemplateMessageEntity message, PropertyInfo pi)
        {
            if (message.TextParsedNode as EmailTemplateParser.BlockNode == null)
            {
                try
                {
                    string errorMessage;
                    message.TextParsedNode = ParseTemplate(message.Template, message.Text, out errorMessage);
                    return errorMessage.DefaultText(null);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        static string EmailTemplateMessageSubject_StaticPropertyValidation(EmailTemplateMessageEntity message, PropertyInfo pi)
        {
            if (message.SubjectParsedNode as EmailTemplateParser.BlockNode == null)
            {
                try
                {
                    string errorMessage;
                    message.SubjectParsedNode = ParseTemplate(message.template, message.Subject, out errorMessage);
                    return errorMessage.DefaultText(null);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        private static EmailTemplateParser.BlockNode ParseTemplate(EmailTemplateEntity template, string text, out string errorMessage)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();
                return EmailTemplateParser.TryParse(text, qd, template.SystemEmail.ToType(), out errorMessage);
            }
        }

        static void EmailTemplate_PreSaving(EmailTemplateEntity template, ref bool graphModified)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                var queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();

                foreach (var message in template.Messages)
                {
                    message.Text = EmailTemplateParser.Parse(message.Text, qd, template.SystemEmail.ToType()).ToString();
                    message.Subject = EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType()).ToString();
                }
            }
        }

        public static IEnumerable<EmailMessageEntity> CreateEmailMessage(this Lite<EmailTemplateEntity> liteTemplate, IEntity entity, ISystemEmail systemEmail = null)
        {
            EmailTemplateEntity template = EmailTemplatesLazy.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".FormatWith(liteTemplate));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
                return new EmailMessageBuilder(template, entity, systemEmail).CreateEmailMessageInternal().ToList();
        }

        class EmailTemplateGraph : Graph<EmailTemplateEntity>
        {
            static bool registered;
            public static bool Registered { get { return registered; } }

            public static void Register()
            {
                new Construct(EmailTemplateOperation.Create)
                {
                    Construct = _ => new EmailTemplateEntity 
                    { 
                        MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate(),
                    }
                }.Register();

                new Execute(EmailTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();

                new Execute(EmailTemplateOperation.Enable) 
                {
                    CanExecute = t => t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyActive.NiceToString() : null,
                    Execute = (t, _) => t.Active = true
                }.Register();

                new Execute(EmailTemplateOperation.Disable) 
                {
                    CanExecute = t => !t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyInactive.NiceToString() : null,
                    Execute = (t, _) => t.Active = false
                }.Register();

                registered = true;
            }
        }

        static SqlPreCommand Schema_Synchronize_Tokens(Replacements replacements)
        {
            if (AvoidSynchronize)
                return null;

            StringDistance sd = new StringDistance();

            var emailTemplates = Database.Query<EmailTemplateEntity>().ToList();

            var table = Schema.Current.Table(typeof(EmailTemplateEntity));

            SqlPreCommand cmd = emailTemplates.Select(uq => EmailTemplateParser.ProcessEmailTemplate(replacements, table, uq, sd)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand Schema_Syncronize_DefaultTemplates(Replacements replacements)
        {
            if (AvoidSynchronize)
                return null;

            var table = Schema.Current.Table(typeof(EmailTemplateEntity));

            var systemEmails = Database.Query<SystemEmailEntity>().Where(se => !se.EmailTemplates().Any(a => a.Active)).ToList();

            string cis = Database.Query<CultureInfoEntity>().Select(a => a.Name).ToString(", ").Etc(60);

            if (!systemEmails.Any())
                return null;

            if (!replacements.Interactive || !SafeConsole.Ask("{0}\r\n have no EmailTemplates. Create in {1}?".FormatWith(systemEmails.ToString("\r\n"), cis.DefaultText("No CultureInfos registered!"))))
                return null;

            using (replacements.WithReplacedDatabaseName())
            {
                var cmd = systemEmails.Select(se =>
                {
                    try
                    {
                        return table.InsertSqlSync(SystemEmailLogic.CreateDefaultTemplate(se), includeCollections: true);
                    }
                    catch (Exception e)
                    {
                        return new SqlPreCommandSimple("Exception on SystemEmail {0}: {1}".FormatWith(se, e.Message));
                    }
                }).Combine(Spacing.Double);

                if (cmd != null)
                    return SqlPreCommand.Combine(Spacing.Double, new SqlPreCommandSimple("DECLARE @parentId INT"), cmd);

                return cmd;
            }
        }

        public static void GenerateDefaultTemplates()
        {
            var systemEmails = Database.Query<SystemEmailEntity>().Where(se => !se.EmailTemplates().Any(a => a.Active)).ToList();

            List<string> exceptions = new List<string>();

            foreach (var se in systemEmails)
            {
                try
                {
                    SystemEmailLogic.CreateDefaultTemplate(se).Save();
                }
                catch (Exception ex)
                {
                    exceptions.Add("{0} in {1}:\r\n{2}".FormatWith(ex.GetType().Name, se.FullClassName, ex.Message.Indent(4)));
                }
            }

            if (exceptions.Any())
                throw new Exception(exceptions.ToString("\r\n\r\n"));
        }

        public static void Regenerate(EmailTemplateEntity et)
        {
            EmailTemplateParser.Regenerate(et, null, Schema.Current.Table<EmailTemplateEntity>()).ExecuteLeaves();
        }
    }
}
