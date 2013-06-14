using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using System.Net;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Web;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Linq.Expressions;
using Signum.Engine.Exceptions;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using System.Globalization;
using Signum.Engine.Translation;
using System.Net.Configuration;
using System.Configuration;
using Signum.Entities.UserQueries;

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        bool IsPlainText { get; set; }
    }

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

    public static class EmailLogic
    {
        public static string DoNotSend = "null@null.com";

        public static Func<string> OverrideEmailAddress = () => null;

        public static Func<EmailMessageDN, SmtpClient> SmtpClientBuilder;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null, null)));
        }

        private static Func<EmailLogicConfigurationDN> EmailLogicConfiguration;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<EmailLogicConfigurationDN> emailLogicConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                EmailLogicConfiguration = emailLogicConfiguration;

                sb.Include<EmailMessageDN>();
                sb.Include<EmailTemplateDN>();
                sb.Include<EmailMasterTemplateDN>();

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

                dqm.RegisterQuery(typeof(EmailMessageDN), () => 
                    from e in Database.Query<EmailMessageDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.State,
                        e.Subject,
                        e.Text,
                        e.Template,
                        e.Sent,
                        e.Received,
                        e.Package,
                        e.Exception,
                    });

                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += Schema_Initializing;
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Schema.EntityEvents<EmailTemplateDN>().PreSaving += new PreSavingEventHandler<EmailTemplateDN>(EmailTemplate_PreSaving);
                sb.Schema.EntityEvents<EmailTemplateDN>().Retrieved += EmailTemplateLogic_Retrieved;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Text).StaticPropertyValidation += 
                    EmailTemplateMessageText_StaticPropertyValidation;

                Validator.OverridePropertyValidator((EmailTemplateMessageDN m) => m.Subject).StaticPropertyValidation +=
                    EmailTemplateMessageSubject_StaticPropertyValidation;
                
                EmailTemplateGraph.Register();
                EmailMasterTemplateGraph.Register();
            }
        }

        static void EmailTemplateLogic_Retrieved(EmailTemplateDN emailTemplate)
        {
            object queryName = QueryLogic.ToQueryName(emailTemplate.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

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
            return EmailTemplateParser.Parse(text, s => QueryUtils.Parse("Entity." + s, qd, false), message.Template.SystemEmail.ToType());
        }

        static Dictionary<Type, Func<EmailTemplateDN>> systemEmails =
            new Dictionary<Type, Func<EmailTemplateDN>>();
        static Dictionary<Type, SystemEmailDN> systemEmailToDN;
        static Dictionary<SystemEmailDN, Type> systemEmailToType;

        public static void RegisterEmailModel<T>(Func<EmailTemplateDN> defaultTemplateConstructor = null)
            where T : ISystemEmail
        {
            RegisterEmailModel(typeof(T), defaultTemplateConstructor);
        }

        public static void RegisterEmailModel(Type model, Func<EmailTemplateDN> defaultTemplateConstructor = null)
        {
            systemEmails[model] = defaultTemplateConstructor;
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
                EmailTemplateParser.Parse(message.Text, s => QueryUtils.Parse("Entity." + s, qd, false), template.SystemEmail.ToType()).FillQueryTokens(list);
                EmailTemplateParser.Parse(message.Subject, s => QueryUtils.Parse("Entity." + s, qd, false), template.SystemEmail.ToType()).FillQueryTokens(list);
            }

            var tokens = list.Distinct();

            if (!template.Tokens.Select(a => a.Token).ToList().SequenceEqual(tokens))
            {
                template.Tokens.ResetRange(tokens.Select(t => new QueryTokenDN(t)));
                graphModified = true;
            }
        }

        #region database management
        static void Schema_Initializing()
        {
            var dbTemplates = Database.RetrieveAll<SystemEmailDN>();

            systemEmailToDN = EnumerableExtensions.JoinStrict(
                dbTemplates, systemEmails.Keys, typeDN => typeDN.FullClassName, type => type.FullName,
                (typeDN, type) => KVP.Create(type, typeDN), "caching EmailTemplates").ToDictionary();

            systemEmailToType = systemEmailToDN.Inverse();

            var emailLogicConfiguration = EmailLogicConfiguration();
            EmailTemplateDN.DefaultCulture = emailLogicConfiguration.DefaultCulture;
            EmailTemplateParser.GlobalVariables.Add("UrlLeft", _ => emailLogicConfiguration.UrlLeft);

            SenderManager = new EmailSenderManager();
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

        #endregion

        #region Old

        //public static void StarProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        //{
        //    if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        //    {
        //        sb.Include<EmailPackageDN>();

        //        ProcessLogic.AssertStarted(sb);
        //        ProcessLogic.Register(EmailProcesses.SendEmails, new SendEmailProcessAlgorithm());

        //        new BasicConstructFromMany<EmailMessageDN, ProcessExecutionDN>(EmailOperations.ReSendEmails)
        //        {
        //            Construct = (messages, args) => ProcessLogic.Create(EmailProcesses.SendEmails, messages)
        //        }.Register();

        //        dqm[typeof(EmailPackageDN)] = (from e in Database.Query<EmailPackageDN>()
        //                                       select new
        //                                       {
        //                                           Entity = e,
        //                                           e.Id,
        //                                           e.Name,
        //                                           e.NumLines,
        //                                           e.NumErrors,
        //                                       }).ToDynamic();
        //    }
        //}

        ////public static EmailMessageDN CreateEmailMessage(IEmailModel model, Lite<EmailPackageDN> package)
        ////{

        ////    if (model == null)
        ////        throw new ArgumentNullException("model");

        ////    if (model.To == null)
        ////        throw new ArgumentNullException("model.To");


        ////    using (Sync.ChangeBothCultures(model.To.CultureInfo))
        ////    {
        ////        //EmailContent content = GetTemplate(model.GetType())(model);

        ////        var result = new EmailMessageDN
        ////        {
        ////            State = EmailState.Created,
        ////            Recipient = model.To.ToLite(),
        ////            Bcc = model.Bcc,
        ////            Cc = model.Cc,
        ////            //TemplateOld = GetTemplateDN(model.GetType()),
        ////            //Subject = content.Subject,
        ////            //Text = content.Body,
        ////            Package = package
        ////        };
        ////        return result;
        ////    }
        ////}

        ////public static EmailMessageDN Send(this IEmailModel model)
        ////{
        ////    EmailMessageDN result = CreateEmailMessage(model, null);

        ////    SendMail(result);

        ////    return result;
        ////}


        //    return exceptions;
        //}

        #endregion

        public static EmailSenderManager SenderManager;

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

            return CreateEmailMessage(template, systemEmail.UntypedEntity, systemEmail);
        }

        public static EmailMessageDN CreateEmailMessage(this EmailTemplateDN template, IIdentifiable entity)
        {
            return CreateEmailMessage(template, entity, null);
        }

        public static EmailMessageDN CreateEmailMessage(this EmailTemplateDN template, IIdentifiable entity, ISystemEmail systemEmail)
        {
            var queryName = QueryLogic.ToQueryName(template.Query.Key);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var smtpConfig = template.SMTPConfiguration ?? EmailLogic.SenderManager.DefaultSMTPConfiguration;
            
            var columns = GetTemplateColumns(template, template.Tokens, qd);

            var table = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                Columns = columns,
                Pagination = new Pagination.All(),
                Filters =  systemEmail.GetFilters(qd),
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
                Recipients = recipients.Select(r => new EmailRecipientDN(r.OwnerData) { Kind = r.Kind }).ToMList(),
                From = from,
                IsBodyHtml = template.IsBodyHtml,
                EditableMessage = template.EditableMessage,
                Template = template.ToLite(),
            };

            CultureInfo ci = recipients.Where(a => a.Kind == EmailRecipientKind.To).Select(a => a.OwnerData.CultureInfo).FirstOrDefault();

            var message = template.GetCultureMessage(ci);

            Func<string, QueryToken> parseToken = str => QueryUtils.Parse("Entity." + str, qd, false);

            if (message.SubjectParsedNode == null)
                message.SubjectParsedNode = EmailTemplateParser.Parse(message.Subject, parseToken, template.SystemEmail.ToType());

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
                message.TextParsedNode = EmailTemplateParser.Parse(message.Text, parseToken, template.SystemEmail.ToType());

            var body = ((EmailTemplateParser.BlockNode)message.TextParsedNode).Print(
                new EmailTemplateParameters
                {
                    Columns = dicTokenColumn,
                    IsHtml = template.IsBodyHtml,
                    CultureInfo = ci,
                    Entity = entity,
                    SystemEmail = systemEmail
                },
                table.Rows);

            if (template.MasterTemplate != null)
                body = EmailMasterTemplateDN.MasterTemplateContentRegex.Replace(template.MasterTemplate.Retrieve().Text, m => body);

            email.Text = body;

            return email;
        }

        public static List<Column> GetTemplateColumns(IdentifiableEntity context, MList<QueryTokenDN> tokens, QueryDescription queryDescription)
        {
            foreach (var t in tokens)
            {
                t.ParseData(context, queryDescription, canAggregate: false);
            }

            return tokens.Select(qt => new Column(qt.Token, null)).ToList();
        }

        public static void SendMail(this ISystemEmail systemEmail)
        {
            var email = systemEmail.CreateEmailMessage();
            SenderManager.Send(email);
        }

        public static void SendMail(this EmailTemplateDN template, IIdentifiable entity)
        {
            var email = template.CreateEmailMessage(entity);
            SenderManager.Send(email);
        }

        public static void SendMailAsync(this ISystemEmail systemEmail)
        {
            var email = systemEmail.CreateEmailMessage();
            SenderManager.SendAsync(email);
        }

        public static void SendMailAsync(this IIdentifiable entity, EmailTemplateDN template)
        {
            var email = template.CreateEmailMessage(entity);
            SenderManager.SendAsync(email);
        }


        public static void SafeSendMailAsync(this SmtpClient client, MailMessage message, Action<AsyncCompletedEventArgs> onComplete)
        {
            client.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
            {
                //client.Dispose(); -> the client can be used later by other messages
                message.Dispose();
                using (AuthLogic.Disable())
                {
                    try
                    {
                        onComplete(e);
                    }
                    catch (Exception ex)
                    {
                        ex.LogException();
                    }
                }
            };
            client.SendAsync(message, null);
        }

        public static SmtpClient SafeSmtpClient()
        {
            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient()
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        internal static SmtpClient SafeSmtpClient(string host, int port)
        {
            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient(host, port)
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        public static MList<EmailTemplateMessageDN> CreateMessages(Func<EmailTemplateMessageDN> func)
        {
            var list = new MList<EmailTemplateMessageDN>();
            foreach (var ci in CultureInfoLogic.ApplicationCultures)
            {
                using (Sync.ChangeBothCultures(ci))
                {
                    list.Add(func());
                }
            }
            return list;
        }
    }


    public class EmailSenderManager
    {
        public EmailSenderManager()
        {
            
        }

        protected MailMessage CreateMailMessage(EmailMessageDN email, string overrideEmailAddress)
        {
            MailMessage message = new MailMessage()
            {
                From =  email.From.ToMailAddress(),
                Subject = email.Subject,
                Body = email.Text,
                IsBodyHtml = email.IsBodyHtml,
            };

            message.To.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => ToMailAddress(r, overrideEmailAddress)).ToList());
            message.CC.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.CC).Select(r => ToMailAddress(r, overrideEmailAddress)).ToList());
            message.Bcc.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => ToMailAddress(r, overrideEmailAddress)).ToList());

            return message;
        }

        MailAddress ToMailAddress(EmailRecipientDN recipient, string overrideEmailAddress)
        {
            var address = overrideEmailAddress ?? recipient.EmailAddress;

            if (recipient.DisplayName != null)
                return new MailAddress(address, recipient.DisplayName);

            return new MailAddress(address);
        }

        public virtual void Send(EmailMessageDN email)
        {
            try
            {
                var overrideEmailAddress = EmailLogic.OverrideEmailAddress();

                if (overrideEmailAddress != EmailLogic.DoNotSend)
                {
                    SmtpClient client = CreateSmtpClient(email);

                    MailMessage message = CreateMailMessage(email, overrideEmailAddress);

                    client.Send(message);
                }

                email.State = EmailMessageState.Sent;
                email.Sent = TimeZoneManager.Now;
                email.Received = null;
                email.Save();
            }
            catch (Exception ex)
            {
                if (Transaction.InTestTransaction) //Transaction.IsTestTransaction
                    throw;

                var exLog = ex.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    email.Exception = exLog;
                    email.State = EmailMessageState.Exception;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

        public Lite<SMTPConfigurationDN> DefaultSMTPConfiguration;

        SmtpClient CreateSmtpClient(EmailMessageDN email)
        {
            if (email.Template != null)
            {
                var smtp = email.Template.InDB(t => t.SMTPConfiguration);
                if (smtp != null)
                    return smtp.GenerateSmtpClient();
            }

            if (DefaultSMTPConfiguration != null)
                return DefaultSMTPConfiguration.GenerateSmtpClient();

            return EmailLogic.SafeSmtpClient();
        }

        public virtual void SendAsync(EmailMessageDN email)
        {
            try
            {
                var overrideEmailAddress = EmailLogic.OverrideEmailAddress();

                if (overrideEmailAddress == EmailLogic.DoNotSend)
                {
                    email.State = EmailMessageState.Sent;
                    email.Sent = TimeZoneManager.Now;
                    email.Received = null;
                    email.Save();
                }
                else
                {
                    SmtpClient client = CreateSmtpClient(email);

                    MailMessage message = CreateMailMessage(email, overrideEmailAddress);

                    email.Sent = null;
                    email.Received = null;
                    email.Save();

                    client.SafeSendMailAsync(message, args =>
                    {
                        Expression<Func<EmailMessageDN, EmailMessageDN>> updater;
                        if (args.Error != null)
                        {
                            var exLog = args.Error.LogException().ToLite();
                            updater = em => new EmailMessageDN
                    {
                        Exception = exLog,
                        State = EmailMessageState.Exception
                    };
                        }
                        else
                            updater = em => new EmailMessageDN
                            {
                                State = EmailMessageState.Sent,
                                Sent = TimeZoneManager.Now
                            };

                        for (int i = 0; i < 4; i++) //to allow main thread to save email asynchronously
                        {
                            if (email.InDB().UnsafeUpdate(updater) > 0)
                                return;

                            if (i != 3)
                                Thread.Sleep(3000);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                if (Transaction.InTestTransaction) //Transaction.InTestTransaction
                    throw;

                var exLog = ex.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    email.Exception = exLog;
                    email.State = EmailMessageState.Exception;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

    }


}
