using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.Basics;
using Signum.Mailing.Templates;
using Signum.API.Filters;
using Signum.Basics;
using Signum.API;
using Signum.Authorization;
using Signum.Mailing.MicrosoftGraph.RemoteEmails;
using Azure.Core;
using Microsoft.Graph;
using Signum.Authorization.ActiveDirectory;
using System.Security.Cryptography;
using Microsoft.Graph.Models;
using DocumentFormat.OpenXml.Drawing;

namespace Signum.Mailing;

[ValidateModelFilter]
public class RemoteEmailController : ControllerBase
{
    [HttpGet("api/remoteEmail/{oid}/{messageId}/")]
    public async Task<RemoteEmailMessageModel> GetRemoteEmail([FromRoute] Guid oid, [FromRoute] string messageId)
    {
        var tokenCredential = AzureADLogic.GetTokenCredential();

        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var user = Database.Query<UserEntity>().Where(a => a.Mixin<UserADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();

        var message = (await graphClient.Users[oid.ToString()].Messages[messageId].GetAsync(req =>
        {
            req.QueryParameters.Expand = new[] { "attachments" };
            req.Headers.Add("Prefer", "IdType='ImmutableId'");
        }));


        return new RemoteEmailMessageModel
        {
            Id = message!.Id!,
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
            Attachments = message.Attachments.EmptyIfNull().Select(a=>new RemoteAttachmentEmbedded
            {
                Id = a.Id!,
                IsInline = a.IsInline!.Value,
                LastModifiedDateTime = a.LastModifiedDateTime!.Value,
                Name = a.Name!,
                Size = a.Size!.Value,
            }).ToMList(),
            HasAttachments = message.HasAttachments!.Value,
            
        };
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

