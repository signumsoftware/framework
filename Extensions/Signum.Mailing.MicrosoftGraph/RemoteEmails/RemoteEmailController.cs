using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Filters;
using Signum.Authorization;
using Signum.Authorization.AzureAD;
using Signum.Mailing.MicrosoftGraph.RemoteEmails;
using System.Text.Json;
using System.Threading;

namespace Signum.Mailing;

[ValidateModelFilter]
public class RemoteEmailController : ControllerBase
{
    [HttpGet("api/remoteEmail/{oid}/message/{messageId}/")]
    public async Task<RemoteEmailMessageModel> GetRemoteEmail([FromRoute] Guid oid, [FromRoute] string messageId)
    {
        try
        {
            var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

            GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

            var user = Database.Query<UserEntity>().Where(a => a.Mixin<UserAzureADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();

            var message = (await graphClient.Users[oid.ToString()].Messages[messageId].GetAsync(req =>
            {
                req.QueryParameters.Expand = new[] { "attachments", "singleValueExtendedProperties($filter=id eq 'String {6A9A7B04-361B-4B89-B270-F2101F00F1D0} Name CommunicationId')" };
                req.Headers.Add("Prefer", "IdType='ImmutableId'");
            }))!;

            var folders = (await graphClient.Users[oid.ToString()].MailFolders.GetAsync(req =>
            {
                req.QueryParameters.Select = new[] { "displayName" };
                req.QueryParameters.Top = 100;
                req.QueryParameters.IncludeHiddenFolders = "true";
            }))!;

            var mailFolders = folders.Value!.ToDictionary(a => a.Id!, a => new RemoteEmailFolderModel { FolderId = a.Id!, DisplayName = a.DisplayName! });

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
                Folder = message.ParentFolderId == null ? null : mailFolders.TryGetC(message.ParentFolderId) ?? new RemoteEmailFolderModel
                {
                    FolderId = message.ParentFolderId,
                    DisplayName = "Unknown"
                },
                Categories = message.Categories!.ToMList(),
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

    private RecipientEmbedded ToRecipientEmbedded(Recipient r) => new RecipientEmbedded
    {
        Name = r.EmailAddress!.Name,
        EmailAddress = r.EmailAddress!.Address,
    };

    [HttpGet("api/remoteEmailFolders/{oid}/")]
    public async Task<List<RemoteEmailFolderModel>> GetRemoteFolders([FromRoute] Guid oid)
    {
        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);


        var folders = (await graphClient.Users[oid.ToString()].MailFolders.GetAsync(req =>
        {
            req.QueryParameters.IncludeHiddenFolders = "true";
            req.QueryParameters.Select = new[] { "displayName" };
            req.QueryParameters.Top = 100;
        }))!;

        return folders.Value!.Select(a => new RemoteEmailFolderModel { FolderId = a.Id!, DisplayName = a.DisplayName! }).ToList();
    }


    [HttpGet("api/remoteEmailCategories/{oid}/")]
    public async Task<List<string>> GetRemoteCategories([FromRoute] Guid oid)
    {
        if(RemoteEmailsLogic.HardCodedCategories != null)
        {
            return RemoteEmailsLogic.HardCodedCategories();
        }

        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var categories = (await graphClient.Users[oid.ToString()].Outlook.MasterCategories.GetAsync());

        return categories!.Value!.Select(a => a.DisplayName!).ToList();
    }


    [HttpGet("api/remoteEmail/{oid}/message/{messageId}/attachment/{attachmentId}"), SignumAllowAnonymous]
    public async Task<FileStreamResult> GetRemoteAttachment([FromRoute] Guid oid, [FromRoute] string messageId, [FromRoute] string attachmentId)
    {
        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var attachment = await graphClient.Users[oid.ToString()].Messages[messageId].Attachments[attachmentId].GetAsync();

        var fa = (FileAttachment)attachment!;

        return MimeMapping.GetFileStreamResult(new FileContent(fa.Name!, fa.ContentBytes!), forDownload: true);
    }


    [HttpPost("api/remoteEmail/{oid}/delete")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task DeleteEmail([FromRoute] Guid oid, [FromBody] List<string> messageIds, CancellationToken cancellationToken)
    {
        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);
        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        return ForeachMessageNDJson(messageIds, cancellationToken, nameof(DeleteEmail), async mId =>
        {
            await graphClient.Users[oid.ToString()].Messages[mId].DeleteAsync();
        });
    }

    [HttpPost("api/remoteEmail/{oid}/moveTo/{folderId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task MoveTo([FromRoute] Guid oid, [FromRoute] string folderId, [FromBody] List<string> messageIds, CancellationToken cancellationToken)
    {
        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        return ForeachMessageNDJson(messageIds, cancellationToken, nameof(MoveTo), async mId =>
        {
            await graphClient.Users[oid.ToString()].Messages[mId].Move.PostAsync(new Microsoft.Graph.Users.Item.Messages.Item.Move.MovePostRequestBody
            {
                DestinationId = folderId
            });
        });
    }

    [HttpPost("api/remoteEmail/{oid}/changeCategories")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task ChangeCategories([FromRoute] Guid oid, [FromBody] ChangeCategoriesRequest changeCategories, CancellationToken cancellationToken)
    {
        var tokenCredential = RemoteEmailsLogic.GetTokenCredentials(oid);

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        return ForeachMessageNDJson(changeCategories.MessageIds, cancellationToken, nameof(MoveTo), async mId =>
        {
            var message = await graphClient
                  .Users[oid.ToString()]    
                  .Messages[mId]
                  .GetAsync(m => m.QueryParameters.Select = new[] { "categories" })!;

            var categories = message!.Categories?.ToHashSet() ?? new HashSet<string>();

            foreach (var categoryName in changeCategories.CategoriesToAdd)
            {
                categories.Add(categoryName);
            }

            foreach (var categoryName in changeCategories.CategoriesToRemove)
            {
                categories.Remove(categoryName);
            }

            await graphClient
                .Users[oid.ToString()]
                .Messages[mId]
                .PatchAsync(new Message
                {
                    Categories = categories.ToList()
                });
        });
    }

    public class ChangeCategoriesRequest
    {
        public List<string> MessageIds { get; set; }
        public List<string> CategoriesToAdd { get; set; }
        public List<string> CategoriesToRemove { get; set; }
    }

    private Task ForeachMessageNDJson(List<string> messageIds, CancellationToken cancellationToken, string actionName, Func<string, Task> action)
    {
        return this.ForeachNDJson(messageIds, cancellationToken, async mId =>
        {
            try
            {
                await action(mId);   
                return new EmailResult(mId);
            }
            catch (Exception e)
            {
                e.Data["MessageId"] = mId;
                e.LogException(a =>
                {
                    a.ControllerName = nameof(RemoteEmailController);
                    a.ActionName = actionName;
                });

                return new EmailResult(mId) { Error = e.Message };
            }
        });
    }


}

public class EmailResult(string id)
{
    public string Id { get; } = id;
    public string? Error { get; init; }
}

