using Signum.Authorization;
using Microsoft.Graph;
using Signum.Utilities.Reflection;
using Signum.API;
using Signum.DynamicQuery.Tokens;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Models;
using Signum.UserAssets;
using Signum.Authorization.AzureAD;

namespace Signum.Mailing.MicrosoftGraph.RemoteEmails;

public static class RemoteEmailsLogic
{
    public static Action<QueryRequest> AuthorizeQueryRequest = qr =>
    {
        if (!qr.Filters.Any(a => a is FilterCondition fc && fc.Token.FullKey() == "User" && fc.Value is Lite<UserEntity> u && u.Is(UserEntity.Current)))
        {
            if (!RemoteEmailMessagePermission.ViewEmailMessagesFromOtherUsers.IsAuthorized())
                throw new UnauthorizedAccessException(RemoteEmailMessage.NotAuthorizedToViewEmailsFromOtherUsers.NiceToString());
        }
    };

    public static Action<Lite<UserEntity>, Microsoft.Graph.Models.Message> AuthorizeMessage = (user, qr) =>
    { 
        if (!user.Is(UserEntity.Current))
        {
            if (!RemoteEmailMessagePermission.ViewEmailMessagesFromOtherUsers.IsAuthorized())
                throw new UnauthorizedAccessException(RemoteEmailMessage.NotAuthorizedToViewEmailsFromOtherUsers.NiceToString());
        }
    };

    public static MessageMicrosoftGraphQueryConverter Converter = new MessageMicrosoftGraphQueryConverter();

    public static Func<Lite<UserEntity>, bool> HasMailbox = user => user.Model is UserLiteModel um && um.OID is Guid oid;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        PermissionLogic.RegisterTypes(typeof(RemoteEmailMessagePermission));

        FilterValueConverter.SpecificConverters.Add(new RemoteEmailFolderConverter());

        QueryLogic.Queries.Register(RemoteEmailMessageQuery.RemoteEmailMessages, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
        {
            using (HeavyProfiler.Log("Microsoft Graph", () => "EmailMessage"))
            {
                var tokenCredential = AzureADLogic.GetTokenCredential();

                RemoteEmailsLogic.AuthorizeQueryRequest(request);

                GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

                var userFilter = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "User" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();
                Microsoft.Graph.Models.MessageCollectionResponse response;
                Lite<UserEntity>? user = userFilter?.Value as Lite<UserEntity>;
                Dictionary<string, RemoteEmailFolderModel> mailFolders = null!; 
                try
                {

                    var (filters, orders) = FixFiltersAndOrders(request.Filters.ToList(), request.Orders.ToList());

                    if (user != null)
                    {
                        if (HasMailbox(user))
                        {
                            var oid = ((UserLiteModel)user.Model!).OID;
                            response = (await graphClient.Users[oid.ToString()].Messages.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = Converter.GetFilters(filters);
                                req.QueryParameters.Search = Converter.GetSearch(filters);
                                req.QueryParameters.Select = Converter.GetSelect(request.Columns.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Orderby = Converter.GetOrderBy(orders.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Expand = Converter.GetExpand(request.Columns.Where(c => InMSGRaph(c.Token)));
                                req.QueryParameters.Top = Converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                                req.Headers.Add("Prefer", "IdType='ImmutableId'");
                            }))!;

                            var folders = (await graphClient.Users[oid.ToString()].MailFolders.GetAsync(req =>
                            {
                                req.QueryParameters.Select = new[] { "displayName" };
                                req.QueryParameters.Top = 100;
                                req.QueryParameters.IncludeHiddenFolders = "true";
                            }))!;

                

                            mailFolders = folders.Value!.ToDictionary(a => a.Id!, a => new RemoteEmailFolderModel { FolderId = a.Id!, DisplayName = a.DisplayName! });
                        }
                        else
                        {
                            response = new Microsoft.Graph.Models.MessageCollectionResponse { Value = new List<Microsoft.Graph.Models.Message>(), OdataCount = 0 };
                        }
                    }
                    else
                    {
                        response = new Microsoft.Graph.Models.MessageCollectionResponse { Value = new List<Microsoft.Graph.Models.Message>(), OdataCount = 0 };
                    }
                }
                catch (ODataError e)
                {
                    throw new ODataException(e);
                }

                var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

           
                var list = response.Value!.Skip(skip).Select(u => new
                {
                    Entity = (Lite<UserEntity>?)null,/*Lie*/
                    Id = u.Id,
                    u.Subject,
                    From = u.From == null ? null : ToRecipientEmbedded(u.From),
                    ToRecipients = u.ToRecipients?.ToString(a => a.EmailAddress!.Name, ", "),
                    u.CreatedDateTime,
                    u.ReceivedDateTime,
                    u.SentDateTime,
                    u.LastModifiedDateTime,
                    u.IsRead,
                    u.IsDraft,
                    u.HasAttachments,
                    Folder = u.ParentFolderId == null ? null : mailFolders.TryGetC(u.ParentFolderId) ?? new RemoteEmailFolderModel
                    {
                        FolderId = u.ParentFolderId,
                        DisplayName = "Unknown"
                    },
                    WellKnownFolderName = (string?)null,
                    User = user,
                    Extension0 = Converter.GetExtension(u, 0),
                    Extension1 = Converter.GetExtension(u, 1),
                    Extension2 = Converter.GetExtension(u, 2),
                    Extension3 = Converter.GetExtension(u, 3),
                });

                return list.ToDEnumerable(queryDescription).Select(request.Columns).OrderBy(request.Orders).WithCount((int?)response.OdataCount);
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
        Implementations.By(typeof(UserEntity)) /*Lie*/);

        if(sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(RemoteEmailMessageQuery), () => QueryLogic.Queries.QueryAllowed(RemoteEmailMessageQuery.RemoteEmailMessages, false));
        }
    }

    private static RecipientEmbedded ToRecipientEmbedded(Recipient a) => new RecipientEmbedded
    {
        EmailAddress = a.EmailAddress?.Address,
        Name = a.EmailAddress?.Name,
    };

    // https://learn.microsoft.com/en-us/graph/api/user-list-messages?view=graph-rest-1.0&tabs=http#using-filter-and-orderby-in-the-same-query
    private static (List<DynamicQuery.Filter> filters, List<Order> orders) FixFiltersAndOrders(List<DynamicQuery.Filter> filters, List<Order> orders)
    {
        orders.Extract(o => o.Token.FullKey() == "Id");

        if (filters.IsEmpty())
            return (filters, orders);

        if(filters.Any(a=>a is FilterCondition fc && fc.Operation == FilterOperation.Contains))
            return (filters, new List<Order>());

        var newFilters = new List<DynamicQuery.Filter>();
        foreach (var order in orders)
        {
            var f = filters.FirstOrDefault(f => f is FilterCondition fc && fc.Token.Equals(order.Token));
            if (f == null)
                f = CreateTrivialFilter(order.Token);
            else
                filters.Remove(f);

            newFilters.Add(f);
        }
        newFilters.AddRange(filters);

        return (newFilters, orders);
    }

    private static DynamicQuery.Filter CreateTrivialFilter(QueryToken token)
    {
        var utype = token.Type.UnNullify();
        var value =
            utype == typeof(string) ? (object)"124536786543214567" :
            utype == typeof(DateTime) ? (object)new DateTime(1990, 1, 1) :
            utype == typeof(DateTimeOffset) ? (object)new DateTimeOffset(new DateTime(1990, 1, 1)) :
            utype == typeof(bool) ? (object)true :
            ReflectionTools.IsNumber(utype) ? (object)0 :
            null;

        return new DynamicQuery.FilterGroup(FilterGroupOperation.Or, null, new List<DynamicQuery.Filter>
        {
            new FilterCondition(token, FilterOperation.EqualTo, value),
            new FilterCondition(token, FilterOperation.DistinctTo, value),
        });
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

    public override string ToGraphField(QueryToken token, GraphFieldUsage usage)
    {
        if (token.FullKey().StartsWith("Folder") || token.FullKey().StartsWith("WellKnownFolderName"))
            return "parentFolderId";

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

    public override string[]? GetOrderBy(IEnumerable<Order> orders)
    {
        return base.GetOrderBy(orders.Where(a => !a.Token.FullKey().StartsWith("WellKnownFolderName")));
    }

    public override string[]? GetSelect(IEnumerable<Column> columns)
    {
        return base.GetSelect(columns.Where(a => !a.Token.FullKey().Let(a => a.StartsWith("Extension") || a.StartsWith("WellKnownFolderName"))));
    }

    public override string? ToFilter(DynamicQuery.Filter f)
    {
        if(f is FilterCondition fc)
        {
            if (fc.Token.FullKey().StartsWith("Extension")) {

                var id = GetExpansionPropertyId(int.Parse(fc.Token.FullKey().After("Extension")));

                if (id == null)
                    return null;

                return $"singleValueExtendedProperties/Any(ep: ep/id eq '{id}' and {BuildCondition("ep/value", fc.Operation, ToStringValue(fc.Value))})";
            }

            if (fc.Token.Type == typeof(RemoteEmailFolderModel))
            {
                return ToGraphField(fc.Token, GraphFieldUsage.Filter) +
                    (fc.Operation == FilterOperation.EqualTo ? " eq " : " ne ") +
                    ToStringValue(fc.Value is RemoteEmailFolderModel fol ? fol.FolderId : null);
            }
        }

        return base.ToFilter(f);
    }

    public virtual string? GetExtension(Microsoft.Graph.Models.Message u, int index)
    {
        if (u.SingleValueExtendedProperties == null)
            return null;

        var guid = GetExpansionPropertyId(index);

        if (guid == null)
            return null;

        return u.SingleValueExtendedProperties!.SingleOrDefaultEx(a => a.Id == guid)?.Value;
    }

    public virtual string[]? GetExpand(IEnumerable<Column> columns)
    {
        return columns
            .Where(a => a.Token.FullKey().StartsWith("Extension"))
            .Select(c => GetExpansionPropertyId(int.Parse(c.Token.FullKey().After("Extension"))))
            .NotNull()
            .Select(id => $"singleValueExtendedProperties($filter=id eq '{id}')")
            .ToArray();
    }

    public virtual string? GetExpansionPropertyId(int index)
    {
        return null;
    }
}
