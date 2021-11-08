using System.Net.Mail;
using Signum.Entities.Mailing;
using Signum.Engine.Authorization;
using System.IO;
using Signum.Engine.Files;
using Microsoft.Exchange.WebServices.Data;

namespace Signum.Engine.Mailing
{
    public partial class EmailSenderManager : IEmailSenderManager
    {
        protected virtual void SendSMTP(EmailMessageEntity email, SmtpEmbedded smtp)
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

            System.Net.Mail.AlternateView view = System.Net.Mail.AlternateView.CreateAlternateViewFromString(email.Body.Text!, null, email.IsBodyHtml ? "text/html" : "text/plain");
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
    }

    public static class SmtpExtensions
    {
        public static MailAddress ToMailAddress(this EmailAddressEmbedded address)
        {
            if (address.DisplayName.HasText())
                return new MailAddress(address.EmailAddress, address.DisplayName);

            return new MailAddress(address.EmailAddress);
        }

        public static MailAddress ToMailAddress(this EmailRecipientEmbedded recipient)
        {
            if (!EmailLogic.Configuration.SendEmails)
                throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

            if (recipient.DisplayName.HasText())
                return new MailAddress(EmailLogic.Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress), recipient.DisplayName);

            return new MailAddress(EmailLogic.Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
        }
    }
}
