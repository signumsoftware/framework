using Signum.Entities.Mailing;
using Signum.Engine.Authorization;
using Signum.Engine.Files;
using Microsoft.Identity.Client;
using Microsoft.Graph.Auth;
using Microsoft.Graph;
using Signum.Entities.Authorization;

namespace Signum.Engine.Mailing;

//https://jatindersingh81.medium.com/c-code-to-to-send-emails-using-microsoft-graph-api-2a90da6d648a
//https://www.jeancloud.dev/2020/06/05/using-microsoft-graph-as-smtp-server.html
public partial class EmailSenderManager : IEmailSenderManager
{
    protected virtual void SendMicrosoftGraph(EmailMessageEntity email, MicrosoftGraphEmbedded microsoftGraph)
    {
        ClientCredentialProvider authProvider = microsoftGraph.GetAuthProvider();
        GraphServiceClient graphClient = new GraphServiceClient(authProvider);

        var message = ToGraphMessage(email);

        graphClient.Users[email.From.AzureUserId.ToString()]
                .SendMail(message, false)
                .Request()
                .PostAsync().Wait();
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
            Attachments = GetAttachments(email.Attachments)
        };
    }

    private IMessageAttachmentsCollectionPage GetAttachments(MList<EmailAttachmentEmbedded> attachments)
    {
        var result = new MessageAttachmentsCollectionPage();
        foreach (var a in attachments)
        {
            result.Add(new FileAttachment
            {
                ContentId = a.ContentId,
                Name = a.File.FileName,
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

    public static ClientCredentialProvider GetAuthProvider(this MicrosoftGraphEmbedded microsoftGraph, string[]? scopes = null)
    {
        if (microsoftGraph.UseActiveDirectoryConfiguration)
            return AuthLogic.Authorizer is ActiveDirectoryAuthorizer ada ? ada.GetConfig().GetAuthProvider() :
                throw new InvalidOperationException("AuthLogic.Authorizer is not an ActiveDirectoryAuthorizer");

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(microsoftGraph.Azure_ApplicationID)
        .WithTenantId(microsoftGraph.Azure_DirectoryID)
        .WithClientSecret(microsoftGraph.Azure_ClientSecret)
        .Build();

        var authResultDirect = confidentialClientApplication.AcquireTokenForClient(scopes ?? new string[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().Result;

        //Microsoft.Graph.Auth is required for the following to work
        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
        return authProvider;
    }

    public static ClientCredentialProvider GetAuthProvider(this ActiveDirectoryConfigurationEmbedded activeDirectoryConfig, string[]? scopes = null)
    {
        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(activeDirectoryConfig.Azure_ApplicationID)
        .WithTenantId(activeDirectoryConfig.Azure_DirectoryID)
        .WithClientSecret(activeDirectoryConfig.Azure_ClientSecret)
        .Build();

        var authResultDirect = confidentialClientApplication.AcquireTokenForClient(scopes ?? new string[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().Result;

        //Microsoft.Graph.Auth is required for the following to work
        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
        return authProvider;
    }


}
