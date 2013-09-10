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

namespace Signum.Engine.Mailing
{
    public static class EmailTemplateLogic
    {
        static Expression<Func<SystemEmailDN, IQueryable<EmailTemplateDN>>> EmailTemplatesExpression =
            se => Database.Query<EmailTemplateDN>().Where(et => et.SystemEmail == se);
        public static IQueryable<EmailTemplateDN> EmailTemplates(this SystemEmailDN se)
        {
            return EmailTemplatesExpression.Evaluate(se);
        }

        public static Func<EmailMasterTemplateDN> CreateDefaultMasterTemplate;
     
        public static ResetLazy<Dictionary<Lite<EmailTemplateDN>, EmailTemplateDN>> EmailTemplatesLazy; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailTemplateDN>();                
                sb.Include<EmailMasterTemplateDN>();

                EmailTemplatesLazy = sb.GlobalLazy(() => Database.Query<EmailTemplateDN>()
                    .Where(et => et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    .ToDictionary(et => et.ToLite()), new InvalidateWith(typeof(EmailTemplateDN)));

                SystemEmailLogic.Start(sb, dqm);

                dqm.RegisterQuery(typeof(EmailMasterTemplateDN), () =>
                 from t in Database.Query<EmailMasterTemplateDN>()
                 select new
                 {
                     Entity = t,
                     t.Id,
                     t.Name,
                 });

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
                EmailMasterTemplateGraph.Register();

                EmailTemplateParser.GlobalVariables.Add("UrlLeft", _ => EmailLogic.Configuration.UrlLeft);

                sb.Schema.Synchronizing += Schema_Synchronize_Tokens;
                sb.Schema.Synchronizing += Schema_Syncronize_DefaultTemplates;

                Validator.PropertyValidator<EmailTemplateDN>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (et.Active && !et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
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
            EmailTemplateParser.BlockNode parsedNode = message.TextParsedNode as EmailTemplateParser.BlockNode;

            if (parsedNode == null)
            {
                try
                {
                    parsedNode = ParseTemplate(message, message.Text);
                    message.TextParsedNode = parsedNode;
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
            EmailTemplateParser.BlockNode parsedNode = message.SubjectParsedNode as EmailTemplateParser.BlockNode;

            if (parsedNode == null)
            {
                try
                {
                    parsedNode = ParseTemplate(message, message.Subject);
                    message.SubjectParsedNode = parsedNode;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        private static EmailTemplateParser.BlockNode ParseTemplate(EmailTemplateMessageDN message, string text)
        {
            using (message.Template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = QueryLogic.ToQueryName(message.Template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();
                return EmailTemplateParser.Parse(text, qd, message.Template.SystemEmail.ToType());
            }
        }

        static void EmailTemplate_PreSaving(EmailTemplateDN template, ref bool graphModified)
        {
            graphModified |= UpdateTokens(template);
        }

        public static bool UpdateTokens(EmailTemplateDN template)
        {
            using (template.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                var queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                List<QueryToken> list = new List<QueryToken>();

                if (template.From != null)
                    list.Add(template.From.Token.Token);

                foreach (var tr in template.Recipients.Where(r => r.Token != null))
                {
                    list.Add(tr.Token.Token);
                }

                foreach (var message in template.Messages)
                {
                    EmailTemplateParser.Parse(message.Text, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
                    EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
                }

                var tokens = list.Distinct();

                if (template.Tokens.Any(t => t.ParseException != null) || !template.Tokens.Select(a => a.Token).ToList().SequenceEqual(tokens))
                {
                    template.Tokens.ResetRange(tokens.Select(t => new QueryTokenDN(t)));
                    return true;
                }
                return false;
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
                        MasterTemplate = EmailTemplateLogic.GetDefaultMasterTemplate(),
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

        class EmailMasterTemplateGraph : Graph<EmailMasterTemplateDN>
        {
            public static void Register()
            {
                new Construct(EmailMasterTemplateOperation.Create)
                {
                    Construct = _ => CreateDefaultMasterTemplate == null ? 
                        new EmailMasterTemplateDN { }:
                        CreateDefaultMasterTemplate()
                }.Register();

                new Execute(EmailMasterTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();
            }
        }

        public static Lite<EmailMasterTemplateDN> GetDefaultMasterTemplate()
        {
            var result = Database.Query<EmailMasterTemplateDN>().Select(emt => emt.ToLite()).SingleEx();

            if (result != null)
                return result;

            if (CreateDefaultMasterTemplate == null)
                return null;

            var newTemplate = CreateDefaultMasterTemplate();

            return newTemplate.Save().ToLite();
        }

        static SqlPreCommand Schema_Synchronize_Tokens(Replacements replacements)
        {
            var emailTemplates = Database.Query<EmailTemplateDN>().ToList();

            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            SqlPreCommand cmd = emailTemplates.Select(uq => ProcessEmailTemplate(replacements, table, uq)).Combine(Spacing.Double);

            return cmd;
        }

        static SqlPreCommand Schema_Syncronize_DefaultTemplates(Replacements replacements)
        {
            var table = Schema.Current.Table(typeof(EmailTemplateDN));

            var systemEmails = Database.Query<SystemEmailDN>().Where(se => !se.EmailTemplates().Any(a => a.Active)).ToList();

            string cis = Database.Query<CultureInfoDN>().Select(a => a.Name).ToString(", ").Etc(60);

            if (!systemEmails.Any() || !SafeConsole.Ask("{0}\r\n have no EmailTemplates. Create in {1}?".Formato(systemEmails.ToString("\r\n"), cis.DefaultText("No CultureInfos registered!"))))
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

        static SqlPreCommand ProcessEmailTemplate(Replacements replacements, Table table, EmailTemplateDN et)
        {
            try
            {
                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name);
                Console.WriteLine(" Query: " + et.Query.Key);

                if (et.Tokens.Any(a => a.ParseException != null))
                    using (et.DisableAuthorization ? ExecutionMode.Global() : null)
                    {
                        QueryDescription qd = DynamicQueryManager.Current.QueryDescription(et.Query.ToQueryName());

                        if (et.Tokens.Any())
                        {
                            Console.WriteLine(" Tokens:");
                            foreach (var item in et.Tokens.ToList())
                            {
                                QueryTokenDN token = item;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, false, "", allowRemoveToken: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et);
                                    case FixTokenResult.RemoveToken: throw new InvalidOperationException("Unexpected RemoveToken");
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix: EmailTemplateParser.ReplaceToken(et, item, token); break;
                                    default: break;
                                }
                            }
                        }
                    }

                Console.Clear();

                return table.UpdateSqlSync(et, includeCollections: true);
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".Formato(et.BaseToString(), e.Message));
            }
        }
    }
}
