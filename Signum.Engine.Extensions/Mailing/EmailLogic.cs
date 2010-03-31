using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Net.Mail;
using Signum.Entities.Mailing;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities;

namespace Signum.Engine.Mailing
{
    public class EmailContent
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public static class EmailLogic
    {


        static Dictionary<Enum, Func<IEmailOwnerDN, object[], EmailContent>> emailTemplates
            = new Dictionary<Enum, Func<IEmailOwnerDN, object[], EmailContent>>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();

                EnumLogic<EmailTemplateDN>.Start(sb, () => emailTemplates.Keys.ToHashSet());
            }
        }

        public static void RegisterTemplate(Enum templateKey, Func<IEmailOwnerDN, object[], EmailContent> template)
        {
            emailTemplates[templateKey] = template;
        }

        public static void SendMail(this IEmailOwnerDN recipient, Enum templateKey, params object[] args)
        {
            EmailMessageDN emailMessage=null;
            try
            {
                emailMessage = new EmailMessageDN
               {
                   Recipient = recipient.ToLite(),
                   Template = EnumLogic<EmailTemplateDN>.ToEntity(templateKey),
               };

                EmailContent content = emailTemplates.GetOrThrow(templateKey, "{0} not registered")(recipient, args);

                emailMessage.Subject = content.Subject;
                emailMessage.Body = content.Body;

                SendMail(emailMessage);
            }
            catch (Exception e)
            {
                emailMessage.Exception = e.Message;
                emailMessage.Save();
            }
        }

        private static void SendMail(EmailMessageDN emailMessageDN)
        {
            try
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
            catch (Exception e)
            {
                emailMessageDN.Exception = e.Message;
                emailMessageDN.Save();
            }
        }

        static void client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            EmailMessageDN emailMessage = (EmailMessageDN)e.UserState;

            emailMessage.Exception = e.Error.TryCC(ex=>ex.Message);
            emailMessage.Save();
        }
    }

    public class EmailPackage: PackageAlgorithm<IEmailOwnerDN>
    {
        public override void ExecuteLine(PackageLineDN pl, PackageDN package)
        {
            
        }

        protected override PackageDN CreatePackage(object[] args)
        {
            throw new NotImplementedException();   
        }
    }
}
