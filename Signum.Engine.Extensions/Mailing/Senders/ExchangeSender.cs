using Signum.Entities.Mailing;
using Signum.Engine.Authorization;
using Signum.Engine.Files;
using Microsoft.Exchange.WebServices.Data;

namespace Signum.Engine.Mailing.Senders;

public class ExchangeWebServiceSender : BaseEmailSender
{
    ExchangeWebServiceEmailServiceEntity exchange;

    public ExchangeWebServiceSender(EmailSenderConfigurationEntity senderConfig, ExchangeWebServiceEmailServiceEntity service) : base(senderConfig)
    {
        exchange = service;
    }

    protected override void SendInternal(EmailMessageEntity email)
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
        message.Body = new MessageBody(email.IsBodyHtml ? BodyType.HTML : BodyType.Text, email.Body.Text);
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

public static class ExchangeExtensions
{
    public static EmailAddress ToEmailAddress(this EmailAddressEmbedded address)
    {
        if (address.DisplayName.HasText())
            return new EmailAddress(address.DisplayName, address.EmailAddress);

        return new EmailAddress(address.EmailAddress);
    }

    public static EmailAddress ToEmailAddress(this EmailRecipientEmbedded recipient)
    {
        if (!EmailLogic.Configuration.SendEmails)
            throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

        if (recipient.DisplayName.HasText())
            return new EmailAddress(recipient.DisplayName, EmailLogic.Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));

        return new EmailAddress(EmailLogic.Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress));
    }
}
