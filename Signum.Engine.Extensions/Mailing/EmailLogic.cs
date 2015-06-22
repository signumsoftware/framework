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
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null, null, null, null)));
        }

        public static Func<EmailMessageEntity, SmtpClient> GetSmtpClient;
        
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<EmailConfigurationEntity> getConfiguration, Func<EmailTemplateEntity, SmtpConfigurationEntity> getSmtpConfiguration,  Func<EmailMessageEntity, SmtpClient> getSmtpClient = null)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {   
                if (getSmtpClient == null && getSmtpConfiguration != null)
                    getSmtpClient = message => getSmtpConfiguration(message.Template.Try(EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow)).GenerateSmtpClient();

                if (getSmtpClient == null)
                    throw new ArgumentNullException("getSmtpClient");

                FilePathLogic.AssertStarted(sb);
                CultureInfoLogic.AssertStarted(sb);
                EmailLogic.getConfiguration = getConfiguration;
                EmailLogic.GetSmtpClient = getSmtpClient;
                EmailTemplateLogic.Start(sb, dqm, getSmtpConfiguration);

                Schema.Current.WhenIncluded<ProcessEntity>(() => EmailPackageLogic.Start(sb, dqm));

                sb.Include<EmailMessageEntity>();

                PermissionAuthLogic.RegisterPermissions(AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel);

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
                email.SendMailAsync();
        }

        public static void SendMailAsync(this Lite<EmailTemplateEntity> template, IEntity entity)
        {
            foreach (var email in template.CreateEmailMessage(entity))
                email.SendMailAsync();
        }

        public static void SendMailAsync(this EmailMessageEntity email)
        {
            using (OperationLogic.AllowSave<EmailMessageEntity>())
            {
                email.State = EmailMessageState.ReadyToSend;
                email.Save();
            }
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
            if (!Configuration.SendEmails)
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
                    ToStates = { EmailMessageState.Created },
                    Construct = _ => new EmailMessageEntity
                    {
                        State = EmailMessageState.Created,
                    }
                }.Register();

                new ConstructFrom<EmailTemplateEntity>(EmailMessageOperation.CreateMailFromTemplate)
                {
                    AllowsNew = false,
                    ToStates = { EmailMessageState.Created },
                    CanConstruct = et => 
                    {
                        if (et.SystemEmail != null && SystemEmailLogic.RequiresExtraParameters(et.SystemEmail))
                            return "SystemEmail ({1}) requires extra parameters ".FormatWith(et.SystemEmail);

                        if (et.SendDifferentMessages)
                            return "Cannot create email becaue {0} has SendDifferentMessages set";

                        return null;
                    },
                    Construct = (et, args) =>
                    {
                        var entity = args.GetArg<Entity>();

                        ISystemEmail systemEmail = et.SystemEmail == null ? null :
                            (ISystemEmail)SystemEmailLogic.GetEntityConstructor(et.SystemEmail.ToType()).Invoke(new[] { entity });

                        return et.ToLite().CreateEmailMessage(entity, systemEmail).Single();
                    }
                }.Register();

                new Execute(EmailMessageOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailMessageState.Created, EmailMessageState.Outdated },
                    ToStates = { EmailMessageState.Draft },
                    Execute = (m, _) => { m.State = EmailMessageState.Draft; }
                }.Register();

                new Execute(EmailMessageOperation.ReadyToSend)
                {
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailMessageState.Created, EmailMessageState.Draft, EmailMessageState.SentException, EmailMessageState.RecruitedForSending, EmailMessageState.Outdated },
                    ToStates = { EmailMessageState.ReadyToSend },
                    Execute = (m, _) =>
                    {
                        m.SendRetries = 0;
                        m.State = EmailMessageState.ReadyToSend;
                    }
                }.Register();

                new Execute(EmailMessageOperation.Send)
                {
                    CanExecute = m => m.State == EmailMessageState.Created || m.State == EmailMessageState.Draft ||
                         m.State == EmailMessageState.ReadyToSend || m.State == EmailMessageState.RecruitedForSending ||
                         m.State == EmailMessageState.Outdated ? null : EmailMessageMessage.TheEmailMessageCannotBeSentFromState0.NiceToString().FormatWith(m.State.NiceToString()),
                    AllowsNew = true,
                    Lite = false,
                    FromStates = { EmailMessageState.Created, EmailMessageState.Draft, EmailMessageState.ReadyToSend, EmailMessageState.Outdated },
                    ToStates = { EmailMessageState.Sent },
                    Execute = (m, _) => EmailLogic.SenderManager.Send(m)
                }.Register();

                new ConstructFrom<EmailMessageEntity>(EmailMessageOperation.ReSend)
                {
                    AllowsNew = false,
                    ToStates = { EmailMessageState.Created },
                    Construct = (m, _) => new EmailMessageEntity
                    {
                        From = m.From.Clone(),
                        Recipients = m.Recipients.Select(r => r.Clone()).ToMList(),
                        Target = m.Target,
                        Subject = m.Subject,
                        Body = m.Body,
                        IsBodyHtml = m.IsBodyHtml,
                        Template = m.Template,
                        EditableMessage = m.EditableMessage,
                        State = EmailMessageState.Created,
                        Attachments=m.Attachments.ToMList(),
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
                    ContentId = a.ContentId,
                }));

            message.Attachments.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.Attachment)
                .Select(a => new Attachment(a.File.FullPhysicalPath, MimeType.FromFileName(a.File.FileName))
                {
                    ContentId = a.ContentId,
                    Name = a.File.FileName,
                }));

            message.AlternateViews.Add(view);

            message.To.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => r.ToMailAddress()).ToList());
            message.CC.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Cc).Select(r => r.ToMailAddress()).ToList());
            message.Bcc.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => r.ToMailAddress()).ToList());

            return message;
        }

        public virtual void Send(EmailMessageEntity email)
        {
            using (OperationLogic.AllowSave<EmailMessageEntity>())
            {
                if (!EmailLogic.Configuration.SendEmails)
                {
                    email.State = EmailMessageState.Sent;
                    email.Sent = TimeZoneManager.Now;
                    email.Save();
                    return;
                }

                try
                {
                    MailMessage message = CustomCreateMailMessage != null ? CustomCreateMailMessage(email) : CreateMailMessage(email);

                    using (HeavyProfiler.Log("SMTP-Send"))
                        EmailLogic.GetSmtpClient(email).Send(message);

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
                    catch { } //error updating state for email  

                    throw;
                }
            }
        }
    }
}
