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
using Signum.Entities.Basics;
using System.Text;
using System.Threading;
using Microsoft.Exchange.WebServices.Data;

namespace Signum.Engine.Mailing
{
    public static class EmailLogic
    {
        static Func<EmailConfigurationEmbedded> getConfiguration = null!;
        public static EmailConfigurationEmbedded Configuration
        {
            get { return getConfiguration(); }
        }

        public static EmailSenderManager SenderManager = null!;

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null!, null!, null!, null)));
        }

        public static void Start(
            SchemaBuilder sb,  
            Func<EmailConfigurationEmbedded> getConfiguration, 
            Func<EmailTemplateEntity?, Lite<Entity>?, EmailSenderConfigurationEntity> getEmailSenderConfiguration,  
            IFileTypeAlgorithm? attachment = null)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {   
                FilePathEmbeddedLogic.AssertStarted(sb);
                CultureInfoLogic.AssertStarted(sb);
                EmailLogic.getConfiguration = getConfiguration;
                EmailTemplateLogic.Start(sb, getEmailSenderConfiguration);
                EmailSenderConfigurationLogic.Start(sb);
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
                
                SenderManager = new EmailSenderManager(getEmailSenderConfiguration);

                EmailGraph.Register();

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(EmailMessageEntity).ToTypeEntity());
            if (dateLimit != null)
                Database.Query<EmailMessageEntity>().Where(o => o.CreationDate < dateLimit!.Value).UnsafeDeleteChunksLog(parameters, sb, token);

            dateLimit = parameters.GetDateLimitDeleteWithExceptions(typeof(EmailMessageEntity).ToTypeEntity());
            if (dateLimit == null)
                return;

            Database.Query<EmailMessageEntity>().Where(o => o.CreationDate < dateLimit!.Value && o.Exception != null).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        public static HashSet<Type> GetAllTypes()
        {
            return TypeLogic.TypeToEntity
                      .Where(kvp => typeof(IEmailOwnerEntity).IsAssignableFrom(kvp.Key))
                      .Select(kvp => kvp.Key)
                      .ToHashSet();
        }

        public static void SendMail(this IEmailModel model)
        {
            foreach (var email in model.CreateEmailMessage())
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

        public static void SendMailAsync(this IEmailModel model)
        {
            foreach (var email in model.CreateEmailMessage())
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
            if (address.DisplayName.HasText())
                return new MailAddress(address.EmailAddress, address.DisplayName);

            return new MailAddress(address.EmailAddress);
        }

        public static MailAddress ToMailAddress(this EmailRecipientEmbedded recipient)
        {
            if (!Configuration.SendEmails)
                throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

            if (recipient.DisplayName.HasText())
                return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress), recipient.DisplayName);

            return new MailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
        }

        public static EmailAddress ToEmailAddress(this EmailAddressEmbedded address)
        {
            if (address.DisplayName.HasText())
                return new EmailAddress(address.DisplayName, address.EmailAddress);

            return new EmailAddress(address.EmailAddress);
        }

        public static EmailAddress ToEmailAddress(this EmailRecipientEmbedded recipient)
        {
            if (!Configuration.SendEmails)
                throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

            if (recipient.DisplayName.HasText())
                return new EmailAddress(recipient.DisplayName, Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));

            return new EmailAddress(Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
        }

        public static void SendAllAsync<T>(List<T> emails)
                   where T : IEmailModel
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
                        if (et.Model != null && EmailModelLogic.RequiresExtraParameters(et.Model))
                            return EmailMessageMessage._01requiresExtraParameters.NiceToString(typeof(EmailModelEntity).NiceName(), et.Model);

                        if (et.SendDifferentMessages)
                            return ValidationMessage._0IsSet.NiceToString(ReflectionTools.GetPropertyInfo(() => et.SendDifferentMessages).NiceName());

                        return null;
                    },
                    Construct = (et, args) =>
                    {
                        var entity = args.TryGetArgC<ModifiableEntity>() ?? args.GetArg<Lite<Entity>>().RetrieveAndRemember();

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
                        From = m.From!.Clone(),
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
        private Func<EmailTemplateEntity?, Lite<Entity>?, EmailSenderConfigurationEntity> getEmailSenderConfiguration;

        public EmailSenderManager(Func<EmailTemplateEntity?, Lite<Entity>?, EmailSenderConfigurationEntity> getEmailSenderConfiguration)
        {
            this.getEmailSenderConfiguration = getEmailSenderConfiguration;
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
            var template = email.Template?.Try(t => EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow(t));

            var config = getEmailSenderConfiguration(template, email.Target);

            if (config.SMTP != null)
            {
                SentSMTP(email, config.SMTP);
            }
            else
            {
                SentExchangeWebService(email, config.Exchange!);
            }
        }


        protected virtual void SentSMTP(EmailMessageEntity email, SmtpEmbedded smtp)
        {
            System.Net.Mail.MailMessage message = CreateMailMessage(email);

            using (HeavyProfiler.Log("SMTP-Send"))
                smtp.GenerateSmtpClient().Send(message);
        }

        protected virtual MailMessage CreateMailMessage(EmailMessageEntity email)
        {
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage()
            {
                From = email.From.ToMailAddress(),
                Subject = email.Subject,
                IsBodyHtml = email.IsBodyHtml,
            };

            System.Net.Mail.AlternateView view = System.Net.Mail.AlternateView.CreateAlternateViewFromString(email.Body, null, email.IsBodyHtml ? "text/html" : "text/plain");
            view.LinkedResources.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.LinkedResource)
                .Select(a => new System.Net.Mail.LinkedResource(a.File.OpenRead(), MimeMapping.GetMimeType(a.File.FileName))
                {
                    ContentId = a.ContentId,
                }));
            message.AlternateViews.Add(view);

            message.Attachments.AddRange(email.Attachments
                .Where(a => a.Type == EmailAttachmentType.Attachment)
                .Select(a => new System.Net.Mail.Attachment(a.File.OpenRead(), MimeMapping.GetMimeType(a.File.FileName))
                {
                    ContentId = a.ContentId,
                    Name = a.File.FileName,
                }));


            message.To.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => r.ToMailAddress()).ToList());
            message.CC.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Cc).Select(r => r.ToMailAddress()).ToList());
            message.Bcc.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => r.ToMailAddress()).ToList());

            return message;
        }

        private void SentExchangeWebService(EmailMessageEntity email, ExchangeWebServiceEmbedded exchange)
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2007_SP1);
            service.UseDefaultCredentials = exchange.UseDefaultCredentials;
            service.Credentials = exchange.Username.HasText() ? new WebCredentials(exchange.Username, exchange.Password) : null;
            //service.TraceEnabled = true;
            //service.TraceFlags = TraceFlags.All;

            if (exchange.Url.HasText())
                service.Url = new Uri(exchange.Url);
            else
                service.AutodiscoverUrl(email.From.EmailAddress, RedirectionUrlValidationCallback);

            EmailMessage message = new EmailMessage(service);

            foreach (var a in email.Attachments.Where(a => a.Type == EmailAttachmentType.Attachment))
            {
                var fa = message.Attachments.AddFileAttachment(a.File.FileName, a.File.GetByteArray());
                fa.ContentId = a.ContentId;
            }
            message.ToRecipients.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => r.ToEmailAddress()).ToList());
            message.CcRecipients.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Cc).Select(r => r.ToEmailAddress()).ToList());
            message.BccRecipients.AddRange(email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => r.ToEmailAddress()).ToList());
            message.Subject = email.Subject;
            message.Body = new MessageBody(email.IsBodyHtml ? BodyType.HTML : BodyType.Text, email.Body);
            message.Send();
        }

        protected virtual bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            Uri redirectionUri = new Uri(redirectionUrl);
            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            return redirectionUri.Scheme == "https";
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
