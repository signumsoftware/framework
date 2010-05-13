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

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public delegate string BodyRenderer(string viewName, Dictionary<string, string> args);

    public static class EmailLogic
    {
        public static event BodyRenderer BodyRenderer;

        public static Func<SmtpClient> SmtpClientBuilder;

        public static EmailContent RenderWebMail(string subject, string viewName, IEmailOwnerDN owner, Dictionary<string, string> args)
        {
            string body = BodyRenderer != null ?
                BodyRenderer(viewName, args) :
                "An email rendering view {0} for entity {1}".Formato(viewName, owner);

            return new EmailContent
            {
                Body = body,
                Subject = subject
            };
        }

        static Dictionary<Enum, Func<IEmailOwnerDN, Dictionary<string, string>, EmailContent>> EmailTemplates
            = new Dictionary<Enum, Func<IEmailOwnerDN, Dictionary<string, string>, EmailContent>>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {

                sb.Include<EmailMessageDN>();

                EnumLogic<EmailTemplateDN>.Start(sb, () => EmailTemplates.Keys.ToHashSet());

                dqm[typeof(EmailTemplateDN)] = (from e in Database.Query<EmailTemplateDN>()
                                                select new
                                                {
                                                    Entity = e.ToLite(),
                                                    e.Id,
                                                    e.Name,
                                                    e.Key,
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
                                                   Template = e.Template.ToLite(),
                                                   e.Sent,
                                                   e.Received,
                                                   e.Package,
                                                   e.Exception,
                                               }).ToDynamic();

            }
        }

        public static void StarProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailPackageDN>();

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(EmailProcesses.ReSendEmails, new ReSendEmailProcessAlgorithm());

                OperationLogic.Register(new BasicConstructorFromMany<EmailMessageDN, ProcessExecutionDN>(EmailOperations.ReSendEmails)
                {
                    Constructor = (messages, args) => ProcessLogic.Create(EmailProcesses.ReSendEmails, messages)
                });

                dqm[typeof(EmailPackageDN)] = (from e in Database.Query<EmailPackageDN>()
                                               select new 
                                               { 
                                                    Entity = e.ToLite(),
                                                    e.Id,
                                                    e.NumLines,
                                                    e.NumErrors,
                                                    Template = e.Template.ToLite(),
                                               }).ToDynamic();

            }
        }

        public static void RegisterTemplate(Enum templateKey, Func<IEmailOwnerDN, Dictionary<string, string>, EmailContent> template)
        {
            EmailTemplates[templateKey] = template;
        }

        public static EmailMessageDN Send(this IEmailOwnerDN recipient, Enum templateKey, Dictionary<string, string> args)
        {
            EmailContent content = EmailTemplates.GetOrThrow(templateKey, Resources.NotRegisteredInEmailLogic)(recipient, args);

            var result = new EmailMessageDN
            {
                Recipient = recipient.ToLite(),
                Template = EnumLogic<EmailTemplateDN>.ToEntity(templateKey),
                Subject = content.Subject,
                Body = content.Body,
            };

            SendMail(result, true);

            return result;
        }

        public static void SendAsync(this IEmailOwnerDN recipient, Enum templateKey, Dictionary<string, string> args)
        {
            EmailContent content = EmailTemplates.GetOrThrow(templateKey, Resources.NotRegisteredInEmailLogic)(recipient, args);

            var result = new EmailMessageDN
            {
                Recipient = recipient.ToLite(),
                Template = EnumLogic<EmailTemplateDN>.ToEntity(templateKey),
                Subject = content.Subject,
                Body = content.Body,
            };

            SendMailAsync(result);
        }

        public static EmailMessageDN ComposeMail(this EmailMessageDN emailMessage, bool throws)
        {
            try
            {
                EmailContent content = EmailTemplates.GetOrThrow(
                    EnumLogic<EmailTemplateDN>.ToEnum(emailMessage.Template), Resources.NotRegistered)(emailMessage.Recipient.Retrieve(), null);
                emailMessage.Subject = content.Subject;
                emailMessage.Body = content.Body;
            }
            catch (Exception ex)
            {
                emailMessage.Exception = ex.Message;
                emailMessage.State = EmailState.ComposedError;
                if (throws)
                    throw;
            }
            return emailMessage;
        }

        public static void SendMail(EmailMessageDN emailMessage, bool throws)
        {
            emailMessage.State = EmailState.Sent;
            emailMessage.Sent = DateTime.Now;
            emailMessage.Received = null;

            try
            {
                MailMessage message = new MailMessage()
                {
                    To = { emailMessage.Recipient.Retrieve().Email },
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    IsBodyHtml = true,
                };

                SmtpClient client = SmtpClientBuilder == null ? new SmtpClient() : SmtpClientBuilder();
                client.Send(message);
                emailMessage.Save();
            }
            catch (Exception e)
            {
                emailMessage.Exception = e.Message;
                emailMessage.State = EmailState.SentError;
                if (throws)
                    throw;
            }
        }

        public static void SendMailAsync(EmailMessageDN emailMessage)
        {
            emailMessage.Sent = DateTime.Now;
            emailMessage.Received = null;

            MailMessage message = new MailMessage()
            {
                To = { emailMessage.Recipient.Retrieve().Email },
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = true,
            };

            SmtpClient client = SmtpClientBuilder == null ? new SmtpClient() : SmtpClientBuilder();
            client.SendCompleted += new SendCompletedEventHandler(client_SendCompleted);

            client.SendAsync(message, emailMessage);
            emailMessage.Save();
        }

        static void client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            EmailMessageDN emailMessage = (EmailMessageDN)e.UserState;
            if (e.Error != null)
            {
                emailMessage.Exception = e.Error.Message;
                emailMessage.State = EmailState.SentError;
            }
            else
            {
                emailMessage.State = EmailState.Sent;
                emailMessage.Sent = DateTime.Now;
            }
            using (AuthLogic.Disable())
                emailMessage.Save();
        }

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null)));
        }
    }

    public abstract class BaseEmailProcessAlgorithm : IProcessAlgorithm
    {
        public FinalState Execute(IExecutingProcess executingProcess)
        {
            EmailPackageDN package = (EmailPackageDN)executingProcess.Data;

            List<Lite<EmailMessageDN>> emails = GetEmailsToProcess(package);


            int lastPercentage = 0;
            for (int i = 0; i < emails.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                EmailMessageDN ml = emails[i].RetrieveAndForget();

                try
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        ProcessMail(ml);
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        ml.Exception = e.Message;
                        ml.Save();
                        tr.Commit();

                        package.NumErrors++;
                        package.Save();
                    }
                }

                int percentage = (NotificationSteps * i) / emails.Count;
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                    lastPercentage = percentage;
                }
            }

            return FinalState.Finished;
        }

        protected abstract List<Lite<EmailMessageDN>> GetEmailsToProcess(EmailPackageDN packege);

        public abstract void ProcessMail(EmailMessageDN ml);

        public int NotificationSteps = 100;

        public abstract IProcessDataDN CreateData(object[] args);
    }

    public class EmailProcessAlgorithm : BaseEmailProcessAlgorithm
    {
        Func<List<Lite<IEmailOwnerDN>>> getLazies;
        Enum EmailTemplateKey;
        public EmailProcessAlgorithm(Enum emailTemplateKey)
        {
            this.EmailTemplateKey = emailTemplateKey;
        }

        public EmailProcessAlgorithm(Enum emailTemplateKey, Func<List<Lite<IEmailOwnerDN>>> getLazies)
        {
            this.getLazies = getLazies;
            this.EmailTemplateKey = emailTemplateKey;
        }

        public override IProcessDataDN CreateData(object[] args)
        {
            EmailPackageDN package = new EmailPackageDN { Template = EnumLogic<EmailTemplateDN>.ToEntity(EmailTemplateKey) };

            package.Save();

            List<Lite<IEmailOwnerDN>> lites =
                args != null && args.Length > 0 ? args.GetArg<List<Lite<IEmailOwnerDN>>>(0) :
                getLazies != null ? getLazies() : null;

            if (lites == null)
                throw new InvalidOperationException(Resources.NoUsersToProcessFound);

            package.NumLines = lites.Count;

            lites.Select(lite => new EmailMessageDN
            {
                Package = package.ToLite(),
                Recipient = lite,
                Template = package.Template,
                State = EmailState.Empty
            }).SaveList();

            return package;
        }

        protected override List<Lite<EmailMessageDN>> GetEmailsToProcess(EmailPackageDN package)
        {
            return (from email in Database.Query<EmailMessageDN>()
             where email.Package == package.ToLite() && email.State == EmailState.Empty
             select email.ToLite()).ToList();
        }

        public override void ProcessMail(EmailMessageDN ml)
        {
            ml.ComposeMail(true);
            System.Threading.Thread.Sleep(3000);
            EmailLogic.SendMail(ml, true);
        }
    }


    public class ReSendEmailProcessAlgorithm : BaseEmailProcessAlgorithm
    {
        public override IProcessDataDN CreateData(object[] args)
        {
            EmailPackageDN package = new EmailPackageDN();
            package.Save();
            List<Lite<EmailMessageDN>> lites = args.GetArg<List<Lite<EmailMessageDN>>>(0);
            package.NumLines = lites.Count;
            List<Lite<EmailMessageDN>> messages = args != null && args.Length > 0 ?
                args.GetArg<List<Lite<EmailMessageDN>>>(0) : null;

            if (messages == null)
                throw new InvalidOperationException(Resources.NoEmailsToProcessFound);

            package.NumLines = messages.Count;

            messages.Select(m => m.RetrieveAndForget()).Select(m => new EmailMessageDN()
            {
                Package = package.ToLite(),
                Recipient = m.Recipient,
                Body = m.Body,
                Subject = m.Subject,
                Template = m.Template,
                State = EmailState.Composed
            }).SaveList();

            return package;
        }

        protected override List<Lite<EmailMessageDN>> GetEmailsToProcess(EmailPackageDN package)
        {
            return (from email in Database.Query<EmailMessageDN>()
                    where email.Package == package.ToLite() && email.State == EmailState.Composed
                    select email.ToLite()).ToList();
        }

        public override void ProcessMail(EmailMessageDN ml)
        {
            EmailLogic.SendMail(ml, true);
        }
    }
}
