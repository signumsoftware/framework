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

namespace Signum.Engine.Mailing
{
    public static class EmailTemplateLogic
    {
        public static Func<EmailMasterTemplateDN> CreateDefaultMasterTemplate;
     
        public static ResetLazy<Dictionary<Lite<EmailTemplateDN>, EmailTemplateDN>> EmailTemplates; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailTemplateDN>();                
                sb.Include<EmailMasterTemplateDN>();

                EmailTemplates = sb.GlobalLazy(() => Database.Query<EmailTemplateDN>()
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


                Validator.PropertyValidator<EmailTemplateDN>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (!et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(EmailLogic.Configuration.DefaultCulture.DisplayName);

                    return null;
                }; 
            }
        }

        static void EmailTemplateLogic_Retrieved(EmailTemplateDN emailTemplate)
        {
            object queryName = QueryLogic.ToQueryName(emailTemplate.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            using (ExecutionMode.Global())
                emailTemplate.ParseData(description);
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
            object queryName = QueryLogic.ToQueryName(message.Template.Query.Key);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            List<QueryToken> list = new List<QueryToken>();
            return EmailTemplateParser.Parse(text, qd, message.Template.SystemEmail.ToType());
        }

        static void EmailTemplate_PreSaving(EmailTemplateDN template, ref bool graphModified)
        {
            graphModified |= UpdateTokens(template);
        }

        public static bool UpdateTokens(EmailTemplateDN template)
        {
            var queryname = QueryLogic.ToQueryName(template.Query.Key);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryname);

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

            if (template.Tokens.Any(t=>t.ParseException != null) || !template.Tokens.Select(a => a.Token).ToList().SequenceEqual(tokens))
            {
                template.Tokens.ResetRange(tokens.Select(t => new QueryTokenDN(t)));
                return true;
            }
            return false;
        }

      

        public static EmailMessageDN CreateEmailMessage(this Lite<EmailTemplateDN> liteTemplate, IIdentifiable entity, ISystemEmail systemEmail = null)
        {
            EmailTemplateDN template = GetTemplate(liteTemplate);

            if (template.SendDifferentMessages)
                throw new InvalidOperationException("{0} has SendDifferentMessages set to true. Call CreateMultipleEmailMessages instead".Formato(template));

            return new EmailMessageBuilder(template, entity, systemEmail).CreateEmailMessageInternal().Single();
        }

        public static IEnumerable<EmailMessageDN> CreateMultipleEmailMessages(this Lite<EmailTemplateDN> liteTemplate, IIdentifiable entity, ISystemEmail systemEmail = null)
        {
            EmailTemplateDN template = GetTemplate(liteTemplate);

            if (!template.SendDifferentMessages)
                throw new InvalidOperationException("{0} has SendDifferentMessages set to false. Call CreateEmailMessage instead".Formato(template));

            return new EmailMessageBuilder(template, entity, systemEmail).CreateEmailMessageInternal().ToList();
        }

        static EmailTemplateDN GetTemplate(Lite<EmailTemplateDN> liteTemplate)
        {
            return EmailTemplates.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".Formato(liteTemplate));
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
    }
}
