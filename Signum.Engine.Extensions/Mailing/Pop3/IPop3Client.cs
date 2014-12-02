using Signum.Entities;
using Signum.Entities.Mailing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Extensions.Mailing.Pop3
{
    public interface IPop3Client : IDisposable
    {
        List<MessageUid> GetMessageInfos();

        EmailMessageEntity GetMessage(MessageUid messageInfo, Lite<Pop3ReceptionEntity> reception);

        void DeleteMessage(MessageUid messageInfo);
        void Disconnect();
    }

    public struct MessageUid
    {
        public MessageUid(string uid, int number, int size)
        {
            Uid = uid;
            Number = number;
            Size = size;
        }

        public readonly string Uid;
        public readonly int Number;
        public readonly int Size;
    }

    /*-- Example implementation of IPop3Client by OpenPop.Pop3Client     
    public class OpenPop3Client : IPop3Client
    {
        Pop3Client client;

        public OpenPop3Client(Pop3ConfigurationEntity configuration)
        {
            client = new Pop3Client();
            client.Connect(configuration.Host, configuration.Port, configuration.EnableSSL);
            client.Authenticate(configuration.Username, configuration.Password);
        }

        public List<MessageInfo> GetMessageInfos()
        {
            return client.GetMessageInfos()
                .Select(m => new MessageInfo(m.Identifier, m.Number, m.Size))
                .ToList();
        }

        public EmailMessageEntity GetMessage(MessageInfo messageInfo, Lite<Pop3ReceptionEntity> reception)
        {
            var message = client.GetMessage(messageInfo.Number);

            var em = new EmailMessageEntity
            {
                EditableMessage = false,
                From = ToEmailAddress(message.Headers.From),
                Recipients =
                   message.Headers.To.Select(ma => ToEmailRecipient(ma, EmailRecipientKind.To)).Concat(
                   message.Headers.Cc.Select(ma => ToEmailRecipient(ma, EmailRecipientKind.Cc))).Concat(
                   message.Headers.Bcc.Select(ma => ToEmailRecipient(ma, EmailRecipientKind.Bcc))).ToMList(),
                State = EmailMessageState.Received,
                Subject = message.Headers.Subject,
                Attachments = message.FindAllAttachments().Select(a =>
                    new EmailAttachmentEntity
                    {
                        ContentId = a.ContentId,
                        File = new FilePathEntity(EmailFileType.Attachment, a.ContentDisposition.Try(cd => cd.FileName) ?? a.FileName, a.Body).Save(),
                        Type = a.ContentDisposition.Try(cd => cd.DispositionType) == "attachment" ? EmailAttachmentType.Attachment : EmailAttachmentType.LinkedResource
                    }).ToMList()
            };

            var receptionInfo = new EmailReceptionInfoEntity
            {
                Reception = reception,
                UniqueId = messageInfo.Uid,
                RawContent = Encoding.ASCII.GetString(message.RawMessage),
                SentDate = message.Headers.DateSent,
                ReceivedDate = TimeZoneManager.Now,
            };

            em.Mixin<EmailReceptionMixin>().ReceptionInfo = receptionInfo;

            var bestMessagePart = message.FindFirstHtmlVersion() ?? message.FindFirstPlainTextVersion();

            if (bestMessagePart != null)
            {
                em.IsBodyHtml = bestMessagePart.ContentType.MediaType.Contains("htm");
                em.Body = bestMessagePart.GetBodyAsText();
            }

            return em;
        }

        private EmailRecipientEntity ToEmailRecipient(RfcMailAddress rfcMailAddress, EmailRecipientKind emailRecipientKind)
        {
            return new EmailRecipientEntity
            {
                DisplayName = rfcMailAddress.DisplayName,
                EmailAddress = rfcMailAddress.Address ?? "unknown@unknown.com",
                Kind = emailRecipientKind
            };
        }

        private EmailAddressEntity ToEmailAddress(RfcMailAddress rfcMailAddress)
        {
            return new EmailAddressEntity
            {
                DisplayName = rfcMailAddress.DisplayName,
                EmailAddress = rfcMailAddress.Address ?? "unknown@unknown.com",
            };
        }

        public void DeleteMessage(MessageInfo messageInfo)
        {
            client.DeleteMessage(messageInfo.Number);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
    ----------*/
}
