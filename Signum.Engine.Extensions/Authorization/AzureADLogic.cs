using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Signum.Engine.Mailing.Senders;
using Signum.Engine.Scheduler;
using Signum.Entities.Authorization;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Signum.Engine.Authorization;

public static class AzureADLogic
{
    public static Func<IAuthenticationProvider> GetClientCredentialProvider = () => ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig().GetAuthProvider();


    public static async Task<List<ActiveDirectoryUser>> FindActiveDirectoryUsers(string subStr, int top, CancellationToken token)
    {
        IAuthenticationProvider authProvider = GetClientCredentialProvider();
        GraphServiceClient graphClient = new GraphServiceClient(authProvider);

        subStr = subStr.Replace("'", "''");

        var query = subStr.Contains("@") ? $"mail eq '{subStr}'" :
            subStr.Contains(",") ? $"startswith(givenName, '{subStr.After(",").Trim()}') AND startswith(surname, '{subStr.Before(",").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
            subStr.Contains(" ") ? $"startswith(givenName, '{subStr.Before(" ").Trim()}') AND startswith(surname, '{subStr.After(" ").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
             $"startswith(givenName, '{subStr}') OR startswith(surname, '{subStr}') OR startswith(displayname, '{subStr.Trim()}') OR startswith(mail, '{subStr.Trim()}')";

        var result = await graphClient.Users.Request().Filter(query).Top(top).GetAsync(token);

        return result.Select(a => new ActiveDirectoryUser
        {
            UPN = a.UserPrincipalName,
            DisplayName = a.DisplayName,
            JobTitle = a.JobTitle,
            ObjectID = Guid.Parse(a.Id),
        }).ToList();
    }

    public static TimeSpan CacheADGroupsFor = new TimeSpan(0, minutes: 30, 0);

    static ConcurrentDictionary<Lite<UserEntity>, (DateTime date, List<Guid> groups)> ADGroupsCache = new ConcurrentDictionary<Lite<UserEntity>, (DateTime date, List<Guid> groups)>();

    public static List<Guid> CurrentADGroups()
    {
        var oid = UserADMixin.CurrentOID;
        if (oid == null)
            return new List<Guid>();

        var tuple = ADGroupsCache.AddOrUpdate(UserEntity.Current,
            addValueFactory: user => (Clock.Now, CurrentADGroupsInternal(oid.Value)),
            updateValueFactory: (user, old) => old.date.Add(CacheADGroupsFor) > Clock.Now ? old : (Clock.Now, CurrentADGroupsInternal(oid.Value)));

        return tuple.groups;
    }

    private static List<Guid> CurrentADGroupsInternal(Guid oid)
    {
        using (HeavyProfiler.Log("Microsoft Graph", () => "CurrentADGroups for OID: " + oid))
        {
            IAuthenticationProvider authProvider = AzureADLogic.GetClientCredentialProvider();
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);
            var result = graphClient.Users[oid.ToString()].TransitiveMemberOf.WithODataCast("microsoft.graph.group").Request().Top(999).Select("id, displayName, ODataType").GetAsync().Result.ToList();

            return result.Select(a => Guid.Parse(a.Id)).ToList();
        }
    }

    public static void Start(SchemaBuilder sb, bool adGroups, bool deactivateUsersTask)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            if (deactivateUsersTask)
            {
                SimpleTaskLogic.Register(ActiveDirectoryTask.DeactivateUsers, stc =>
                {
                    var list = Database.Query<UserEntity>().Where(u => u.Mixin<UserADMixin>().OID != null).ToList();

                    IAuthenticationProvider authProvider = GetClientCredentialProvider();
                    GraphServiceClient graphClient = new GraphServiceClient(authProvider);
                    stc.ForeachWriting(list.Chunk(10), gr => gr.Length + " user(s)...", gr =>
                    {
                        var filter = gr.Select(a => "id eq '" + a.Mixin<UserADMixin>().OID + "'").Combined(FilterGroupOperation.Or);
                        var users = graphClient.Users.Request().Filter(filter).Select("accountEnabled, id").GetAsync().Result;

                        var isEnabledDictionary = users.ToDictionary(a => Guid.Parse(a.Id), a => a.AccountEnabled!.Value);

                        foreach (var u in gr)
                        {
                            if (u.State == UserState.Active && !isEnabledDictionary.GetOrThrow(u.Mixin<UserADMixin>().OID!.Value))
                            {
                                stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with OID {u.Mixin<UserADMixin>().OID} has been deactivated in Azure AD");
                                u.Execute(UserOperation.Deactivate);
                            }
                        }
                    });

                    return null;
                });
            }

            if (adGroups)
            {
                sb.Include<ADGroupEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.DisplayName
                    });

                Schema.Current.OnMetadataInvalidated += () => ADGroupsCache.Clear();

                new Graph<ADGroupEntity>.Execute(ADGroupOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (e, _) =>
                    {
                        if (e.IsNew && e.IdOrNull != null)
                            Administrator.SaveDisableIdentity(e);
                    },
                }.Register();

                new Graph<ADGroupEntity>.Delete(ADGroupOperation.Delete)
                {
                    Delete = (e, _) => e.Delete(),
                }.Register();


            

                QueryLogic.Queries.Register(UserADQuery.ActiveDirectoryUsers, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
                 {
                     using (HeavyProfiler.Log("Microsoft Graph", () => "ActiveDirectoryUsers"))
                     {
                         IAuthenticationProvider authProvider = GetClientCredentialProvider();
                         GraphServiceClient graphClient = new GraphServiceClient(authProvider);

                         var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "InGroup" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();

                         var query = graphClient.Users.Request()
                            .InGroup(inGroup?.Value as Lite<ADGroupEntity>)
                            .Filter(request.Filters)
                            .Search(request.Filters)
                            .Select(request.Columns)
                            .OrderBy(request.Orders)
                            .Paginate(request.Pagination);

                         query.QueryOptions.Add(new QueryOption("$count", "true"));
                         query.Headers.Add(new HeaderOption("ConsistencyLevel", "eventual"));

                         var result = await query.GetAsync(cancellationToken);

                         var count = ((JsonElement)result.AdditionalData["@odata.count"]).GetInt32();

                         var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

                         return result.Skip(skip).Select(u => new
                         {
                             Entity = (Lite<Entities.Entity>?)null,
                             u.Id,
                             u.DisplayName,
                             u.UserPrincipalName,
                             u.Mail,
                             u.GivenName,
                             u.Surname,
                             u.JobTitle,
                             u.Department,
                             u.OfficeLocation,
                             u.EmployeeType,
                             OnPremisesExtensionAttributes = u.OnPremisesExtensionAttributes?.Let(ea => new OnPremisesExtensionAttributesModel
                             {
                                 ExtensionAttribute1 = ea.ExtensionAttribute1,
                                 ExtensionAttribute2 = ea.ExtensionAttribute2,
                                 ExtensionAttribute3 = ea.ExtensionAttribute3,
                                 ExtensionAttribute4 = ea.ExtensionAttribute4,
                                 ExtensionAttribute5 = ea.ExtensionAttribute5,
                                 ExtensionAttribute6 = ea.ExtensionAttribute6,
                                 ExtensionAttribute7 = ea.ExtensionAttribute7,
                                 ExtensionAttribute8 = ea.ExtensionAttribute8,
                                 ExtensionAttribute9 = ea.ExtensionAttribute9,
                                 ExtensionAttribute10 = ea.ExtensionAttribute10,
                                 ExtensionAttribute11 = ea.ExtensionAttribute11,
                                 ExtensionAttribute12 = ea.ExtensionAttribute12,
                                 ExtensionAttribute13 = ea.ExtensionAttribute13,
                                 ExtensionAttribute14 = ea.ExtensionAttribute14,
                                 ExtensionAttribute15 = ea.ExtensionAttribute15,
                             }),
                             u.OnPremisesImmutableId,
                             u.CompanyName,
                             u.CreationType,
                             u.AccountEnabled,
                             InGroup = (Lite<ADGroupEntity>?)null,
                         }).ToDEnumerable(queryDescription).Select(request.Columns).WithCount(count);
                     }
                 })
                .Column(a => a.Entity, c => c.Implementations = Implementations.By())
                .ColumnDisplayName(a => a.Id, () => ActiveDirectoryMessage.Id.NiceToString())
                .ColumnDisplayName(a => a.DisplayName, () => ActiveDirectoryMessage.DisplayName.NiceToString())
                .ColumnDisplayName(a => a.Mail, () => ActiveDirectoryMessage.Mail.NiceToString())
                .ColumnDisplayName(a => a.GivenName, () => ActiveDirectoryMessage.GivenName.NiceToString())
                .ColumnDisplayName(a => a.Surname, () => ActiveDirectoryMessage.Surname.NiceToString())
                .ColumnDisplayName(a => a.JobTitle, () => ActiveDirectoryMessage.JobTitle.NiceToString())
                .ColumnDisplayName(a => a.OnPremisesExtensionAttributes, () => ActiveDirectoryMessage.OnPremisesExtensionAttributes.NiceToString())
                .ColumnDisplayName(a => a.OnPremisesImmutableId, () => ActiveDirectoryMessage.OnPremisesImmutableId.NiceToString())
                .ColumnDisplayName(a => a.CompanyName, () => ActiveDirectoryMessage.CompanyName.NiceToString())
                .ColumnDisplayName(a => a.AccountEnabled, () => ActiveDirectoryMessage.AccountEnabled.NiceToString())
                .Column(a => a.InGroup, c => { c.Implementations = Implementations.By(typeof(ADGroupEntity)); c.OverrideDisplayName = () => ActiveDirectoryMessage.InGroup.NiceToString(); })
                ,
                Implementations.By());

                QueryLogic.Queries.Register(UserADQuery.ActiveDirectoryGroups, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
                {
                    using (HeavyProfiler.Log("Microsoft Graph", () => "ActiveDirectoryGroups"))
                    {
                        IAuthenticationProvider authProvider = GetClientCredentialProvider();
                        GraphServiceClient graphClient = new GraphServiceClient(authProvider);

                        var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "HasUser" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();

                        var query = graphClient.Groups.Request()
                           .HasUser(inGroup?.Value as Lite<UserEntity>)
                           .Filter(request.Filters)
                           .Search(request.Filters)
                           .Select(request.Columns)
                           .OrderBy(request.Orders)
                           .Paginate(request.Pagination);

                        query.QueryOptions.Add(new QueryOption("$count", "true"));
                        query.Headers.Add(new HeaderOption("ConsistencyLevel", "eventual"));

                        var result = await query.GetAsync(cancellationToken);

                        var count = ((JsonElement)result.AdditionalData["@odata.count"]).GetInt32();

                        var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

                        return result.Skip(skip).Select(u => new
                        {
                            Entity = (Lite<Entities.Entity>?)null,
                            u.Id,
                            u.DisplayName,
                            u.Description,
                            u.SecurityEnabled,
                            u.Visibility,
                            HasUser = (Lite<UserEntity>?)null,
                        }).ToDEnumerable(queryDescription).Select(request.Columns).WithCount(count);
                    }
                })
                .Column(a => a.Entity, c => c.Implementations = Implementations.By())
                .ColumnDisplayName(a => a.Id, () => ActiveDirectoryMessage.Id.NiceToString())
                .ColumnDisplayName(a => a.DisplayName, () => ActiveDirectoryMessage.DisplayName.NiceToString())
                .ColumnDisplayName(a => a.Description, () => ActiveDirectoryMessage.Description.NiceToString())
                .ColumnDisplayName(a => a.SecurityEnabled, () => ActiveDirectoryMessage.SecurityEnabled.NiceToString())
                .ColumnDisplayName(a => a.Visibility, () => ActiveDirectoryMessage.Visibility.NiceToString())
                .Column(a => a.HasUser, c => { c.Implementations = Implementations.By(typeof(UserEntity)); c.OverrideDisplayName = () => ActiveDirectoryMessage.HasUser.NiceToString(); })
                ,
                Implementations.By());
            }
        }
    }


    static string ToGraphField(QueryToken token, bool simplify = false)
    {
        var field = token.FullKey().Split(".").ToString(a => a.FirstLower(), "/");

        if (simplify)
            return field.TryBefore("/") ?? field;

        return field;
    }


    private static string ToStringValue(object? value)
    {
        return value is string str ? $"'{str}'" :
            value is DateOnly date ? $"'{date.ToIsoString()}'" :
            value is DateTime dt ? $"'{dt.ToIsoString()}'" :
            value is DateTimeOffset dto ? $"'{dto.DateTime.ToIsoString()}'" :
            value is Guid guid ? $"'{guid}'" :
            value is bool b ? b.ToString().ToLower() :
            value?.ToString() ?? "";
    }

    static IGraphServiceUsersCollectionRequest InGroup(this IGraphServiceUsersCollectionRequest users, Lite<ADGroupEntity>? group)
    {
        if (group == null)
            return users;

        var client = (GraphServiceClient)users.Client;

        var url = client.Groups[group.Id.ToString()].TransitiveMembers.AppendSegmentToRequestUrl("microsoft.graph.user");

        var constructor = users.GetType().GetConstructors().SingleEx();

        return (IGraphServiceUsersCollectionRequest)constructor.Invoke(new object[] { url, client, users.QueryOptions });
    }

    static IGraphServiceGroupsCollectionRequest HasUser(this IGraphServiceGroupsCollectionRequest groups, Lite<UserEntity>? user)
    {
        if (user == null)
            return groups;

        var oid = user.InDB(a => a.Mixin<UserADMixin>().OID);
        if (oid == null)
            return groups.Filter("Id eq 'invalid'");

        var client = (GraphServiceClient)groups.Client;

        var url = client.Users[oid.ToString()].TransitiveMemberOf.AppendSegmentToRequestUrl("microsoft.graph.group");

        var constructor = groups.GetType().GetConstructors().SingleEx();

        return (IGraphServiceGroupsCollectionRequest)constructor.Invoke(new object[] { url, client, groups.QueryOptions });
    }

    /// <summary>
    /// Applies an OData cast filter to the returned collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="requestBuilder">Current request builder</param>
    /// <param name="oDataCast">The OData type name</param>
    /// <returns>Request builder with OData cast filter applied</returns>
    public static T WithODataCast<T>(this T requestBuilder, string oDataCast) where T : IBaseRequestBuilder
    {
        var updatedUrl = requestBuilder.AppendSegmentToRequestUrl(oDataCast);
        var updatedBuilder = (T)Activator.CreateInstance(requestBuilder.GetType(), updatedUrl, requestBuilder.Client)!;

        return updatedBuilder;
    }

    static string? ToFilter(Filter f)
    {
        if (f is FilterCondition fc)
        {
            return fc.Operation switch
            {
                FilterOperation.EqualTo => ToGraphField(fc.Token) + " eq " + ToStringValue(fc.Value),
                FilterOperation.DistinctTo => ToGraphField(fc.Token) + " ne " + ToStringValue(fc.Value),
                FilterOperation.GreaterThan => ToGraphField(fc.Token) + " gt " + ToStringValue(fc.Value),
                FilterOperation.GreaterThanOrEqual => ToGraphField(fc.Token) + " ge " + ToStringValue(fc.Value),
                FilterOperation.LessThan => ToGraphField(fc.Token) + " lt " + ToStringValue(fc.Value),
                FilterOperation.LessThanOrEqual => ToGraphField(fc.Token) + " le " + ToStringValue(fc.Value),
                FilterOperation.Contains => null,
                FilterOperation.NotContains => "NOT (" + ToGraphField(fc.Token) + ":" + ToStringValue(fc.Value) + ")",
                FilterOperation.StartsWith => "startswith(" + ToGraphField(fc.Token) + "," + ToStringValue(fc.Value) + ")",
                FilterOperation.EndsWith => "endswith(" + ToGraphField(fc.Token) + "," + ToStringValue(fc.Value) + ")",
                FilterOperation.NotStartsWith => "not startswith(" + ToGraphField(fc.Token) + "," + ToStringValue(fc.Value) + ")",
                FilterOperation.NotEndsWith => "not endswith(" + ToGraphField(fc.Token) + "," + ToStringValue(fc.Value) + ")",
                FilterOperation.IsIn => "(" + ((object[])fc.Value!).ToString(a => ToGraphField(fc.Token) + " eq " + ToStringValue(a), " OR ") + ")",
                FilterOperation.IsNotIn => "not (" + ((object[])fc.Value!).ToString(a => ToGraphField(fc.Token) + " eq " + ToStringValue(a), " OR ") + ")",
                FilterOperation.Like or
                FilterOperation.NotLike or
                _ => throw new InvalidOperationException(fc.Operation + " is not implemented in Microsoft Graph API")
            };
        }
        else if (f is FilterGroup fg)
        {
            return fg.Filters.Select(f2 => ToFilter(f2)).Combined(fg.GroupOperation);
        }
        else
            throw new UnexpectedValueException(f);
    }

    static string? ToSearch(Filter f)
    {
        if (f is FilterCondition fc)
        {
            return fc.Operation switch
            {
                FilterOperation.Contains => "\"" +  ToGraphField(fc.Token) + ":" + fc.Value?.ToString()?.Replace(@"""", @"\""") + "\"",
                _ => null
            };
        }
        else if (f is FilterGroup fg)
        {
            return fg.Filters.Select(f2 => ToSearch(f2)).Combined(fg.GroupOperation);
        }
        else
            throw new UnexpectedValueException(f);
    }

    static BR Filter<BR>(this BR request, List<Filter> filters) where BR : IBaseRequest
    {
        var filterStr = filters.Select(f => ToFilter(f)).Combined(FilterGroupOperation.And);
        if (filterStr.HasText())
            request.QueryOptions.Add(new QueryOption("$filter", filterStr));

        return request;
    }

    static BR Search<BR>(this BR request, List<Filter> filters) where BR : IBaseRequest
    {
        var searchStr = filters.Select(f => ToSearch(f)).Combined(FilterGroupOperation.And);
        if (searchStr.HasText())
            request.QueryOptions.Add(new QueryOption("$search", searchStr));

        return request;
    }

    static string? Combined(this IEnumerable<string?> filterEnumerable, FilterGroupOperation groupOperation)
    {
        var filters = filterEnumerable.ToList();
        var cleanFilters = filters.NotNull().ToList();

        if(groupOperation == FilterGroupOperation.And)
        {
            if (cleanFilters.IsEmpty())
                return null;

            return cleanFilters.ToString(" AND ");
        }
        else
        {
            if (cleanFilters.IsEmpty())
                return null;

            if (cleanFilters.Count != filters.Count)
                throw new InvalidOperationException("Unable to convert filter (mix $filter and $search in an OR");

            if (cleanFilters.Count == 1)
                return cleanFilters.SingleEx();

            return "(" + cleanFilters.ToString(" OR ") + ")";
        }
    }

    static BR Select<BR>(this BR request, List<Column> columns) where BR : IBaseRequest
    {
        var selectStr = columns.Select(c => ToGraphField(c.Token, simplify: true)).Distinct().ToString(",");
        request.QueryOptions.Add(new QueryOption("$select", selectStr));
        return request;
    }

    static BR OrderBy<BR>(this BR request, List<Order> orders) where BR : IBaseRequest
    {
        var orderStr = orders.Select(c => ToGraphField(c.Token) + " " + (c.OrderType == OrderType.Ascending ? "asc" : "desc")).ToString(",");
        if (orderStr.HasText())
            request.QueryOptions.Add(new QueryOption("$orderby", orderStr));

        return request;
    }

    static BR Paginate<BR>(this BR request, Pagination pagination) where BR : IBaseRequest
    {
        var top = pagination switch
        {
            Pagination.All => (int?)null,
            Pagination.Firsts f => f.TopElements,
            Pagination.Paginate p => p.ElementsPerPage * p.CurrentPage,
            _ => throw new UnexpectedValueException(pagination)
        };

        if (top != null)
            request.QueryOptions.Add(new QueryOption("$top", top.ToString()));


        return request;
    }

    public static UserEntity CreateUserFromAD(ActiveDirectoryUser adUser)
    {
        var adAuthorizer = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;
        var config = adAuthorizer.GetConfig();
        
        var acuCtx = GetMicrosoftGraphContext(adUser);

        using (ExecutionMode.Global())
        {
            using (var tr = new Transaction())
            {
                var user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().OID == acuCtx.OID);
                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == acuCtx.UserName) ??
                           (acuCtx.UserName.Contains("@") && config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == acuCtx.UserName || a.UserName == acuCtx.UserName.Before("@")) : null);
                }

                if (user != null)
                {
                    adAuthorizer.UpdateUser(user, acuCtx);

                    return user;
                }

                var result = adAuthorizer.OnCreateUser(acuCtx);

                return tr.Commit(result);
            }
        }
    }

    private static MicrosoftGraphCreateUserContext GetMicrosoftGraphContext(ActiveDirectoryUser adUser)
    {
        IAuthenticationProvider authProvider = GetClientCredentialProvider();
        GraphServiceClient graphClient = new GraphServiceClient(authProvider);
        var msGraphUser = graphClient.Users[adUser.ObjectID.ToString()].Request().GetAsync().Result;

        return new MicrosoftGraphCreateUserContext(msGraphUser);
    }


    public static Task<MemoryStream> GetUserPhoto(Guid OId, int size)
    {
        IAuthenticationProvider authProvider = GetClientCredentialProvider();
        GraphServiceClient graphClient = new GraphServiceClient(authProvider);
        int imageSize = 
            size <= 48 ? 48 : 
            size <= 64 ? 64 : 
            size <= 96 ? 96 : 
            size <= 120 ? 120 : 
            size <= 240 ? 240 : 
            size <= 360 ? 360 : 
            size <= 432 ? 432 : 
            size <= 504 ? 504 : 648;

        return graphClient.Users[OId.ToString()].Photos[$"{imageSize}x{imageSize}"].Content.Request().GetAsync().ContinueWith(photo =>
        {
            MemoryStream ms = new MemoryStream();
            photo.Result.CopyTo(ms);
            return ms;
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }
}

public class MicrosoftGraphCreateUserContext : IAutoCreateUserContext
{
    public MicrosoftGraphCreateUserContext(User user)
    {
        User = user;
    }

    public User User { get; set; }

    public string UserName => User.UserPrincipalName;
    public string? EmailAddress => User.UserPrincipalName;

    public string FirstName => User.GivenName;
    public string LastName => User.Surname;

    public Guid? OID => Guid.Parse(User.Id);

    public string? SID => null;
}

public class ActiveDirectoryUser
{
    public required string DisplayName;
    public required string UPN;
    public required Guid ObjectID;
    public required string JobTitle;
}
