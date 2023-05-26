using MailKit.Net.Pop3;
using MimeKit;
using MimeKit.Tnef;
using Signum.Files;
using Signum.Mailing.Reception;
using System.IO;
using System.Text.RegularExpressions;

namespace Signum.Mailing.Pop3;

public class MailKitPop3Client : Mailing.Pop3.IPop3Client
{
    Pop3Client client;

    public MailKitPop3Client(Pop3EmailReceptionServiceEntity service)
    {
        client = new Pop3Client();

        if (service.EnableSSL)
            client.Connect(service.Host, service.Port, true);
        else
            client.Connect(service.Host, service.Port, MailKit.Security.SecureSocketOptions.None);

        string cleanpw = Pop3ConfigurationLogic.DecryptPassword(service.Password!);

        client.Authenticate(service.Username, cleanpw);
    }

    public EmailMessageEntity GetMessage(MessageUid messageInfo, Lite<EmailReceptionEntity> reception)
    {
        var message = client.GetMessage(messageInfo.Number);

        EmailMessageEntity em = ToEmailMessage(message);

        var ri = em.Mixin<EmailReceptionMixin>().ReceptionInfo;
        ri!.UniqueId = messageInfo.Uid;
        ri!.Reception = reception;

        return em;
    }
    public static int EmailAddressMaxLengt = 100;




    public static List<MailboxAddress> GetMailboxAddress(InternetAddressList ial)
    {

        var mba1 = ial.Select(x => x as MimeKit.GroupAddress).NotNull()
            .SelectMany(g => g.Members).Select(m => m as MailboxAddress).NotNull().Where(a => a?.Address.Length <= EmailAddressMaxLengt);
        var mba2 = ial.Select(x => x as MailboxAddress).NotNull().Where(a => a.Address.Length <= EmailAddressMaxLengt);

        return mba1.Concat(mba2).ToList();
    }

    private EmailMessageEntity ToEmailMessage(MimeMessage message)
    {

        var em = new EmailMessageEntity
        {
            EditableMessage = false,

            From =
            GetMailboxAddress(message.From).Select(x => new EmailFromEmbedded { EmailAddress = x.Address, DisplayName = x.Name }).FirstOrDefault() ??
            GetMailboxAddress(message.ReplyTo).Select(x => new EmailFromEmbedded { EmailAddress = x.Name, DisplayName = x.Name }).FirstOrDefault() ??
                new EmailFromEmbedded { EmailAddress = message.MessageId, InvalidEmail = true, DisplayName = "Missing FROM and ReplyTo" },

            Recipients =
               GetMailboxAddress(message.To).Select(x => new EmailRecipientEmbedded(new System.Net.Mail.MailAddress(x.Address, x.Name), EmailRecipientKind.To)).Concat(
                GetMailboxAddress(message.Cc).Select(x => new EmailRecipientEmbedded(new System.Net.Mail.MailAddress(x.Address, x.Name), EmailRecipientKind.Cc))).Concat(
                 GetMailboxAddress(message.Bcc).Select(x => new EmailRecipientEmbedded(new System.Net.Mail.MailAddress(x.Address, x.Name), EmailRecipientKind.Bcc))).ToMList(),
            State = EmailMessageState.Received,
            Subject = message.Subject
        };



        //Inline images
        //if (message.HtmlBody.HasText())
        {
            foreach (MimePart att in message.BodyParts.Select(bp => bp as MimePart).NotNull())
            {
                //https://stackoverflow.com/questions/31423809/how-to-access-embedded-attachments
                if ((att.ContentDisposition != null && att.ContentDisposition.FileName.HasText()) || att.ContentType.MimeType == "text/calendar")
                {
                    AddAttachment(em, att);
                }
            }
        }


        foreach (var a in message.Attachments)
        {
            if (a is MessagePart)
            {
                var fileName = a.ContentDisposition?.FileName;
                var rfc822 = (MessagePart)a;

                if (string.IsNullOrEmpty(fileName))
                    fileName = "attached-message.eml";

                using (var stream = new MemoryStream())
                {
                    rfc822.Message.WriteTo(stream)
;

                    var attf = new EmailAttachmentEmbedded
                    {
                        ContentId = a.ContentId,
                        Type = EmailAttachmentType.Attachment,
                        File = new FilePathEmbedded(EmailFileType.Attachment, fileName, stream.ReadAllBytes())
                    };
                    AddAttachment(em, attf);
                }
            }
            else if (a is TnefPart || IsWinMailDat(a.ContentType))
            {
                foreach (var a2 in ((TnefPart)a).ExtractAttachments())
                {
                    AddAttachment(em, a2);
                }
            }
            else
            {
                AddAttachment(em, a);
            }
        }


        var delivered = TrimAndClean(message.Headers["Delivered-To"]);
        if (delivered.HasText() && !em.Recipients.Any(r => r.EmailAddress == delivered))
            em.Recipients.Add(new EmailRecipientEmbedded
            {
                DisplayName = null,
                EmailAddress = delivered,
                Kind = EmailRecipientKind.Bcc
            });

        var splitableRecipients = em.Recipients.Where(emr => emr.EmailAddress.Contains(",")).ToList();
        foreach (var omr in splitableRecipients)
        {
            foreach (var splitedEmail in omr.EmailAddress.Split(','))
            {
                em.Recipients.Add(new EmailRecipientEmbedded
                {
                    DisplayName = omr.DisplayName,
                    EmailAddress = splitedEmail,
                    Kind = omr.Kind,
                });
            }
            em.Recipients.Remove(omr);
        }

        using (var stream = new MemoryStream())
        {
            message.WriteTo(stream)
;

            var receptionInfo = new EmailReceptionInfoEmbedded
            {
                RawContent = new BigStringEmbedded { Text = Encoding.ASCII.GetString(stream.ToArray()) },
                SentDate = message.Date.UtcDateTime == DateTime.MinValue ? Clock.Now : message.Date.UtcDateTime,
                ReceivedDate = Clock.Now,
            };

            em.Mixin<EmailReceptionMixin>().ReceptionInfo = receptionInfo;
        }

        var winMailDat = em.Attachments.Extract(a => a.ContentId == "winMailConverted").SingleOrDefaultEx();
        if (winMailDat != null)
        {
            em.IsBodyHtml =
                winMailDat.File.FileName.EndsWith("html", StringComparison.CurrentCultureIgnoreCase) ||
                winMailDat.File.FileName.EndsWith("htm", StringComparison.CurrentCultureIgnoreCase);
            em.Body.Text = Encoding.UTF8.GetString(winMailDat.File.BinaryFile);
        }
        else
        {
            em.IsBodyHtml = message.HtmlBody.HasText();
            em.Body.Text = message.HtmlBody.HasText() ? message.HtmlBody : message.TextBody;

        }

        em.SetCalculateHash();
        return em;
    }

    private void AddAttachment(EmailMessageEntity em, MimeEntity att)
    {
        var attf = GetAttachment(att);
        if (attf != null)
            AddAttachment(em, attf);
    }

    private static void AddAttachment(EmailMessageEntity em, EmailAttachmentEmbedded attf)
    {
        if (!em.Attachments.Any(a => a.File.Hash == attf.File.Hash))
            em.Attachments.Add(attf);
    }

    private EmailAttachmentEmbedded? GetAttachment(MimeEntity attachment)
    {
        var part = (MimePart)attachment;
        var fileName = part.FileName.HasText() ? part.FileName : GetName(part.ContentType);
        fileName = FixFilename(fileName);

        using (var stream = new MemoryStream())
        {

            if (part?.Content == null)
                return null;

            part.Content.DecodeTo(stream)
;
            return new EmailAttachmentEmbedded
            {
                ContentId = part.ContentId.HasText() ? part.ContentId : "NotSet{0}".FormatWith(Guid.NewGuid().ToString()).Substring(0, 20),
                File = new FilePathEmbedded(EmailFileType.Attachment, fileName, stream.ToArray()),
                Type = (!attachment.ContentType.MimeType.Contains("image") || part.ContentDisposition?.Disposition == "attachment") ? EmailAttachmentType.Attachment : EmailAttachmentType.LinkedResource
            };
        }
    }

    private string FixFilename(string filename)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c, '_');
        }

        if (filename.Length > 250)
            return filename.Substring(250);

        if (filename.Length == 0)
            return "NoFileName";

        return filename;
    }

    private bool IsWinMailDat(ContentType contentType)
    {
        return contentType.MediaType != null &&
            (string.Equals(contentType.MediaType, "application/ms-tnef") || string.Equals(contentType.MediaType, "application/dat")) &&
            string.Equals(contentType.Name, "winmail.dat", StringComparison.CurrentCultureIgnoreCase);
    }

    private static string GetName(ContentType ct)
    {
        if (ct.Name.HasText())
            return FileNameValidatorAttribute.RemoveInvalidCharts(ct.Name);

        if (ct.MimeType == "text/calendar")
            return "calendar.ics";

        return "noname" + (MimeMapping.GetExtensionFromMimeType(ct.MediaType) ?? ".unknown");
    }

    private static string? TrimAndClean(string emailIdentifier)
    {
        if (emailIdentifier.IsNullOrEmpty())
            return null;

        var cleaned = emailIdentifier.Trim();

        if (cleaned.StartsWith("'") || cleaned.StartsWith("\""))
            cleaned = cleaned.RemoveStart(1);

        if (cleaned.EndsWith("'") || cleaned.EndsWith("\""))
            cleaned = cleaned.RemoveEnd(1);

        return Regex.Replace(cleaned, @"\t|\n|\r", "");
    }

    public List<MessageUid> GetMessageInfos()
    {
        if (!client.Capabilities.HasFlag(Pop3Capabilities.UIDL))
            throw new Exception("The POP3 server does not support UIDs!");

        var uids = client.GetMessageUids();

        var res = new List<MessageUid>();
        for (int i = 0; i < uids.Count; i++)
        {
            var size = client.GetMessageSize(i);
            var u = new MessageUid(uids[i], i, size);
            res.Add(u);
        }

        return res;
    }

    public void DeleteMessage(MessageUid messageInfo)
    {
        client.DeleteMessage(messageInfo.Number);
    }

    public void Disconnect()
    {
        client.Disconnect(true);
    }

    public void Dispose()
    {
        client.Dispose();
    }

}
