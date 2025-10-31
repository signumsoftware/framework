using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization;
using Signum.Mailing.MicrosoftGraph.RemoteEmails;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Signum.Authorization.AzureAD;

namespace Signum.Mailing;

[ValidateModelFilter]
public class RemoteEmailController : ControllerBase
{
    [HttpGet("api/remoteEmail/{oid}/{messageId}/")]
    public async Task<RemoteEmailMessageModel> GetRemoteEmail([FromRoute] Guid oid, [FromRoute] string messageId)
    {
        try
        {
            var tokenCredential = AzureADLogic.GetTokenCredential();

            GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

            var user = Database.Query<UserEntity>().Where(a => a.Mixin<UserAzureADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();

            var message = (await graphClient.Users[oid.ToString()].Messages[messageId].GetAsync(req =>
            {
                req.QueryParameters.Expand = new[] { "attachments", "singleValueExtendedProperties($filter=id eq 'String {6A9A7B04-361B-4B89-B270-F2101F00F1D0} Name CommunicationId')" };
                req.Headers.Add("Prefer", "IdType='ImmutableId'");
            }))!;

            RemoteEmailsLogic.AuthorizeMessage(user, message);

            return new RemoteEmailMessageModel
            {
                Id = message.Id!,
                User = user,
                From = message.From == null ? null! : ToRecipientEmbedded(message.From),
                ToRecipients = message.ToRecipients.EmptyIfNull().Select(r => ToRecipientEmbedded(r)).ToMList(),
                CcRecipients = message.CcRecipients.EmptyIfNull().Select(r => ToRecipientEmbedded(r)).ToMList(),
                BccRecipients = message.BccRecipients.EmptyIfNull().Select(r => ToRecipientEmbedded(r)).ToMList(),
                Subject = message.Subject!,
                Body = message.Body!.Content!,
                IsBodyHtml = message.Body.ContentType == BodyType.Html,
                IsDraft = message.IsDraft!.Value,
                IsRead = message.IsRead!.Value,
                CreatedDateTime = message.CreatedDateTime,
                LastModifiedDateTime = message.LastModifiedDateTime,
                ReceivedDateTime = message.ReceivedDateTime,
                SentDateTime = message.SentDateTime,
                WebLink = message.WebLink,
                Attachments = message.Attachments.EmptyIfNull().Select(a => new RemoteAttachmentEmbedded
                {
                    Id = a.Id!,
                    ContentId = (a as FileAttachment)?.ContentId,
                    IsInline = a.IsInline!.Value,
                    LastModifiedDateTime = a.LastModifiedDateTime!.Value,
                    Name = a.Name!,
                    Size = a.Size!.Value,
                }).ToMList(),
                HasAttachments = message.HasAttachments!.Value,
                Extension0 = RemoteEmailsLogic.Converter.GetExtension(message, 0),
                Extension1 = RemoteEmailsLogic.Converter.GetExtension(message, 0),
                Extension2 = RemoteEmailsLogic.Converter.GetExtension(message, 0),
                Extension3 = RemoteEmailsLogic.Converter.GetExtension(message, 0),
            };
        }
        catch (ODataError e)
        {
            throw new ODataException(e);
        }
    }

    [HttpGet("api/remoteEmailFolders/{oid}/"), SignumAllowAnonymous]
    public async Task<List<RemoteEmailFolderModel>> GetRemoteFolders([FromRoute] Guid oid)
    {
        var tokenCredential = AzureADLogic.GetTokenCredential();

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var user = Database.Query<UserEntity>().Where(a => a.Mixin<UserAzureADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();

        var folders = (await graphClient.Users[oid.ToString()].MailFolders.GetAsync(req =>
        {
            req.QueryParameters.IncludeHiddenFolders = "true";
            req.QueryParameters.Select = new[] { "displayName" };
            req.QueryParameters.Top = 100;
        }))!;

        return folders.Value!.Select(a => new RemoteEmailFolderModel { FolderId = a.Id!, DisplayName = a.DisplayName! }).ToList();
    }

    private RecipientEmbedded ToRecipientEmbedded(Recipient r) => new RecipientEmbedded
    {
        Name = r.EmailAddress!.Name,
        EmailAddress = r.EmailAddress!.Address,
    };


    [HttpGet("api/remoteEmail/{oid}/{messageId}/attachment/{attachmentId}"), SignumAllowAnonymous]
    public async Task<FileStreamResult> GetRemoteAttachment([FromRoute] Guid oid, [FromRoute] string messageId, [FromRoute] string attachmentId)
    {
        var tokenCredential = AzureADLogic.GetTokenCredential();

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var attachment = await graphClient.Users[oid.ToString()].Messages[messageId].Attachments[attachmentId].GetAsync();

        var fa = (FileAttachment)attachment!;

        return MimeMapping.GetFileStreamResult(new FileContent(fa.Name!, fa.ContentBytes!), forDownload: true);
    }
}

