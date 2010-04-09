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

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public static class EmailLogic
    {
        static Dictionary<Enum, Func<IEmailOwnerDN, object[], EmailContent>> EmailTemplates
            = new Dictionary<Enum, Func<IEmailOwnerDN, object[], EmailContent>>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();

                EnumLogic<EmailTemplateDN>.Start(sb, () => EmailTemplates.Keys.ToHashSet());
            }
        }

        public static void RegisterTemplate(Enum templateKey, Func<IEmailOwnerDN, object[], EmailContent> template)
        {
            EmailTemplates[templateKey] = template;
        }

      
      /*  public static void ComposeAndSend(this IEmailOwnerDN recipient, Enum templateKey, params object[] args)
        {
            SendMail(ComposeMail(recipient, templateKey, args));
        }  */
        
        public static EmailMessageDN PrepareMail(this Lite<IEmailOwnerDN> recipient, Enum templateKey)
        {
            EmailMessageDN emailMessage = new EmailMessageDN
            {
                Recipient = recipient,
                TemplateKey = templateKey,
            };

            return emailMessage;
        }

        public static EmailMessageDN ComposeMail(this EmailMessageDN emailMessage, params object[] args)
        {
            EmailContent content = EmailTemplates.GetOrThrow(
                emailMessage.TemplateKey, "{0} not registered")(emailMessage.Recipient.Retrieve(), args);
            emailMessage.Subject = content.Subject;
            emailMessage.Body = content.Body;
            return emailMessage;
        }


        private static void SendMail(EmailMessageDN emailMessageDN)
        {
            emailMessageDN.Sent = DateTime.Now;
            emailMessageDN.Received = null;

            MailMessage message = new MailMessage()
            {
                To = { emailMessageDN.Recipient.Retrieve().EMail },
                Subject = emailMessageDN.Subject,
                Body = emailMessageDN.Body,
                IsBodyHtml = true,
            };

            SmtpClient client = new SmtpClient();
            client.SendCompleted += new SendCompletedEventHandler(client_SendCompleted);
            client.SendAsync(message, emailMessageDN);
            message.Dispose();
        }

        static void client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            EmailMessageDN emailMessage = (EmailMessageDN)e.UserState;

            emailMessage.Exception = e.Error.TryCC(ex => ex.Message);
            emailMessage.Save();
        }
    }

    public class EmailAlgorithm : IProcessAlgorithm
    {
        Func<List<Lite<IEmailOwnerDN>>> getLazies;
        Enum EmailTemplateKey;
        public EmailAlgorithm()
        {
        }

        public EmailAlgorithm(Enum emailTemplateKey, Func<List<Lite<IEmailOwnerDN>>> getLazies, Enum operationKey)
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
                throw new InvalidOperationException("No users to process found");

            package.NumLines = lites.Count;

            lites.Select(lite => new EmailPackageLineDN
            {
                Package = package.ToLite(),
                Target = lite.ToLite<IIdentifiable>()
            }).SaveList();

            return package;
        }


        public FinalState Execute(IExecutingProcess executingProcess)
        {
            EmailPackageDN package = (EmailPackageDN)executingProcess.Data;

            List<Lite<EmailPackageLineDN>> lines =
                (from pl in Database.Query<EmailPackageLineDN>()
                 where pl.Package == package.ToLite() && pl.FinishTime == null && pl.Exception == null
                 select pl.ToLite()).ToList();

            int lastPercentage = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                EmailPackageLineDN pl = lines[i].RetrieveAndForget();

                try
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        EmailLogic.PrepareMail(pl.Target.ToLite<IEmailOwnerDN>(), EmailTemplateKey);

                        OperationLogic.ExecuteLite<IEmailOwnerDN>(pl.Target.ToLite<IEmailOwnerDN>(), EmailTemplateKey);

                        pl.FinishTime = DateTime.Now;
                        pl.Save();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        pl.Exception = e.Message;
                        pl.Save();
                        tr.Commit();

                        package.NumErrors++;
                        package.Save();
                    }
                }

                int percentage = (NotificationSteps * i) / lines.Count;
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
