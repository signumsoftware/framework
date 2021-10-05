using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Authorization
{
    public static class MicrosoftGraphLogic
    {
        public static Func<ClientCredentialProvider> GetClientCredentialProvider = () => ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig().GetAuthProvider();


        public static async Task<List<ActiveDirectoryUser>> FindActiveDirectoryUsers(string subStr, int top, CancellationToken token)
        {
            ClientCredentialProvider authProvider = GetClientCredentialProvider();
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
            var oid = UserEntity.Current.Mixin<UserADMixin>().OID;
            if (oid == null)
                return new List<Guid>();

            var tuple = ADGroupsCache.AddOrUpdate(UserEntity.Current.ToLite(),
                addValueFactory: user => (TimeZoneManager.Now, CurrentADGroupsInternal(oid.Value)),
                updateValueFactory: (user, old) => old.date.Add(CacheADGroupsFor) > TimeZoneManager.Now ? old : (TimeZoneManager.Now, CurrentADGroupsInternal(oid.Value)));

            return tuple.groups;
        }

        private static List<Guid> CurrentADGroupsInternal(Guid oid)
        {
            using (HeavyProfiler.Log("Microsoft Graph", () => "CurrentADGroups for OID: " + oid))
            {
                ClientCredentialProvider authProvider = MicrosoftGraphLogic.GetClientCredentialProvider();
                GraphServiceClient graphClient = new GraphServiceClient(authProvider);
                var result = graphClient.Users[oid.ToString()].MemberOf.WithODataCast("microsoft.graph.group").Request().Top(1000).Select("id, displayName, ODataType").GetAsync().Result.ToList();

                return result.Select(a => Guid.Parse(a.Id)).ToList();
            }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
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
                     using (HeavyProfiler.Log("Microsoft Graph", ()=> "ActiveDirectoryUsers"))
                     {
                         ClientCredentialProvider authProvider = GetClientCredentialProvider();
                         GraphServiceClient graphClient = new GraphServiceClient(authProvider);

                         var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "InGroup" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();

                         var query = graphClient.Users.Request()
                            .InGroup(inGroup?.Value as Lite<ADGroupEntity>)
                            .Filter(request.Filters)
                            .Select(request.Columns)
                            .OrderBy(request.Orders)
                            .Paginate(request.Pagination);

                         query.QueryOptions.Add(new QueryOption("$count", "true"));
                         query.Headers.Add(new HeaderOption("ConsistencyLevel", "eventual"));

                         var result =  await query.GetAsync(cancellationToken);

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
                        ClientCredentialProvider authProvider = GetClientCredentialProvider();
                        GraphServiceClient graphClient = new GraphServiceClient(authProvider);

                        var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "HasUser" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();

                        var query = graphClient.Groups.Request()
                           .HasUser(inGroup?.Value as Lite<UserEntity>)
                           .Filter(request.Filters)
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
                value is Utilities.Date date ? $"'{date.ToIsoString()}'" :
                value is DateTime dt ? $"'{dt.ToIsoString()}'" :
                value is DateTimeOffset dto ? $"'{dto.DateTime.ToIsoString()}'" :
                value is Guid guid ? $"'{guid.ToString()}'" :
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

            var url = client.Users[oid.ToString()].MemberOf.AppendSegmentToRequestUrl("microsoft.graph.group");

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

        static string ToFilter(Filter f)
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
                    FilterOperation.Contains => ToGraphField(fc.Token) + ":" + ToStringValue(fc.Value),
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
                if (fg.GroupOperation == FilterGroupOperation.Or)
                    return "(" + fg.Filters.Select(f2 => ToFilter(f2)).ToString(" OR ") + ")";
                else
                    return fg.Filters.Select(f2 => ToFilter(f2)).ToString(" AND ");
            }
            else
                throw new UnexpectedValueException(f);
        }

        static BR Filter<BR>(this BR request, List<Filter> filters) where BR : IBaseRequest
        {
            var filterStr = filters.Select(f => ToFilter(f)).ToString(" AND ");
            if (filterStr.HasText())
                request.QueryOptions.Add(new QueryOption("$filter", filterStr));
            return request;
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
            }

            var result = adAuthorizer.OnAutoCreateUser(acuCtx);

            return result ?? throw new InvalidOperationException(ReflectionTools.GetPropertyInfo((ActiveDirectoryConfigurationEmbedded e) => e.AutoCreateUsers).NiceName() + " is not activated");
        }

        private static MicrosoftGraphCreateUserContext GetMicrosoftGraphContext(ActiveDirectoryUser adUser)
        {
            ClientCredentialProvider authProvider = GetClientCredentialProvider();
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);
            var msGraphUser = graphClient.Users[adUser.ObjectID.ToString()].Request().GetAsync().Result;

            return new MicrosoftGraphCreateUserContext(msGraphUser);
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class ActiveDirectoryUser
    {
        public string DisplayName;
        public string UPN;
        public Guid ObjectID;

        public string JobTitle;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
