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

namespace Signum.Engine.Mailing
{
    public static class EmailTemplateLogic
    {   
        public static EmailTemplateMessageDN GetCultureMessage(this EmailTemplateDN template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }
     
        static Expression<Func<SystemEmailDN, IQueryable<EmailTemplateDN>>> EmailTemplatesExpression =
            se => Database.Query<EmailTemplateDN>().Where(et => et.SystemEmail == se);
        public static IQueryable<EmailTemplateDN> EmailTemplates(this SystemEmailDN se)
        {
            return EmailTemplatesExpression.Evaluate(se);
        }
        
        public static ResetLazy<Dictionary<Lite<EmailTemplateDN>, EmailTemplateDN>> EmailTemplatesLazy; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                sb.Include<EmailTemplateDN>();       

                EmailTemplatesLazy = sb.GlobalLazy(() => Database.Query<EmailTemplateDN>()
                    .Where(et => et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(EmailTemplateDN)));

                SystemEmailLogic.Start(sb, dqm);
                EmailMasterTemplateLogic.Start(sb, dqm);

                dqm.RegisterQuery(typeof(EmailTemplateDN), () =>
                    from t in Database.Query<EmailTemplateDN>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        Active = t.IsActiveNow(),
                        t.IsBodyHtml
                    });

                sb.Schema.EntityEvents<EmailTemplateDN>().PreSaving += new PreSavingEventHandler<EmailTemplateDN>(EmailTemplate_PreSaving);
                sb.Schema.EntityEvents<EmailTemplateDN>().Retrieved += EmailTemplateLogic_Retrieved;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Text).StaticPropertyValidation +=
                    EmailTemplateMessageText_StaticPropertyValidation;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Subject).StaticPropertyValidation +=
                    EmailTemplateMessageSubject_StaticPropertyValidation;

                EmailTemplateGraph.Register();

                EmailTemplateParser.GlobalVariables.Add("UrlLeft", _ => EmailLogic.Configuration.UrlLeft);

                sb.Schema.Synchronizing += Schema_Synchronize_Tokens;
                sb.Schema.Synchronizing += Schema_Syncronize_DefaultTemplates;

                sb.Schema.Table<SystemEmailDN>().PreDeleteSqlSync += EmailTemplateLogic_PreDeleteSqlSync;

                Validator.PropertyValidator<EmailTemplateDN>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (et.Active && !et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
        }

        static SqlPreCommand EmailTemplateLogic_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            SystemEmailDN systemEmail = (SystemEmailDN)arg;

            var emailTemplates = Administrator.UnsafeDeletePreCommand(Database.Query<EmailTemplateDN>().Where(et => et.SystemEmail == systemEmail));

            return emailTemplates;
        }

        static void EmailTemplateLogic_Retrieved(EmailTemplateDN emailTemplate)
        {
            using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(emailTemplate.Query.Key);
                QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

                using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
                    emailTemplate.ParseData(description);
            }
        }

        static string EmailTemplateMessageText_StaticPropertyValidation(EmailTemplateMessageDN message, PropertyInfo pi)
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

        static string EmailTemplateMessageSubject_StaticPropertyValidation(EmailTemplateMessageDN message, PropertyInfo pi)
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

        private static EmailTemplateParser.BlockNode ParseTemplate(EmailTemplateDN template, string text, out string errorMessage)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();
                return EmailTemplateParser.TryParse(text, qd, template.SystemEmail.ToType(), out errorMessage);
            }
        }

        static void EmailTemplate_PreSaving(EmailTemplateDN template, ref bool graphModified)
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

        public static IEnumerable<EmailMessageDN> CreateEmailMessage(this Lite<EmailTemplateDN> liteTemplate, IIdentifiable entity, ISystemEmail systemEmail = null)
        {
            EmailTemplateDN template = EmailTemplatesLazy.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".Formato(liteTemplate));

            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
                return new EmailMessageBuilder(template, entity, systemEmail).CreateEmailMessageInternal().ToList();
        }

        class EmailTemplateGraph : Graph<EmailTemplateDN>
        {
            static bool registered;
            public static bool Registered { get { return registered; } }

            public static void Register()
            {
                new Construct(EmailTemplateOperation.Create)
                {
                    Construct = _ => new EmailTemplateDN 
                    { 
                        SmtpConfiguration = SmtpConfigurationLogic.DefaultSmtpConfiguration.Value.ToLite(),
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
            StringDistance sd = new StringDistance();

            var emailTemplates = Database.Query<EmailTemplateDN>().ToList();

            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            SqlPreCommand cmd = emailTemplates.Select(uq => EmailTemplateParser.ProcessEmailTemplate(replacements, table, uq, sd)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand Schema_Syncronize_DefaultTemplates(Replacements replacements)
        {
            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            var systemEmails = Database.Query<SystemEmailDN>().Where(se => !se.EmailTemplates().Any(a => a.Active)).ToList();

            string cis = Database.Query<CultureInfoDN>().Select(a => a.Name).ToString(", ").Etc(60);

            if (!systemEmails.Any())
                return null;

            if (!SafeConsole.IsConsolePresent || !SafeConsole.Ask("{0}\r\n have no EmailTemplates. Create in {1}?".Formato(systemEmails.ToString("\r\n"), cis.DefaultText("No CultureInfos registered!"))))
                return null;

            var cmd = systemEmails
                    .Select(se =>
                    {
                        try
                        {
                            return table.InsertSqlSync(SystemEmailLogic.CreateDefaultTemplate(se), includeCollections: true);
                        }
                        catch (Exception e)
                        {
                            return new SqlPreCommandSimple("Exception on SystemEmail {0}: {1}".Formato(se, e.Message));
                        }
                    })
                    .Combine(Spacing.Double);

            if (cmd != null)
                return SqlPreCommand.Combine(Spacing.Double, new SqlPreCommandSimple("DECLARE @idParent INT"), cmd);

            return cmd;
        }

     
    }
}
