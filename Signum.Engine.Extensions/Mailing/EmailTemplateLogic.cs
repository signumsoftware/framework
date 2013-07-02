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
                     t.State
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
            var queryname = QueryLogic.ToQueryName(template.Query.Key);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryname);

            List<QueryToken> list = new List<QueryToken>();

            foreach (var tr in template.Recipients.Where(r => r.Token != null))
            {
                list.Add(QueryUtils.Parse(".".Combine("Entity", tr.Token.TokenString, "EmailOwnerData"), qd, false));
            }

            foreach (var message in template.Messages)
            {
                EmailTemplateParser.Parse(message.Text, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
                EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType()).FillQueryTokens(list);
            }

            var tokens = list.Distinct();

            if (!template.Tokens.Select(a => a.Token).ToList().SequenceEqual(tokens))
            {
                template.Tokens.ResetRange(tokens.Select(t => new QueryTokenDN(t)));
                graphModified = true;
            }
        }

        public static EmailMessageDN CreateEmailMessage(this Lite<EmailTemplateDN> template, IIdentifiable entity)
        {
            return CreateEmailMessage(template, entity, null);
        }

        public static EmailMessageDN CreateEmailMessage(this Lite<EmailTemplateDN> liteTemplate, IIdentifiable entity, ISystemEmail systemEmail)
        {
            using (ExecutionMode.Global())
            {
                var template = EmailTemplates.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".Formato(liteTemplate));

                var queryName = QueryLogic.ToQueryName(template.Query.Key);
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                var smtpConfig = template.SmtpConfiguration ?? EmailLogic.SenderManager.DefaultSmtpConfiguration;

                var columns = GetTemplateColumns(template, template.Tokens, qd);

                var table = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
                {
                    QueryName = queryName,
                    Columns = columns,
                    Pagination = new Pagination.All(),
                    Filters = systemEmail.GetFilters(qd),
                    Orders = new List<Order>(),
                });

                var dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);

                MList<EmailOwnerRecipientData> recipients = new MList<EmailOwnerRecipientData>();
                if (systemEmail != null)
                    recipients.AddRange(systemEmail.GetRecipients());

                recipients.AddRange(template.Recipients.SelectMany(tr =>
                {
                    if (tr.Token != null)
                    {
                        var owner = dicTokenColumn.GetOrThrow(QueryUtils.Parse("Entity." + tr.Token.TokenString + ".EmailOwnerData", qd, false));

                        var groups = table.Rows.Select(r => (EmailOwnerData)r[owner]).Distinct(a => a.Owner).ToList();
                        if (groups.Count == 1 && groups[0] == null)
                            return new List<EmailOwnerRecipientData>();

                        return groups.Select(g => new EmailOwnerRecipientData(g) { Kind = tr.Kind }).ToList();
                    }
                    else
                    {
                        return new List<EmailOwnerRecipientData>
                    { 
                        new EmailOwnerRecipientData(new EmailOwnerData
                        {
                             CultureInfo = null, 
                             Email = tr.EmailAddress,
                             DisplayName = tr.DisplayName
                        }){ Kind = tr.Kind },
                    };
                    }
                }));

                if (smtpConfig != null)
                    recipients.AddRange(smtpConfig.RetrieveFromCache().AditionalRecipients.Select(r =>
                        new EmailOwnerRecipientData(r.EmailOwner.Retrieve().EmailOwnerData) { Kind = r.Kind }));

                EmailAddressDN from = null;
                if (template.From != null)
                {
                    if (template.From.Token != null)
                    {
                        var owner = dicTokenColumn.GetOrThrow(QueryUtils.Parse("Entity." + template.From.Token.TokenString + ".EmailOwnerData", qd, false));

                        var eod = table.Rows.Select(r => (EmailOwnerData)r[owner]).Distinct(a => a.Owner).SingleOrDefaultEx(() => "More than one distinct From value");

                        from = new EmailAddressDN(eod);
                    }
                    else
                    {
                        from = new EmailAddressDN(new EmailOwnerData
                        {
                            CultureInfo = null,
                            Email = template.From.EmailAddress,
                            DisplayName = template.From.DisplayName,
                        });
                    }
                }
                else if (smtpConfig != null)
                {
                    from = smtpConfig.RetrieveFromCache().DefaultFrom;
                }

                if (from == null)
                {
                    SmtpSection smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;

                    from = new EmailAddressDN
                    {
                        EmailAddress = smtpSection.From
                    };
                }

                var email = new EmailMessageDN
                {
                    Target = (Lite<IdentifiableEntity>)entity.ToLite(),
                    Recipients = recipients.Select(r => new EmailRecipientDN(r.OwnerData) { Kind = r.Kind }).ToMList(),
                    From = from,
                    IsBodyHtml = template.IsBodyHtml,
                    EditableMessage = template.EditableMessage,
                    Template = template.ToLite(),
                };

                CultureInfo ci = recipients.Where(a => a.Kind == EmailRecipientKind.To).Select(a => a.OwnerData.CultureInfo).FirstOrDefault();

                var message = template.GetCultureMessage(ci) ?? template.GetCultureMessage(EmailLogic.Configuration.DefaultCulture.CultureInfo);

                if (message == null)
                    throw new InvalidOperationException("Message {0} does not have a message for CultureInfo {0} (or Default)".Formato(template, ci));

                if (message.SubjectParsedNode == null)
                    message.SubjectParsedNode = EmailTemplateParser.Parse(message.Subject, qd, template.SystemEmail.ToType());

                email.Subject = ((EmailTemplateParser.BlockNode)message.SubjectParsedNode).Print(
                    new EmailTemplateParameters
                    {
                        Columns = dicTokenColumn,
                        IsHtml = false,
                        CultureInfo = ci,
                        Entity = entity,
                        SystemEmail = systemEmail
                    },
                    table.Rows);

                if (message.TextParsedNode == null)
                {
                    string body = message.Text;

                    if (template.MasterTemplate != null)
                        body = EmailMasterTemplateDN.MasterTemplateContentRegex.Replace(template.MasterTemplate.Retrieve().Text, m => body);

                    message.TextParsedNode = EmailTemplateParser.Parse(body, qd, template.SystemEmail.ToType());
                }

                email.Body = ((EmailTemplateParser.BlockNode)message.TextParsedNode).Print(
                    new EmailTemplateParameters
                    {
                        Columns = dicTokenColumn,
                        IsHtml = template.IsBodyHtml,
                        CultureInfo = ci,
                        Entity = entity,
                        SystemEmail = systemEmail
                    },
                    table.Rows); ;

                return email;
            }
        }

        public static List<Column> GetTemplateColumns(IdentifiableEntity context, MList<QueryTokenDN> tokens, QueryDescription queryDescription)
        {
            using (ExecutionMode.Global())
            {
                foreach (var t in tokens)
                {
                    t.ParseData(context, queryDescription, canAggregate: false);
                }
            }

            return tokens.Select(qt => new Column(qt.Token, null)).ToList();
        }


        class EmailTemplateGraph : Graph<EmailTemplateDN, EmailTemplateState>
        {
            static bool registered;
            public static bool Registered { get { return registered; } }

            public static void Register()
            {
                GetState = t => t.State;

                new Construct(EmailTemplateOperation.Create)
                {
                    ToState = EmailTemplateState.Created,
                    Construct = _ => new EmailTemplateDN 
                    { 
                        State = EmailTemplateState.Created,
                        SmtpConfiguration = EmailLogic.SenderManager.TryCC(m => m.DefaultSmtpConfiguration)
                    }
                }.Register();

                new Execute(EmailTemplateOperation.Save)
                {
                    ToState = EmailTemplateState.Modified,
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailTemplateState.Created, EmailTemplateState.Modified },
                    Execute = (t, _) => t.State = EmailTemplateState.Modified
                }.Register();

                new Execute(EmailTemplateOperation.Enable) 
                {
                    ToState = EmailTemplateState.Modified,
                    FromStates = { EmailTemplateState.Modified },
                    CanExecute = t => t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyActive.NiceToString() : null,
                    Execute = (t, _) => t.Active = true
                }.Register();

                new Execute(EmailTemplateOperation.Disable) 
                {
                    ToState = EmailTemplateState.Modified,
                    FromStates = { EmailTemplateState.Modified },
                    CanExecute = t => !t.Active ? EmailTemplateMessage.TheTemplateIsAlreadyInactive.NiceToString() : null,
                    Execute = (t, _) => t.Active = false
                }.Register();

                registered = true;
            }
        }

        class EmailMasterTemplateGraph : Graph<EmailMasterTemplateDN, EmailTemplateState>
        {
            public static void Register()
            {
                GetState = t => t.State;

                new Construct(EmailMasterTemplateOperation.Create)
                {
                    ToState = EmailTemplateState.Created,
                    Construct = _ => new EmailMasterTemplateDN { State = EmailTemplateState.Created }
                }.Register();

                new Execute(EmailMasterTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailTemplateState.Created, EmailTemplateState.Modified },
                    ToState = EmailTemplateState.Modified,
                    Execute = (t, _) => t.State = EmailTemplateState.Modified
                }.Register();
            }
        }
    }
}
