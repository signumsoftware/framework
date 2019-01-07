using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.IO;
using Signum.Engine.Files;
using Microsoft.AspNetCore.StaticFiles;

namespace Signum.Engine.Mailing
{
    public static class EmailLogic
    {
        static Func<EmailConfigurationEmbedded> getConfiguration;
        public static EmailConfigurationEmbedded Configuration
        {
            get { return getConfiguration(); }
        }

        public static EmailSenderManager SenderManager;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null, null, null, null, null)));
        }

        public static Func<EmailMessageEntity, SmtpClient> GetSmtpClient;
        
        public static void Start(
            SchemaBuilder sb,  
            Func<EmailConfigurationEmbedded> getConfiguration, 
            Func<EmailTemplateEntity, Lite<Entity>, SmtpConfigurationEntity> getSmtpConfiguration,  
            Func<EmailMessageEntity, SmtpClient> getSmtpClient = null, 
            IFileTypeAlgorithm attachment = null)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {   
                if (getSmtpClient == null && getSmtpConfiguration != null)
                    getSmtpClient = message => getSmtpConfiguration(message.Template?.Let(EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow), message.Target).GenerateSmtpClient();

                FilePathEmbeddedLogic.AssertStarted(sb);
                CultureInfoLogic.AssertStarted(sb);
                EmailLogic.getConfiguration = getConfiguration;
                EmailLogic.GetSmtpClient = getSmtpClient ?? throw new ArgumentNullException(nameof(getSmtpClient));
                EmailTemplateLogic.Start(sb, getSmtpConfiguration);
                if (attachment != null)
                    FileTypeLogic.Register(EmailFileType.Attachment, attachment);

                Schema.Current.WhenIncluded<ProcessEntity>(() => EmailPackageLogic.Start(sb));

                sb.Include<EmailMessageEntity>()
                    .WithQuery(() => e => new
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

                PermissionAuthLogic.RegisterPermissions(AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel);
                
                SenderManager = new EmailSenderManager();

                EmailGraph.Register();
            }
        }

        public static void SendMail(this ISystemEmail systemEmail)
        {
            foreach (var email in systemEmail.CreateEmailMessage())
                SenderManager.Send(email);
        }

        public static void SendMail(this Lite<EmailTemplateEntity> template, ModifiableEntity entity)
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

        public static void SendMailAsync(this Lite<EmailTemplateEntity> template, ModifiableEntity entity)
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

        public static MList<EmailTemplateMessageEmbedded> CreateMessages(Func<EmailTemplateMessageEmbedded> func)
        {
            var list = new MList<EmailTemplateMessageEmbedded>();
            foreach (var ci in CultureInfoLogic.ApplicationCultures)
            {
                using (CultureInfoUtils.ChangeBothCultures(ci))
                {
                    list.Add(func());
                }
            }
            return list;
        }

        public static MailAddress ToMailAddress(this EmailAddressEmbedded address)
        {
            if (address.DisplayName != null)
                return new MailAddress(address.EmailAddress, address.DisplayName);

            return new MailAddress(address.EmailAddress);
        }

        public static MailAddress ToMailAddress(this EmailRecipientEmbedded recipient)
        {
            if (!Configuration.SendEmails)
                throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

            if (recipient.DisplayName != null)
                return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress), recipient.DisplayName);

            return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
        }

        public static void SendAllAsync<T>(List<T> emails)
                   where T : ISystemEmail
        {
            var list = emails.SelectMany(a => a.CreateEmailMessage()).ToList();

            list.ForEach(a => a.State = EmailMessageState.ReadyToSend);

            using (OperationLogic.AllowSave<EmailMessageEntity>())
            {
                list.SaveList();
            }
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

                new ConstructFrom<EmailTemplateEntity>(EmailMessageOperation.CreateEmailFromTemplate)
                {
                    ToStates = { EmailMessageState.Created },
                    CanConstruct = et => 
                    {
                        if (et.SystemEmail != null && SystemEmailLogic.RequiresExtraParameters(et.SystemEmail))
                            return EmailMessageMessage._01requiresExtraParameters.NiceToString(typeof(SystemEmailEntity).NiceName(), et.SystemEmail);

                        if (et.SendDifferentMessages)
                            return ValidationMessage._0IsSet.NiceToString(ReflectionTools.GetPropertyInfo(() => et.SendDifferentMessages).NiceName());

                        return null;
                    },
                    Construct = (et, args) =>
                    {
                        var entity = args.TryGetArgC<ModifiableEntity>() ?? args.GetArg<Lite<Entity>>().Retrieve();

                        var emailMessageEntity = et.ToLite().CreateEmailMessage(entity).FirstOrDefault();
                        if (emailMessageEntity == null)
                        {
                            throw new InvalidOperationException("No suitable recipients were found");
                        }
                        return emailMessageEntity;
                    }
                }.Register();

                new Execute(EmailMessageOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    FromStates = { EmailMessageState.Created, EmailMessageState.Outdated },
                    ToStates = { EmailMessageState.Draft },
                    Execute = (m, _) => { m.State = EmailMessageState.Draft; }
                }.Register();

                new Execute(EmailMessageOperation.ReadyToSend)
                {
                    CanBeNew = true,
                    CanBeModified = true,
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
                    CanBeNew = true,
                    CanBeModified = true,
                    FromStates = { EmailMessageState.Created, EmailMessageState.Draft, EmailMessageState.ReadyToSend, EmailMessageState.Outdated },
                    ToStates = { EmailMessageState.Sent },
                    Execute = (m, _) => EmailLogic.SenderManager.Send(m)
                }.Register();

                new ConstructFrom<EmailMessageEntity>(EmailMessageOperation.ReSend)
                {
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
                        Attachments = m.Attachments.Select(a => a.Clone()).ToMList()
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
                    SendInternal(email);

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

        protected virtual void SendInternal(EmailMessageEntity email)
        {
            MailMessage message = CustomCreateMailMessage != null ? CustomCreateMailMessage(email) : CreateMailMessage(email);

            using (HeavyProfiler.Log("SMTP-Send"))
                EmailLogic.GetSmtpClient(email).Send(message);
        }


        protected virtual MailMessage CreateMailMessage(EmailMessageEntity email)
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
                .Select(a => new LinkedResource(a.File.OpenRead(), MimeMapping.GetMimeType(a.File.FileName))
                {
                    ContentId = a.ContentId,
                }));

            message.Attachments.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.Attachment)
                .Select(a => new Attachment(a.File.OpenRead(), MimeMapping.GetMimeType(a.File.FileName))
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
    }


    public static class MimeMapping
    {        
        public static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            FileExtensionContentTypeProvider mimeConverter = new FileExtensionContentTypeProvider();

            return mimeConverter.Mappings.TryGetValue(extension ?? "", out var result) ? result : "application/octet-stream";
        }
    }
}
