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
using Signum.Entities.Operations;
using Signum.Engine.Operations;
using Signum.Engine.Extensions.Properties;
using System.Net;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Web;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        bool IsPlainText { get; set; }
    }

    public interface IEmailModel
    {
        IEmailOwnerDN To { get; set; }
        string Cc { get; set; }
        string Bcc { get; set; }  
    }

    public class EmailModel<T> : IEmailModel
        where T : IEmailOwnerDN
    {
        public T To;

        IEmailOwnerDN IEmailModel.To
        {
            get { return To; }
            set { To = (T)value; }
        }

        public string Cc { get; set; }
        public string Bcc { get; set; }  
    }

    public static class EmailLogic
    {
        public static string DoNotSend = "null@null.com";

        public static Func<string> OverrideEmailAddress = () => null;

        [ThreadStatic]
        static string overrideEmailAddressForProcess;
        internal static IDisposable OverrideEmailAddressForProcess(string emailAddress)
        {
            var old = overrideEmailAddressForProcess;
            overrideEmailAddressForProcess = emailAddress;
            return new Disposable(() => overrideEmailAddressForProcess = old);
        }

        internal static string OnEmailAddress()
        {
            if (overrideEmailAddressForProcess.HasText())
                return overrideEmailAddressForProcess;

            return OverrideEmailAddress();
        }

        public static Func<SmtpClient> SmtpClientBuilder;

        static Dictionary<Type, Func<IEmailModel, EmailContent>> templates = new Dictionary<Type, Func<IEmailModel, EmailContent>>();
        static Dictionary<Type, Lite<EmailTemplateDN>> templateToDN;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();

                dqm[typeof(EmailTemplateDN)] = (from e in Database.Query<EmailTemplateDN>()
                                                select new
                                                {
                                                    Entity = e.ToLite(),
                                                    e.Id,
                                                    e.FullClassName,
                                                    e.FriendlyName,
                                                }).ToDynamic();

                dqm[typeof(EmailMessageDN)] = (from e in Database.Query<EmailMessageDN>()
                                               select new
                                               {
                                                   Entity = e.ToLite(),
                                                   e.Id,
                                                   e.Recipient,
                                                   e.State,
                                                   e.Subject,
                                                   e.Body,
                                                   Template = e.Template,
                                                   e.Sent,
                                                   e.Received,
                                                   e.Package,
                                                   e.Exception,
                                               }).ToDynamic();

                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += Schema_Initializing;
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;
            }
        }

        #region database management
        static void Schema_Initializing()
        {
            List<EmailTemplateDN> dbTemplates = Database.RetrieveAll<EmailTemplateDN>();

            templateToDN = EnumerableExtensions.JoinStrict(
                dbTemplates, templates.Keys, t => t.FullClassName, t => t.FullName,
                (typeDN, type) => new { typeDN, type }, "caching EmailTemplates").ToDictionary(a => a.type, a => a.typeDN.ToLite());
        }

        static readonly string EmailTemplates = "EmailTemplates";


        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<EmailTemplateDN>();

            Dictionary<string, EmailTemplateDN> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, EmailTemplateDN> old = Administrator.TryRetrieveAll<EmailTemplateDN>(replacements).ToDictionary(c => c.FullClassName);

            replacements.AskForReplacements(old, should, EmailTemplates);

            Dictionary<string, EmailTemplateDN> current = replacements.ApplyReplacements(old, EmailTemplates);

            return Synchronizer.SynchronizeScript(
                current,
                should,
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s) => table.InsertSqlSync(s),
                (tn, c, s) =>
                {
                    c.FullClassName = s.FullClassName;
                    c.FriendlyName = s.FriendlyName;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<EmailTemplateDN>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<EmailTemplateDN> GenerateTemplates()
        {
            var lista = (from type in templates.Keys
                         select new EmailTemplateDN
                         {
                             FullClassName = type.FullName,
                             FriendlyName = type.NiceName()
                         }).ToList();
            return lista;
        }
        #endregion

        public static void StarProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailPackageDN>();

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(EmailProcesses.SendEmails, new SendEmailProcessAlgorithm());

                new BasicConstructFromMany<EmailMessageDN, ProcessExecutionDN>(EmailOperations.ReSendEmails)
                {
                    Construct = (messages, args) => ProcessLogic.Create(EmailProcesses.SendEmails, messages)
                }.Register();

                dqm[typeof(EmailPackageDN)] = (from e in Database.Query<EmailPackageDN>()
                                               select new
                                               {
                                                   Entity = e.ToLite(),
                                                   e.Id,
                                                   e.Name,
                                                   e.NumLines,
                                                   e.NumErrors,
                                               }).ToDynamic();
            }
        }

        public static void RegisterTemplate<T>(Func<T, EmailContent> template)
            where T : IEmailModel
        {
            templates[typeof(T)] = et => template((T)et);
        }

        public static Lite<EmailTemplateDN> GetTemplateDN(Type type)
        {
            return templateToDN.GetOrThrow(type, Resources.NotRegisteredInEmailLogic);
        }

        public static Func<IEmailModel, EmailContent> GetTemplate(Type type)
        {
            return templates.GetOrThrow(type, Resources.NotRegisteredInEmailLogic);
        }

        public static EmailMessageDN CreateEmailMessage(IEmailModel model, Lite<EmailPackageDN> package)
        {

            if (model == null)
                throw new ArgumentNullException("model");

            if (model.To == null)
                throw new ArgumentNullException("model.To");


            using (Sync.ChangeBothCultures(model.To.CultureInfo))
            {
                EmailContent content = GetTemplate(model.GetType())(model);

                var result = new EmailMessageDN
                {
                    State = EmailState.Created,
                    Recipient = model.To.ToLite(),
                    Bcc = model.Bcc,
                    Cc = model.Cc,
                    Template = GetTemplateDN(model.GetType()),
                    Subject = content.Subject,
                    Body = content.Body,
                    Package = package
                };
                return result;
            }
        }

        public static EmailMessageDN Send(this IEmailModel model)
        {
            EmailMessageDN result = CreateEmailMessage(model, null);

            SendMail(result);

            return result;
        }

        public static void SendMail(EmailMessageDN emailMessage)
        {
            try
            {
                MailMessage message = CreateMailMessage(emailMessage);

                if (message != null)
                {
                    SmtpClient client = SmtpClientBuilder == null ? new SmtpClient() : SmtpClientBuilder();
                    client.Send(message);
                }

                emailMessage.State = EmailState.Sent;
                emailMessage.Sent = TimeZoneManager.Now;
                emailMessage.Received = null;
                emailMessage.Save();
            }
            catch (Exception e)
            {
                emailMessage.Exception = e.Message;
                emailMessage.State = EmailState.SentError;
                emailMessage.Save();
                throw;
            }
        }

        public static void SendAsync(this IEmailModel model)
        {
            EmailMessageDN message = CreateEmailMessage(model, null);

            SendMailAsync(message);
        }

        class EmailUser
        {
            public EmailMessageDN EmailMessage;
            public UserDN User;
        }

        public static void SendMailAsync(EmailMessageDN emailMessage)
        {
            try
            {
                MailMessage message = CreateMailMessage(emailMessage);
                if (message != null)
                {
                    SmtpClient client = SmtpClientBuilder == null ? new SmtpClient() : SmtpClientBuilder();
                    client.SendCompleted += new SendCompletedEventHandler(client_SendCompleted);

                    emailMessage.Sent = null;
                    emailMessage.Received = null;
                    emailMessage.Save();

                    client.SendAsync(message, new EmailUser { EmailMessage = emailMessage, User = UserDN.Current });
                }
                else
                {
                    emailMessage.Received = null;
                    emailMessage.State = EmailState.Sent;
                    emailMessage.Sent = TimeZoneManager.Now;
                    emailMessage.Save();
                }
            }
            catch (Exception e)
            {
                emailMessage.Sent = TimeZoneManager.Now;
                emailMessage.State = EmailState.SentError;
                emailMessage.Exception = e.Message;
                emailMessage.Save();
            }
        }

        static void client_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            EmailUser emailUser = (EmailUser)e.UserState;
            EmailMessageDN em = emailUser.EmailMessage;
            if (e.Error != null)
            {
                em.Exception = e.Error.Message;
                em.State = EmailState.SentError;
            }
            else
            {
                em.State = EmailState.Sent;
                em.Sent = TimeZoneManager.Now;
            }
            using (AuthLogic.User(emailUser.User))
                em.Save();
        }

        static MailMessage CreateMailMessage(EmailMessageDN emailMessage)
        {
            var address = OnEmailAddress();

            if (address == DoNotSend)
                return null;

            MailMessage message = new MailMessage()
            {
                To = { address ?? emailMessage.Recipient.Retrieve().Email },
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = true,
            };

            if(emailMessage.Bcc.HasText())
                message.Bcc.AddRange( emailMessage.Bcc.Split(';').Select(a => new MailAddress(a)).ToList());
            if (emailMessage.Cc.HasText())
                message.Bcc.AddRange(emailMessage.Cc.Split(';').Select(a => new MailAddress(a)).ToList());
            return message;
        }

        public static ProcessExecutionDN SendAll<T>(List<T> emails)
            where T : IEmailModel
        {
            EmailPackageDN package = new EmailPackageDN
            {
                NumLines = emails.Count,
                OverrideEmailAddress = OnEmailAddress()
            }.Save();

            var packLite = package.ToLite();

            emails.Select(e => CreateEmailMessage(e, packLite)).SaveList();

            var process = ProcessLogic.Create(EmailProcesses.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static ProcessExecutionDN SendToMany<T>(EmailModel<T> model, List<T> recipientList)
            where T : class, IEmailOwnerDN
        {
            if (model.To != null)
                throw new InvalidOperationException("model should have no To");

            EmailContent content = GetTemplate(model.GetType())(model);
            var template = GetTemplateDN(model.GetType());

            EmailPackageDN package = new EmailPackageDN
            {
                NumLines = recipientList.Count,
                OverrideEmailAddress = EmailLogic.OnEmailAddress()
            }.Save();

            var lite = package.ToLite();

            recipientList.Select(to => new EmailMessageDN
            {
                State = EmailState.Created,
                Recipient = to.ToLite<IEmailOwnerDN>(),
                Template = template,
                Subject = content.Subject,
                Body = content.Body,
                Package = lite
            }).SaveList();

            var process = ProcessLogic.Create(EmailProcesses.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static Dictionary<Type, Exception> GetAllErrors()
        {
            Dictionary<Type, Exception> exceptions = new Dictionary<Type, Exception>();

            foreach (var item in templates)
            {
                try
                {
                    item.Value((IEmailModel)Activator.CreateInstance(item.Key));
                }
                catch (Exception e)
                {
                    exceptions.Add(item.Key, e);
                }
            }

            return exceptions;
        }
    }

    public struct Link
    {
        public readonly string Url;
        public readonly string Content;

        public Link(string url, string content)
        {
            this.Url = url;
            this.Content = content;
        }

        public override string ToString()
        {
            return @"<a href='{0}'>{1}</a>".Formato(Url, HttpUtility.HtmlEncode(Content));
        }
    }

    public static class SMTPConfigurationLogic
    {
        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => SMTPConfigurationLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMTPConfigurationDN>();
                sb.Schema.EntityEvents<SMTPConfigurationDN>().Saving += new SavingEventHandler<SMTPConfigurationDN>(EmailClientSettingsLogic_Saving);

                dqm[typeof(SMTPConfigurationDN)] = (from s in Database.Query<SMTPConfigurationDN>()
                                                    select new
                                                    {
                                                        Entity = s.ToLite(),
                                                        s.Id,
                                                        s.Name,
                                                        s.Host,
                                                        s.Port,
                                                        s.UseDefaultCredentials,
                                                        s.Username,
                                                        s.Password,
                                                        s.EnableSSL
                                                    }).ToDynamic();

                dqm[SMTPConfigurationQueries.NoCredentialsData] = (from s in Database.Query<SMTPConfigurationDN>()
                                                                   select new
                                                                   {
                                                                       Entity = s.ToLite(),
                                                                       s.Id,
                                                                       s.Name,
                                                                       s.Host,
                                                                       s.Port,
                                                                       s.UseDefaultCredentials,
                                                                       s.EnableSSL
                                                                   }).ToDynamic();

                dqm[typeof(ClientCertificationFileDN)] = (from c in Database.Query<ClientCertificationFileDN>()
                                                          select new 
                                                          { 
                                                            Entity = c.ToLite(),
                                                            c.Id,
                                                            c.Name,
                                                            CertFileType = c.CertFileType.NiceToString(),
                                                            c.FullFilePath
                                                          }).ToDynamic();

                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += SetCache;
            }
        }

        static void EmailClientSettingsLogic_Saving(SMTPConfigurationDN ident)
        {
            if (ident.Modified.Value)
                Transaction.RealCommit += () => smtpConfigurations = null;
        }

        static void SetCache()
        {
            smtpConfigurations = Database.RetrieveAll<SMTPConfigurationDN>().ToDictionary(s => s.Name);
        }

        static Dictionary<string, SMTPConfigurationDN> smtpConfigurations;
        public static Dictionary<string, SMTPConfigurationDN> SmtpConfigurations
        {
            get
            {
                if (smtpConfigurations == null)
                    SetCache();
                return SMTPConfigurationLogic.smtpConfigurations;
            }
        }

        public static SmtpClient GenerateSmtpClient(string smtpSettingsName, bool defaultIfNotPresent)
        {
            var settings = SmtpConfigurations.TryGet(smtpSettingsName, null);
            if (settings == null)
                if (defaultIfNotPresent)
                    return new SmtpClient();
                else
                    throw new ArgumentException("The setting {0} was not found in the SMTP settings cache".Formato(smtpSettingsName));

            SmtpClient client = new SmtpClient()
            {
                Host = settings.Host,
                Port = settings.Port,
                UseDefaultCredentials = settings.UseDefaultCredentials,
                Credentials = settings.Username.HasText() ? new NetworkCredential(settings.Username, settings.Password) : null,
                EnableSsl = settings.EnableSSL,
            };
            foreach (var cc in settings.ClientCertificationFiles)
            {
                client.ClientCertificates.Add(cc.CertFileType == CertFileType.CertFile ?
                    X509Certificate.CreateFromCertFile(cc.FullFilePath)
                    : X509Certificate.CreateFromSignedFile(cc.FullFilePath));
            }

            return client;
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SMTPConfigurationDN> config)
        {
            return GenerateSmtpClient(config.ToString(), false);
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SMTPConfigurationDN> config, bool defaultIfNotPresent)
        {
            return GenerateSmtpClient(config.TryCC(c => c.ToString()), defaultIfNotPresent);
        }
    }
}
