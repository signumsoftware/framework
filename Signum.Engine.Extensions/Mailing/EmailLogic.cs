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
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using System.Globalization;
using Signum.Engine.Translation;
using System.Net.Configuration;
using System.Configuration;
using Signum.Entities.UserQueries;
using System.IO;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Files;
using System.Threading.Tasks;

namespace Signum.Engine.Mailing
{
    public static class EmailLogic
    {
        static Func<EmailConfigurationEntity> getConfiguration;
        public static EmailConfigurationEntity Configuration
        {
            get { return getConfiguration(); }
        }

        public static EmailSenderManager SenderManager;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<EmailConfigurationEntity> getConfiguration, Func<SmtpConfigurationEntity> defaultSmtpConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FilePathLogic.AssertStarted(sb);
                CultureInfoLogic.AssertStarted(sb);
                EmailLogic.getConfiguration = getConfiguration;
                EmailTemplateLogic.Start(sb, dqm);
                SmtpConfigurationLogic.Start(sb, dqm, defaultSmtpConfiguration ?? (Func<SmtpConfigurationEntity>)(() => null)); 

                sb.Include<EmailMessageEntity>();

                dqm.RegisterQuery(typeof(EmailMessageEntity), () =>
                    from e in Database.Query<EmailMessageEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.State,
                        e.Subject,
                        e.Template,
                        e.Sent,
                        e.Target,
                        e.Package,
                        e.Exception,
                    });

                SenderManager = new EmailSenderManager();

                EmailGraph.Register();
            }
        }

        public static void SendMail(this ISystemEmail systemEmail)
        {
            foreach (var email in systemEmail.CreateEmailMessage())
                SenderManager.Send(email);
        }

        public static void SendMail(this Lite<EmailTemplateEntity> template, IEntity entity)
        {
            foreach (var email in template.CreateEmailMessage(entity))
                SenderManager.Send(email);
        }

        public static void SendMail(this EmailMessageEntity email)
        {
            SenderManager.Send(email);
        }

        public static void SendMailAsync(this ISystemEmail systemEmail)
        {
            foreach (var email in systemEmail.CreateEmailMessage())
                SenderManager.SendAsync(email);
        }

        public static void SendMailAsync(this Lite<EmailTemplateEntity> template, IEntity entity)
        {
            foreach (var email in template.CreateEmailMessage(entity))
                SenderManager.SendAsync(email);
        }

        public static void SendMailAsync(this EmailMessageEntity email)
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
            //http://stackoverflow.com/questions/6935427/smtpclient-sendasync-blocking-my-asp-net-mvc-request
            Task.Factory.StartNew(() => client.SendAsync(message, null), TaskCreationOptions.LongRunning);
        }

        public static SmtpClient SafeSmtpClient()
        {
            if (!EmailLogic.Configuration.SendEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.SendEmails is set to false");

            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient()
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        internal static SmtpClient SafeSmtpClient(string host, int port)
        {
            if (!EmailLogic.Configuration.SendEmails)
                throw new InvalidOperationException("EmailLogic.Configuration.SendEmails is set to false");

            //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
            return new SmtpClient(host, port)
            {
                ServicePoint = { MaxIdleTime = 2 }
            };
        }

        public static MList<EmailTemplateMessageEntity> CreateMessages(Func<EmailTemplateMessageEntity> func)
        {
            var list = new MList<EmailTemplateMessageEntity>();
            foreach (var ci in CultureInfoLogic.ApplicationCultures)
            {
                using (CultureInfoUtils.ChangeBothCultures(ci))
                {
                    list.Add(func());
                }
            }
            return list;
        }

        public static MailAddress ToMailAddress(this EmailAddressEntity address)
        {
            if (address.DisplayName != null)
                return new MailAddress(address.EmailAddress, address.DisplayName);

            return new MailAddress(address.EmailAddress);
        }

        public static MailAddress ToMailAddress(this EmailRecipientEntity recipient)
        {
            if(!Configuration.SendEmails)
                throw new InvalidOperationException("EmailConfigurationEntity.SendEmails is set to false");

            if (recipient.DisplayName != null)
                return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress), recipient.DisplayName);

            return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
        }

        public static ProcessEntity SendAll<T>(List<T> emails, string packageName = null)
                   where T : ISystemEmail
        {
            EmailPackageEntity package = new EmailPackageEntity
            {
                Name = packageName ?? "Package of {0} created on {0}".FormatWith(typeof(T).TypeName(), TimeZoneManager.Now)
            }.Save();

            var packLite = package.ToLite();

            var list = emails.SelectMany(e => e.CreateEmailMessage()).ToList();

            list.ForEach(l => l.Package = packLite);

            list.SaveList();

            var process = ProcessLogic.Create(EmailMessageProcess.SendEmails, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        class EmailGraph : Graph<EmailMessageEntity, EmailMessageState>
        {
            public static void Register()
            {            
                GetState = m => m.State;

                new Construct(EmailMessageOperation.CreateMail)
                {
                    ToState = EmailMessageState.Created,
                    Construct = _ => new EmailMessageEntity
                    {
                        State = EmailMessageState.Created,
                    }
                }.Register();

                new ConstructFrom<EmailTemplateEntity>(EmailMessageOperation.CreateMailFromTemplate)
                {
                    AllowsNew = false,
                    ToState = EmailMessageState.Created,
                    CanConstruct = et => 
                    {
                        if (et.SystemEmail != null)
                            return "Cannot send email because {0} is a SystemEmail ({1})".FormatWith(et, et.SystemEmail);

                        if (et.SendDifferentMessages)
                            return "Cannot create email becaue {0} has SendDifferentMessages set";

                        return null;
                    },
                    Construct = (et, args) =>
                    {
                        var entity = args.GetArg<Entity>();
                        return et.ToLite().CreateEmailMessage(entity).Single();
                    }
                }.Register();

                new Execute(EmailMessageOperation.Send)
                {
                    CanExecute = m => m.State == EmailMessageState.Created ? null : EmailMessageMessage.TheEmailMessageCannotBeSentFromState0.NiceToString().FormatWith(m.State.NiceToString()),
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailMessageState.Created },
                    ToState = EmailMessageState.Sent,
                    Execute = (m, _) => EmailLogic.SenderManager.Send(m)
                }.Register();

                new ConstructFrom<EmailMessageEntity>(EmailMessageOperation.ReSend)
                {
                    AllowsNew = false,
                    ToState = EmailMessageState.Created,
                    Construct = (m, _) => new EmailMessageEntity
                    {
                        From = m.From.Clone(),
                        Recipients = m.Recipients.Select(r => r.Clone()).ToMList(),
                        Target = m.Target,
                        Subject = m.Subject,
                        Body = m.Body,
                        IsBodyHtml = m.IsBodyHtml,
                        Template = m.Template,
                        SmtpConfiguration = m.SmtpConfiguration,
                        EditableMessage = m.EditableMessage,
                        State = EmailMessageState.Created
                    }
                }.Register();

                new Graph<EmailMessageEntity>.Delete(EmailMessageOperation.Delete)
                {
                    Delete = (m, _) => m.Delete()
                }.Register();
            }
        }
    }

    public class EmailSenderManager
    {
        public EmailSenderManager()
        {

        }

        public static Func<EmailMessageEntity, MailMessage> CustomCreateMailMessage;

        public MailMessage CreateMailMessage(EmailMessageEntity email)
        {
            MailMessage message = new MailMessage()
            {
                From = email.From.ToMailAddress(),
                Subject = email.Subject,
                IsBodyHtml = email.IsBodyHtml,
            };

            AlternateView view = AlternateView.CreateAlternateViewFromString(email.Body, null, email.IsBodyHtml ? "text/html" : "text/plain");
            view.LinkedResources.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.LinkedResource)
                .Select(a => new LinkedResource(a.File.FullPhysicalPath, MimeType.FromFileName(a.File.FileName))
                {
                    ContentId = a.ContentId
                }));

            message.Attachments.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.Attachment)
                .Select(a => new Attachment(a.File.FullPhysicalPath, MimeType.FromFileName(a.File.FileName))
                {
                    ContentId = a.ContentId
                }));

            message.AlternateViews.Add(view);

            message.To.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => r.ToMailAddress()).ToList());
            message.CC.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Cc).Select(r => r.ToMailAddress()).ToList());
            message.Bcc.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => r.ToMailAddress()).ToList());

            return message;
        }

        public virtual void Send(EmailMessageEntity email)
        {
            if (!EmailLogic.Configuration.SendEmails)
            {
                email.State = EmailMessageState.Sent;
                email.Sent = TimeZoneManager.Now;
                email.Save();
                return;
            }

            using (OperationLogic.AllowSave<EmailMessageEntity>())
            {
                try
                {
                    MailMessage message = CustomCreateMailMessage != null ? CustomCreateMailMessage(email) : CreateMailMessage(email);

                    CreateSmtpClient(email).Send(message);

                    email.State = EmailMessageState.Sent;
                    email.Sent = TimeZoneManager.Now;
                    email.Save();
                }
                catch (Exception ex)
                {
                    if (Transaction.InTestTransaction) //Transaction.IsTestTransaction
                        throw;

                    var exLog = ex.LogException().ToLite();

                    try
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            email.Exception = exLog;
                            email.State = EmailMessageState.SentException;
                            email.Save();
                            tr.Commit();
                        }
                    }
                    catch (Exception)
                    {

                    }

                    throw;
                }
            }
        }

        SmtpClient CreateSmtpClient(EmailMessageEntity email)
        {
            if (email.SmtpConfiguration != null)
            {
                return email.SmtpConfiguration.Retrieve().GenerateSmtpClient();
            }

            if (email.Template != null)
            {
                var smtp = email.Template.InDB(t => t.SmtpConfiguration);
                if (smtp != null)
                    return smtp.GenerateSmtpClient();
            }

            if (SmtpConfigurationLogic.DefaultSmtpConfiguration != null)
            {
                var val = SmtpConfigurationLogic.DefaultSmtpConfiguration();
                if (val != null)
                    return val.GenerateSmtpClient();
            }

            return EmailLogic.SafeSmtpClient();
        }

        public virtual void SendAsync(EmailMessageEntity email)
        {
            using (OperationLogic.AllowSave<EmailMessageEntity>())
            {
                try
                {
                    if (!EmailLogic.Configuration.SendEmails)
                    {
                        email.State = EmailMessageState.Sent;
                        email.Sent = TimeZoneManager.Now;
                        email.Save();
                    }
                    else
                    {
                        SmtpClient client = CreateSmtpClient(email);

                        MailMessage message = CustomCreateMailMessage != null ? CustomCreateMailMessage(email) : CreateMailMessage(email);

                        email.Sent = null;
                        email.Save();

                        client.SafeSendMailAsync(message, args =>
                        {
                            if (args.Error != null)
                            {
                                var exLog = args.Error.LogException().ToLite();
                                email.InDB().UnsafeUpdate()
                                    .Set(a => a.Exception, a => exLog)
                                    .Set(a => a.State, a => EmailMessageState.SentException)
                                    .Execute();
                            }
                            else
                            {
                                email.InDB().UnsafeUpdate()
                                   .Set(a => a.Sent, a => TimeZoneManager.Now)
                                   .Set(a => a.State, a => EmailMessageState.Sent)
                                   .Execute();
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
}
