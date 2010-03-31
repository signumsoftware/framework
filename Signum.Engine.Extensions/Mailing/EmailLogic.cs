using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Net.Mail;

namespace Signum.Engine.Mailing
{
    public static class EmailLogic
    {
        static Dictionary<Enum, Func<UserDN, object[], EmailMessageDN>> emailTemplates
            = new Dictionary<Enum,Func<UserDN,object[],EmailMessageDN>>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMessageDN>();

                EnumLogic<EmailTemplateDN>.Start(sb, () => emailTemplates.Keys.ToHashSet());
            }
        }

        public static void RegisterTemplate(Enum templateKey, Func<UserDN, object[], EmailMessageDN> template)
        {
            emailTemplates[templateKey] = template;
        }

        public static void SendMail(this UserDN user, Enum templateKey, params object[] args)
        {
            SendMail(emailTemplates.GetOrThrow(templateKey, "{0} not registered")(user, args)); 
        }

        private static void SendMail(EmailMessageDN emailMessageDN)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress("sender@foo.bar.com");
            message.To.Add(new MailAddress("recipient1@foo.bar.com"));
            message.To.Add(new MailAddress("recipient2@foo.bar.com"));
            message.To.Add(new MailAddress("recipient3@foo.bar.com"));
            message.CC.Add(new MailAddress("carboncopy@foo.bar.com"));
            message.Subject = "This is my subject";
            message.Body = "This is the content";
            SmtpClient client = new SmtpClient();
            client.Send(message);            
        }
    }
}
