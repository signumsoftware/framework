using Signum.Entities.Mailing;
using Signum.Engine.Authorization;
using Signum.Engine.Files;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;
using Microsoft.Graph;
using Signum.Entities.Authorization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Entity = Signum.Entities.Entity;

namespace Signum.Engine.Mailing.Senders;

//https://jatindersingh81.medium.com/c-code-to-to-send-emails-using-microsoft-graph-api-2a90da6d648a
//https://www.jeancloud.dev/2020/06/05/using-microsoft-graph-as-smtp-server.html
public class MicrosoftGraphSender : BaseEmailSender
{
    public static long MicrosoftGraphFileSizeLimit = 3 * 1024 * 1024;
    MicrosoftGraphEmailServiceEntity microsoftGraph;

    public MicrosoftGraphSender(EmailSenderConfigurationEntity senderConfig, MicrosoftGraphEmailServiceEntity service) : base(senderConfig)
    {
        microsoftGraph = service;
    }

    protected override void SendInternal(EmailMessageEntity email)
    {
        try
        {
            var authProvider = microsoftGraph.GetAuthProvider();
        GraphServiceClient graphClient = new GraphServiceClient(authProvider);


            var bigAttachments = email.Attachments.Where(a => a.File.FileLength > MicrosoftGraphFileSizeLimit).ToList();

            var message = ToGraphMessageWithSmallAttachments(email);
            var userId = email.From.AzureUserId.ToString();
            var user = graphClient.Users[userId];

            if (bigAttachments.IsEmpty())
            {
                user.SendMail(message, false).Request().PostAsync().Wait();
            }
            else if (bigAttachments.Any())
            {
                message.IsDraft = true;

                var newMessage = user.Messages.Request().AddAsync(message).Result;
                foreach (var a in bigAttachments)
                {
                    AttachmentItem attachmentItem = new AttachmentItem
                    {
                        AttachmentType = AttachmentType.File,
                        IsInline = a.Type == EmailAttachmentType.LinkedResource,
                        Name = a.File.FileName,
                        Size = a.File.FileLength,
                        ContentType = MimeMapping.GetMimeType(a.File.FileName)
                    };

                    UploadSession uploadSession = user.Messages[newMessage.Id].Attachments.CreateUploadSession(attachmentItem).Request().PostAsync().Result;

                    int maxSliceSize = 320 * 1024;

                    using var fileStream = new MemoryStream(a.File.GetByteArray());

                    var fileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, fileStream, maxSliceSize).UploadAsync().Result;

                    if (!fileUploadTask.UploadSucceeded)
                        throw new InvalidOperationException("Upload of big files to Microsoft Graph didn't succeed");
    }

                user.MailFolders["Drafts"].Messages[newMessage.Id].Send().Request().PostAsync().Wait();
            }
        }
        catch(AggregateException e)
    {
            var only = e.InnerExceptions.Only();
            if(only != null)
            {
                if(only is ServiceException se)
                {
                    if (se.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
                        throw new InvalidOperationException("Request was rejected by Microsoft Graph for being too large", se);
                }

                only.PreserveStackTrace();
                throw only;
            }
            else
            {
                throw;
            }

        }
    }

    private Message ToGraphMessageWithSmallAttachments(EmailMessageEntity email)
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
            Attachments = GetAttachments(email.Attachments)
        };
    }

    private IMessageAttachmentsCollectionPage GetAttachments(MList<EmailAttachmentEmbedded> attachments)
    {
        var result = new MessageAttachmentsCollectionPage();
        
        foreach (var a in attachments)
        {
            if (a.File.FileLength <= MicrosoftGraphFileSizeLimit)
                result.Add(new FileAttachment
                {
                    ContentId = a.ContentId,
                    Name = a.File.FileName,
                    IsInline = a.Type == EmailAttachmentType.LinkedResource,
                    ContentType = MimeMapping.GetMimeType(a.File.FileName),
                    ContentBytes = a.File.GetByteArray(),
                });
        }
        return result;
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

    static AsyncThreadVariable<IAuthenticationProvider?> AuthenticationProvider = Statics.ThreadVariable<IAuthenticationProvider?>("OverrideAuthenticationProvider");


    public static IDisposable OverrideAuthenticationProvider(IAuthenticationProvider value)
    {
        var old = AuthenticationProvider.Value;
        AuthenticationProvider.Value = value;
        return new Disposable(() => AuthenticationProvider.Value = old);
    }

    public static IAuthenticationProvider GetAuthProvider(this MicrosoftGraphEmailServiceEntity microsoftGraph, string[]? scopes = null)
    {
        if (AuthenticationProvider.Value is var ap && ap != null)
            return ap;

        if (microsoftGraph.UseActiveDirectoryConfiguration)
            return AuthLogic.Authorizer is ActiveDirectoryAuthorizer ada ? ada.GetConfig().GetAuthProvider() :
                throw new InvalidOperationException("AuthLogic.Authorizer is not an ActiveDirectoryAuthorizer");

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(microsoftGraph.Azure_ApplicationID.ToString())
        .WithTenantId(microsoftGraph.Azure_DirectoryID.ToString())
        .WithClientSecret(microsoftGraph.Azure_ClientSecret)
        .Build();

        var authResultDirect = confidentialClientApplication.AcquireTokenForClient(scopes ?? new string[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().Result;

        //Microsoft.Graph.Auth is required for the following to work
        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
        return authProvider;
    }

    public static IAuthenticationProvider GetAuthProvider(this ActiveDirectoryConfigurationEmbedded activeDirectoryConfig, string[]? scopes = null)
    {
        if (AuthenticationProvider.Value is var ap && ap != null)
            return ap;

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(activeDirectoryConfig.Azure_ApplicationID.ToString())
        .WithTenantId(activeDirectoryConfig.Azure_DirectoryID.ToString())
        .WithClientSecret(activeDirectoryConfig.Azure_ClientSecret)
        .Build();

        var authResultDirect = confidentialClientApplication.AcquireTokenForClient(scopes ?? new string[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().Result;

        //Microsoft.Graph.Auth is required for the following to work
        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
        return authProvider;
    }

    public static IDisposable OverrideAuthenticationProvider(string accessToken) =>
        OverrideAuthenticationProvider(new AccessTokenProvider(accessToken));
}

public class AccessTokenProvider : IAuthenticationProvider
{
    private string accessToken;

    public AccessTokenProvider(string accessToken)
    {
        this.accessToken = accessToken;
    }

    public Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return Task.CompletedTask;
    }
}
