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
    public static class EmailLogic
    {
        public static EmailSenderManager SenderManager;

        public static string DoNotSend = "null@null.com";

        public static Func<string> OverrideEmailAddress = () => null;

        public static Func<EmailMessageDN, SmtpClient> SmtpClientBuilder;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                sb.Include<EmailMessageDN>();

                dqm.RegisterQuery(typeof(EmailMessageDN), () =>
                    from e in Database.Query<EmailMessageDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.State,
                        e.Subject,
                        Text = e.Body,
                        e.Template,
                        e.Sent,
                        e.Received,
                        e.Package,
                        e.Exception,
                    });

                SenderManager = new EmailSenderManager();
            }
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

        public static void SendMail(this EmailMessageDN email)
        {
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

        public static void SendMailAsync(this EmailMessageDN email)
        {
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

        class EmailGraph : Graph<EmailMessageDN, EmailMessageState>
        {
            public static void Register()
            {
                GetState = m => m.State;

                new Construct(EmailMessageOperation.CreateMail)
                {
                    Construct = _ => new EmailMessageDN
                    {
                        State = EmailMessageState.Created,
                    }
                }.Register();

                new ConstructFrom<IIdentifiable>(EmailMessageOperation.CreateMailFromTemplate)
                {
                    AllowsNew = false,
                    Construct = (e, args) =>
                    {
                        var template = args.GetArg<Lite<EmailTemplateDN>>();
                        return EmailTemplateLogic.CreateEmailMessage(template.Retrieve(), e);
                    }
                }.Register();

                new Execute(EmailMessageOperation.Send)
                {
                    CanExecute = m => m.State == EmailMessageState.Created ? null : EmailMessageMessage.TheEmailMessageCannotBeSentFromState0.NiceToString().Formato(m.State.NiceToString()),
                    AllowsNew = true,
                    Lite = false,
                    Execute = (m, _) => EmailLogic.SenderManager.Send(m)
                }.Register();

                new ConstructFrom<EmailMessageDN>(EmailMessageOperation.ReSend)
                {
                    AllowsNew = false,
                    Construct = (m, _) => new EmailMessageDN
                    {
                        From = m.From.Clone(),
                        Recipients = m.Recipients.Select(r => r.Clone()).ToMList(),
                        Subject = m.Subject,
                        Body = m.Body,
                        IsBodyHtml = m.IsBodyHtml,
                        Template = m.Template,
                        EditableMessage = m.EditableMessage,
                        State = EmailMessageState.Created
                    }
                }.Register();
            }
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
                From = email.From.ToMailAddress(),
                Subject = email.Subject,
                Body = email.Body,
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
                    email.State = EmailMessageState.SentException;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

        public Lite<SmtpConfigurationDN> DefaultSmtpConfiguration;

        SmtpClient CreateSmtpClient(EmailMessageDN email)
        {
            if (email.Template != null)
            {
                var smtp = email.Template.InDB(t => t.SmtpConfiguration);
                if (smtp != null)
                    return smtp.GenerateSmtpClient();
            }

            if (DefaultSmtpConfiguration != null)
                return DefaultSmtpConfiguration.GenerateSmtpClient();

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
                                State = EmailMessageState.SentException
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
                    email.State = EmailMessageState.SentException;
                    email.Save();
                    tr.Commit();
                }

                throw;
            }
        }

    }


}
