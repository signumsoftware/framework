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

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public delegate string BodyRenderer(string viewName, IEmailOwnerDN owner, Dictionary<string, object> args);  

    public static class EmailLogic
    {
        public static event BodyRenderer BodyRenderer;

        public static EmailContent RenderWebMail(string subject, string viewName, IEmailOwnerDN owner, Dictionary<string, object> args)
        {
            string body = BodyRenderer != null ?
                BodyRenderer(viewName, owner, args) :
                "An email rendering view {0} for entity {1}".Formato(viewName, owner);

            return new EmailContent
            {
                Body = body,
                Subject = subject
            };
        }

        static Dictionary<Enum, Func<IEmailOwnerDN, Dictionary<string, object>, EmailContent>> EmailTemplates
            = new Dictionary<Enum, Func<IEmailOwnerDN, Dictionary<string, object>, EmailContent>>();

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
         
            }
        }

        public static void RegisterTemplate(Enum templateKey, Func<IEmailOwnerDN, Dictionary<string, object>, EmailContent> template)
        {
            EmailTemplates[templateKey] = template;
        }

        public static EmailMessageDN Send(this IEmailOwnerDN recipient, Enum templateKey, Dictionary<string, object> args)
        {
            EmailContent content = EmailTemplates.GetOrThrow(templateKey, Resources.NotRegisteredInEmailLogic)(recipient, args);

            var result = new EmailMessageDN
            {
                Recipient = recipient.ToLite(),
                Template = EnumLogic<EmailTemplateDN>.ToEntity(templateKey) ,
                Subject = content.Subject,
                Body = content.Body,
            };

            SendMail(result);

            return result;
        }

        public static EmailMessageDN ComposeMail(this EmailMessageDN emailMessage)
        {
            EmailContent content = EmailTemplates.GetOrThrow(
                EnumLogic<EmailTemplateDN>.ToEnum(emailMessage.Template), Resources.NotRegistered)(emailMessage.Recipient.Retrieve(), null);
            emailMessage.Subject = content.Subject;
            emailMessage.Body = content.Body;
            return emailMessage;
        }

        public static void SendMail(EmailMessageDN emailMessage)
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

            SmtpClient client = new SmtpClient();
            client.Send(message);
            emailMessage.Save();
        }     
    }

    public class EmailProcessAlgorithm : IProcessAlgorithm
    {
        Func<List<Lite<IEmailOwnerDN>>> getLazies;
        Enum EmailTemplateKey;
        public EmailProcessAlgorithm()
        {
        }

        public EmailProcessAlgorithm(Enum emailTemplateKey, Func<List<Lite<IEmailOwnerDN>>> getLazies, Enum operationKey)
        {
            this.getLazies = getLazies;
            this.EmailTemplateKey = emailTemplateKey;
        }

        public virtual IProcessDataDN CreateData(object[] args)
        {
            EmailPackageDN package = CreatePackage(args);

            package.Save();

            List<Lite<IEmailOwnerDN>> lites =
                args != null && args.Length > 1 ? (List<Lite<IEmailOwnerDN>>)args.GetArg<List<Lite<IEmailOwnerDN>>>(1) :
                getLazies != null ? getLazies() : null;

            if (lites == null)
                throw new InvalidOperationException(Resources.NoUsersToProcessFound);

            package.NumLines = lites.Count;

            lites.Select(lite => new EmailMessageDN
            {
                Package = package.ToLite(),
                Recipient = lite,
                Template = package.Template,
                State = EmailState.Prepared
            });
                
            return package;
        }


        public FinalState Execute(IExecutingProcess executingProcess)
        {
            EmailPackageDN package = (EmailPackageDN)executingProcess.Data;

            List<Lite<EmailMessageDN>> emails =
                (from email in Database.Query<EmailMessageDN>()
                 where email.Package == package.ToLite() && email.Sent == DateTime.MinValue && email.Exception == null
                 select email.ToLite()).ToList();

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
                        EmailLogic.SendMail(ml);
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

        public int NotificationSteps = 100;

        EmailPackageDN CreatePackage(object[] args)
        {
            EmailPackageDN package = new EmailPackageDN { Template = EnumLogic<EmailTemplateDN>.ToEntity(EmailTemplateKey) };

            if (args != null && args.Length > 0)
                package.Name = args.TryGetArgC<string>(0);
            return package;
        }

    }
}
