using Signum.Authorization.ActiveDirectory;
using Signum.Authorization;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Azure.Amqp.Framing;
using Signum.Utilities.Reflection;
using Signum.API;
using Signum.DynamicQuery.Tokens;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Groups.Item.MembersWithLicenseErrors;

namespace Signum.Mailing.MicrosoftGraph.RemoteEmails;

public static class RemoteEmailMessageLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            QueryLogic.Queries.Register(RemoteEmailMessageQuery.RemoteEmailMessages, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
            {
                using (HeavyProfiler.Log("Microsoft Graph", () => "EmailMessage"))
                {
                    var tokenCredential = AzureADLogic.GetTokenCredential();

                    GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

                    var userFilter = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "User" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();
                    MessageCollectionResponse response;
                    Lite<UserEntity>? user = userFilter?.Value as Lite<UserEntity>;
                    try
                    {
                        var converter = new MessageMicrosoftGraphQueryConverter();
                        
                        if (user != null)
                        {
                            var um = (UserLiteModel)user.Model!;

                            string oid = um.OID?.ToString() ?? throw new InvalidOperationException($"User {user} does not have an OID");
                            response = (await graphClient.Users[oid].Messages.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                                req.Headers.Add("Prefer", "IdType='ImmutableId'");
                            }))!;
                        }
                        else if(SignumTokenCredentials.OverridenTokenCredential.Value != null)
                        {
                            response = (await graphClient.Me.Messages.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                                req.Headers.Add("Prefer", "IdType='ImmutableId'");
                            }))!;

                            user = UserEntity.Current;

                        }
                        else
                        {
                            response = new MessageCollectionResponse { Value = new List<Message>(), OdataCount = 0 };
                        }
                    }
                    catch (ODataError e)
                    {
                        throw new ODataException(e);
                    }

                    var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

                    return response.Value!.Skip(skip).Select(u => new
                    {
                        Entity = (Lite<EmailMessageEntity>?)null,/*Lie*/
                        Id = u.Id,
                        u.Subject,
                        From = new RecipientEmbedded
                        {
                            EmailAddress = u.From?.EmailAddress?.Address,
                            Name = u.From?.EmailAddress?.Name,
                        },
                        u.CreatedDateTime,
                        u.ReceivedDateTime,
                        u.SentDateTime,
                        u.LastModifiedDateTime,
                        u.IsRead,
                        u.IsDraft,
                        u.HasAttachments,
                        u.ParentFolderId,
                        User = user,
                    }).ToDEnumerable(queryDescription).Select(request.Columns).WithCount((int?)response.OdataCount);
                }
            })
            .Column(a => a.Entity, c => c.Implementations = Implementations.By())
            .ColumnProperyRoutes(a => a.Id, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.Id))
            .ColumnProperyRoutes(a => a.Subject, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.Subject))
            .ColumnProperyRoutes(a => a.CreatedDateTime, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.CreatedDateTime))
            .ColumnProperyRoutes(a => a.ReceivedDateTime, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.ReceivedDateTime))
            .ColumnProperyRoutes(a => a.SentDateTime, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.SentDateTime))
            .ColumnProperyRoutes(a => a.LastModifiedDateTime, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.LastModifiedDateTime))
            .ColumnProperyRoutes(a => a.From, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.From))
            .ColumnProperyRoutes(a => a.User, PropertyRoute.Construct((RemoteEmailMessageModel a) => a.User)),
            Implementations.By(typeof(EmailMessageEntity)) /*Lie*/);

            if(sb.WebServerBuilder != null)
            {
                ReflectionServer.RegisterLike(typeof(RemoteEmailMessageQuery), () => QueryLogic.Queries.QueryAllowed(RemoteEmailMessageQuery.RemoteEmailMessages, false));
            }
        }
    }

    static bool InMSGRaph(QueryToken token)
    {
        if (token.FullKey().StartsWith("Entity") || token.FullKey().StartsWith("User"))
            return false;

        return true;
    }
}

public class MessageMicrosoftGraphQueryConverter : MicrosoftGraphQueryConverter
{
    static PropertyInfo piEmailAddress = ReflectionTools.GetPropertyInfo((RecipientEmbedded re) => re.EmailAddress);
    static PropertyInfo piName = ReflectionTools.GetPropertyInfo((RecipientEmbedded re) => re.Name);

    public override string ToGraphField(QueryToken token)
    {
        var field = token.Follow(a => a.Parent).Reverse().ToString(a =>
        {
            if (a is EntityPropertyToken ept)
            {
                if (ReflectionTools.PropertyEquals(ept.PropertyInfo, piEmailAddress))
                    return "emailAddress/address";

                if (ReflectionTools.PropertyEquals(ept.PropertyInfo, piName))
                    return "emailAddress/name";
            }

            return a.Key.FirstLower();
        }, "/");

        return field;
    }
}
