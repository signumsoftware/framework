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
using Signum.Engine.Extensions.Properties;
using System.Net;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Web;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Linq.Expressions;
using Signum.Engine.Exceptions;

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

        public static Func<EmailMessageDN, SmtpClient> SmtpClientBuilder;

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
                                                    Entity = e,
                                                    e.Id,
                                                    e.FullClassName,
                                                    e.FriendlyName,
                                                }).ToDynamic();

                dqm[typeof(EmailMessageDN)] = (from e in Database.Query<EmailMessageDN>()
                                               select new
                                               {
                                                   Entity = e,
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

                new BasicExecute<EmailTemplateDN>(EmailTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (et, _) => { },
                }.Register();
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

            return Synchronizer.SynchronizeScript(should, current, 
                (tn, s) => table.InsertSqlSync(s), 
                (tn, c) => table.DeleteSqlSync(c), 
                (tn, s, c) =>
                {
                    c.FullClassName = s.FullClassName;
                    c.FriendlyName = s.FriendlyName;
                    return table.UpdateSqlSync(c);
                }, 
                Spacing.Double);
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

        public static void RegisterTemplate<T>(Func<T, EmailContent> template)
            where T : IEmailModel
        {
            templates[typeof(T)] = et => template((T)et);
        }

        public static Lite<EmailTemplateDN> GetTemplateDN(Type type)
        {
            return templateToDN.GetOrThrow(type, "{0} not registered in EmailLogic");
        }

        public static Func<IEmailModel, EmailContent> GetTemplate(Type type)
        {
            return templates.GetOrThrow(type, "{0} not registered in EmailLogic");
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
                    State = EmailMessageState.Created,
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
                    SmtpClient client = SmtpClientBuilder == null ? SafeSmtpClient() : SmtpClientBuilder(emailMessage);
                    client.Send(message);
                }

                emailMessage.State = EmailMessageState.Sent;
                emailMessage.Sent = TimeZoneManager.Now;
                emailMessage.Received = null;
                emailMessage.Save();
            }
            catch (Exception e)
            {
                if (Transaction.InTestTransaction)
                    throw; 

                var exLog = e.LogException().ToLite();

                using (Transaction tr = Transaction.ForceNew())
                {
                    emailMessage.Exception = exLog;
                    emailMessage.State = EmailMessageState.Exception;
                    emailMessage.Save();
                    tr.Commit();
                }

                throw;
            }
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
                    SmtpClient client = SmtpClientBuilder == null ? SafeSmtpClient() : SmtpClientBuilder(emailMessage);

                    emailMessage.Sent = null;
                    emailMessage.Received = null;
                    emailMessage.Save();

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

                        for (int i = 0; i < 4; i++)
                        {
                            if (emailMessage.InDB().UnsafeUpdate(updater) > 0)
                                return;

                            if (i != 3)
                                Thread.Sleep(3000);
                        }
                    }); 
                }
                else
                {
                    emailMessage.Received = null;
                    emailMessage.State = EmailMessageState.Sent;
                    emailMessage.Sent = TimeZoneManager.Now;
                    emailMessage.Save();
                }
            }
            catch (Exception ex)
            {
                if (Transaction.InTestTransaction)
                    throw; 

                var exLog = ex.LogException().ToLite();

                using (var tr = Transaction.ForceNew())
                {
                    emailMessage.Sent = TimeZoneManager.Now;
                    emailMessage.State = EmailMessageState.Exception;
                    emailMessage.Exception = exLog;
                    emailMessage.Save();
                    tr.Commit();
                }
            }
        }

        public static void SafeSendMailAsync(this SmtpClient client, MailMessage message, Action<AsyncCompletedEventArgs> onComplete)
        {
            client.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
            {
                client.Dispose();
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

        static MailMessage CreateMailMessage(EmailMessageDN emailMessage)
        {
            var address = OverrideEmailAddress();

            if (address == DoNotSend)
                return null;

            MailMessage message = new MailMessage()
            {
                To = { address ?? emailMessage.Recipient.Retrieve().Email },
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = true,
            };

            if (emailMessage.Bcc.HasText())
                message.Bcc.AddRange(emailMessage.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            if (emailMessage.Cc.HasText())
                message.CC.AddRange(emailMessage.Cc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => new MailAddress(a)).ToList());
            return message;
        }

        public static ProcessExecutionDN SendAll<T>(List<T> emails)
            where T : IEmailModel
        {
            EmailPackageDN emailPackage = new EmailPackageDN
            {
            }.Save();

            var packLite = emailPackage.ToLite();

            emails.Select(e => CreateEmailMessage(e, packLite)).SaveList();

            var process = ProcessLogic.Create(EmailMessageProcesses.SendEmails, emailPackage);

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
            }.Save();

            var lite = package.ToLite();

            recipientList.Select(to => new EmailMessageDN
            {
                State = EmailMessageState.Created,
                Recipient = to.ToLite(),
                Template = template,
                Subject = content.Subject,
                Body = content.Body,
                Package = lite
            }).SaveList();

            var process = ProcessLogic.Create(EmailMessageProcesses.SendEmails, package);

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

    
}
