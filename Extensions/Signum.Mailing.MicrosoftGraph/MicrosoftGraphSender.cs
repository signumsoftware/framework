using Signum.Mailing;
using Microsoft.Graph;
using System.IO;
using Microsoft.Graph.Users.Item.Messages.Item.Attachments.CreateUploadSession;
using Microsoft.Graph.Models;
using Azure.Identity;
using Azure.Core;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Extensions;
using Signum.Authorization;
using Signum.Files;
using Signum.Utilities.Synchronization;
using Signum.Authorization.AzureAD;

namespace Signum.Mailing.MicrosoftGraph;

//https://jatindersingh81.medium.com/c-code-to-to-send-emails-using-microsoft-graph-api-2a90da6d648a
//https://www.jeancloud.dev/2020/06/05/using-microsoft-graph-as-smtp-server.html
public class MicrosoftGraphSender : EmailSenderBase
{
    public static long MicrosoftGraphFileSizeLimit = 3 * 1024 * 1024;
    MicrosoftGraphEmailServiceEntity microsoftGraph;

    public MicrosoftGraphSender(EmailSenderConfigurationEntity senderConfig, MicrosoftGraphEmailServiceEntity service) : base(senderConfig)
    {
        microsoftGraph = service;
    }

    protected override void SendInternal(EmailMessageEntity email)
    {
        var tokenCritical = microsoftGraph.GeTokenCredential();
        GraphServiceClient graphClient = new GraphServiceClient(tokenCritical);

        var message = ToGraphMessage(email);
        var userId = email.From.AzureUserId.ToString();
        var senderUser = graphClient.Users[userId];

        SendMessage(senderUser, message);
    }

    public static void SendMessage(Microsoft.Graph.Users.Item.UserItemRequestBuilder senderUser, Message message)
    {
        try
        {
            var bigAttachments = message.Attachments?.Extract(a => a is FileAttachment fa && fa.ContentBytes!.Length > MicrosoftGraphFileSizeLimit);
            if (bigAttachments.IsNullOrEmpty())
            {
                senderUser.SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false,
                }).WaitSafe();
            }
            else if (bigAttachments.Any())
            {
                message.IsDraft = true;

                var newMessage = senderUser.Messages.PostAsync(message).ResultSafe()!;
                foreach (var a in bigAttachments.Cast<FileAttachment>())
                {
                    UploadSession uploadSession = senderUser.Messages[newMessage.Id].Attachments.CreateUploadSession.PostAsync(new CreateUploadSessionPostRequestBody
                    {
                        AttachmentItem = new AttachmentItem
                        {
                            AttachmentType = AttachmentType.File,
                            IsInline = a.IsInline,
                            Name = a.Name,
                            Size = a.ContentBytes!.Length,
                            ContentType = MimeMapping.GetMimeType(a.Name!)
                        },
                    }).ResultSafe()!;

                    int maxSliceSize = 320 * 1024;

                    using var fileStream = new MemoryStream(a.ContentBytes!, false);

                    var fileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, fileStream, maxSliceSize).UploadAsync().Result;

                    if (!fileUploadTask.UploadSucceeded)
                        throw new InvalidOperationException("Upload of big files to Microsoft Graph didn't succeed");
                }

                senderUser.MailFolders["Drafts"].Messages[newMessage.Id].Send.PostAsync().WaitSafe();
            }

        }
        catch (ODataError e)
        {
            throw new ODataException(e);
        }
    }

    private Message ToGraphMessage(EmailMessageEntity email)
    {
        return new Message
        {
            Subject = email.Subject,
            Body = new ItemBody
            {
                Content = email.Body.Text,
                ContentType = email.IsBodyHtml ? BodyType.Html : BodyType.Text,
            },
            From = email.From.ToRecipient(),
            ToRecipients = email.Recipients.Where(r => r.Kind == EmailRecipientKind.To).Select(r => r.ToRecipient()).ToList(),
            CcRecipients = email.Recipients.Where(r => r.Kind == EmailRecipientKind.Cc).Select(r => r.ToRecipient()).ToList(),
            BccRecipients = email.Recipients.Where(r => r.Kind == EmailRecipientKind.Bcc).Select(r => r.ToRecipient()).ToList(),
            Attachments = email.Attachments.Select(a => (Attachment)new FileAttachment
            {
                ContentId = a.ContentId,
                Name = a.File.FileName,
                IsInline = a.Type == EmailAttachmentType.LinkedResource,
                ContentType = MimeMapping.GetMimeType(a.File.FileName),
                ContentBytes = a.File.GetByteArray(),
            }).ToList()
        };
    }
}

public static class MicrosoftGraphExtensions
{
    public static Recipient ToRecipient(this EmailAddressEmbedded address)
    {
        return new Recipient
        {
            EmailAddress = new EmailAddress
            {
                Address = address.EmailAddress,
                Name = address.DisplayName
            }
        };
    }

    public static Recipient ToRecipient(this EmailRecipientEmbedded recipient)
    {
        if (!EmailLogic.Configuration.SendEmails)
            throw new InvalidOperationException("EmailConfigurationEmbedded.SendEmails is set to false");

        return new Recipient
        {
            EmailAddress = new EmailAddress
            {
                Address = EmailLogic.Configuration.OverrideEmailAddress.DefaultText(recipient.EmailAddress),
                Name = recipient.DisplayName
            }
        };
    }



    public static TokenCredential GeTokenCredential(this MicrosoftGraphEmailServiceEntity microsoftGraph, string[]? scopes = null)
    {
        if (SignumTokenCredentials.OverridenTokenCredential.Value is var ap && ap != null)
            return ap;

        if (microsoftGraph.UseActiveDirectoryConfiguration)
            return SignumTokenCredentials.GetAuthorizerTokenCredential();

        ClientSecretCredential result = new ClientSecretCredential(
            tenantId: microsoftGraph.Azure_DirectoryID.ToString(),
            clientId: microsoftGraph.Azure_ApplicationID.ToString(),
            clientSecret: microsoftGraph.Azure_ClientSecret);

        return result;
    }



  
}

