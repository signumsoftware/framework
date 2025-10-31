using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Signum.Scheduler;
using System.Collections.Concurrent;
using System.IO;
using Signum.API;
using Microsoft.Graph.Models.ODataErrors;
using Signum.Utilities.Synchronization;
using Signum.Authorization.ADGroups;
using Signum.Authorization.AzureAD.ADGroup;
using Signum.Authorization.AzureAD.Authorizer;

namespace Signum.Authorization.AzureAD;

public static class AzureADLogic
{
    public static Func<TokenCredential> GetTokenCredential = () => SignumTokenCredentials.GetAuthorizerTokenCredential();

    public static void Start(SchemaBuilder sb, bool adGroupsAndQueries, bool deactivateUsersTask)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        MixinDeclarations.AssertDeclared(typeof(UserEntity), typeof(UserAzureADMixin));

        PermissionLogic.RegisterTypes(typeof(ActiveDirectoryPermission));
            
        As.ReplaceExpression((UserEntity u) => u.EmailOwnerData, u => new EmailOwnerData
        {
            Owner = u.ToLite(),
            CultureInfo = u.CultureInfo,
            DisplayName = u.UserName,
            Email = u.Email,
            AzureUserId = u.Mixin<UserAzureADMixin>().OID
        });

        UserWithClaims.FillClaims += (userWithClaims, user) =>
        {
            var mixin = ((UserEntity)user).Mixin<UserAzureADMixin>();
            userWithClaims.Claims["OID"] = mixin.OID;
        };

        Lite.RegisterLiteModelConstructor((UserEntity u) => new UserLiteModel
        {
            UserName = u.UserName,
            ToStringValue = u.ToString(),
            OID = u.Mixin<UserAzureADMixin>().OID,
        });

        if (deactivateUsersTask)
        {
            SimpleTaskLogic.Register(AzureADTask.DeactivateUsers, stc =>
            {
                var list = Database.Query<UserEntity>().Where(u => u.Mixin<UserAzureADMixin>().OID != null).ToList();

                var tokenCredential = GetTokenCredential();
                GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);
                stc.ForeachWriting(list.Chunk(10), gr => gr.Length + " user(s)...", gr =>
                {
                    var filter = gr.Select(a => "id eq '" + a.Mixin<UserAzureADMixin>().OID + "'").Combined(FilterGroupOperation.Or);
                    var users = graphClient.Users.GetAsync(r =>
                    {
                        r.QueryParameters.Select = new[] { "id", "accountEnabled" };
                        r.QueryParameters.Filter = filter;
                    }).Result;

                    var isEnabledDictionary = users!.Value!.ToDictionary(a => Guid.Parse(a.Id!), a => a.AccountEnabled!.Value);

                    foreach (var u in gr)
                    {
                        if (u.State == UserState.Active && isEnabledDictionary.TryGetS(u.Mixin<UserAzureADMixin>().OID!.Value) != true)
                        {
                            stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with OID {u.Mixin<UserAzureADMixin>().OID} has been deactivated in Azure AD");
                            u.Execute(UserOperation.AutoDeactivate);
                        }

                        if (u.State == UserState.AutoDeactivate && isEnabledDictionary.TryGetS(u.Mixin<UserAzureADMixin>().OID!.Value) == true)
                        {
                            stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with OID {u.Mixin<UserAzureADMixin>().OID} has been reactivated in Azure AD");
                            u.Execute(UserOperation.Reactivate);
                        }
                    }
                });

                return null;
            });
        }

        if (adGroupsAndQueries)
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

            QueryLogic.Queries.Register(AzureADQuery.ActiveDirectoryUsers, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
            {
                using (HeavyProfiler.Log("Microsoft Graph", () => "ActiveDirectoryUsers"))
                {
                    var tokenCredential = GetTokenCredential();
                    GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

                    var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "InGroup" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();

                    UserCollectionResponse response;
                    try
                    {
                        var converter = new MicrosoftGraphQueryConverter();

                        if (inGroup?.Value is Lite<ADGroupEntity> group)
                        {
                            response = (await graphClient.Groups[group.Id.ToString()].TransitiveMembers.GraphUser.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns);
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders);
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                            }))!;
                        }
                        else
                        {
                            response = (await graphClient.Users.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns);
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders);
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                            }))!;
                        }
                    }
                    catch (ODataError e)
                    {
                        throw new ODataException(e);
                    }

                    var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

                    return response.Value!.Skip(skip).Select(u => new
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
                    }).ToDEnumerable(queryDescription).Select(request.Columns).WithCount((int?)response.OdataCount);
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

            QueryLogic.Queries.Register(AzureADQuery.ActiveDirectoryGroups, () => DynamicQueryCore.Manual(async (request, queryDescription, cancellationToken) =>
            {
                using (HeavyProfiler.Log("Microsoft Graph", () => "ActiveDirectoryGroups"))
                {
                    var tokenCredential = GetTokenCredential();
                    GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

                    var inGroup = (FilterCondition?)request.Filters.Extract(f => f is FilterCondition fc && fc.Token.Key == "HasUser" && fc.Operation == FilterOperation.EqualTo).SingleOrDefaultEx();


                    GroupCollectionResponse response;
                    try
                    {
                        var converter = new MicrosoftGraphQueryConverter();
                        if (inGroup?.Value is Lite<UserEntity> user)
                        {
                            var oid = user.InDB(a => a.Mixin<UserAzureADMixin>().OID);
                            if (oid == null)
                                throw new InvalidOperationException($"User {user} has no OID");

                            response = (await graphClient.Users[oid.ToString()].TransitiveMemberOf.GraphGroup.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns);
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders);
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                            }))!;
                        }
                        else
                        {
                            response = (await graphClient.Groups.GetAsync(req =>
                            {
                                req.QueryParameters.Filter = converter.GetFilters(request.Filters);
                                req.QueryParameters.Search = converter.GetSearch(request.Filters);
                                req.QueryParameters.Select = converter.GetSelect(request.Columns);
                                req.QueryParameters.Orderby = converter.GetOrderBy(request.Orders);
                                req.QueryParameters.Top = converter.GetTop(request.Pagination);
                                req.QueryParameters.Count = true;
                                req.Headers.Add("ConsistencyLevel", "eventual");
                            }))!;
                        }
                    }
                    catch (ODataError e)
                    {
                        throw new ODataException(e);
                    }

                    var skip = request.Pagination is Pagination.Paginate p ? (p.CurrentPage - 1) * p.ElementsPerPage : 0;

                    return response.Value!.Skip(skip).Select(u => new
                    {
                        Entity = (Lite<Entities.Entity>?)null,
                        u.Id,
                        u.DisplayName,
                        u.Description,
                        u.SecurityEnabled,
                        u.Visibility,
                        HasUser = (Lite<UserEntity>?)null,
                    }).ToDEnumerable(queryDescription).Select(request.Columns).WithCount((int?)response.OdataCount);
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

            if (sb.WebServerBuilder != null)
            {
                ReflectionServer.RegisterLike(typeof(ActiveDirectoryPermission), () => ActiveDirectoryPermission.InviteUsersFromAD.IsAuthorized());
                ReflectionServer.RegisterLike(typeof(OnPremisesExtensionAttributesModel), () =>  
        						QueryLogic.Queries.QueryAllowed(AzureADQuery.ActiveDirectoryGroups, false) ||
                	QueryLogic.Queries.QueryAllowed(AzureADQuery.ActiveDirectoryUsers, false));
            }
        }
        else
        {
            if (sb.WebServerBuilder != null)
            {
                ReflectionServer.RegisterLike(typeof(ActiveDirectoryPermission), () => ActiveDirectoryPermission.InviteUsersFromAD.IsAuthorized());
                ReflectionServer.RegisterLike(typeof(OnPremisesExtensionAttributesModel), () => false);
            }
        }

    }



    public static async Task<List<ActiveDirectoryUser>> FindActiveDirectoryUsers(string subStr, int top, CancellationToken token)
    {
        var tokenCredential = GetTokenCredential();
        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        subStr = subStr.Replace("'", "''");

        var query = subStr.Contains("@") ? $"mail eq '{subStr}'" :
            subStr.Contains(",") ? $"startswith(givenName, '{subStr.After(",").Trim()}') AND startswith(surname, '{subStr.Before(",").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
            subStr.Contains(" ") ? $"startswith(givenName, '{subStr.Before(" ").Trim()}') AND startswith(surname, '{subStr.After(" ").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
             $"startswith(givenName, '{subStr}') OR startswith(surname, '{subStr}') OR startswith(displayname, '{subStr.Trim()}') OR startswith(mail, '{subStr.Trim()}')";

        var result = await graphClient.Users.GetAsync(req =>
        {
            req.QueryParameters.Top = top;
            req.QueryParameters.Filter = query;
        }, token);

        return result!.Value!.Select(a => new ActiveDirectoryUser
        {
            UPN = a.UserPrincipalName!,
            DisplayName = a.DisplayName!,
            JobTitle = a.JobTitle!,
            ObjectID = Guid.Parse(a.Id!),
            SID = null,
        }).ToList();
    }

    public static async Task<ActiveDirectoryUser> GetActiveDirectoryUser(Guid oid, CancellationToken token)
    {
        var tokenCredential = GetTokenCredential();
        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);

        var u = await graphClient.Users[oid.ToString()].GetAsync(cancellationToken: token);

        if (u == null)
            throw new Exception("User with OID '" + oid.ToString() + "' not found in Active Directory");
        else
        {
            return new ActiveDirectoryUser
            {
                UPN = u.UserPrincipalName!,
                DisplayName = u.DisplayName!,
                JobTitle = u.JobTitle!,
                ObjectID = Guid.Parse(u.Id!),
                SID = null,
            };
        }
    }

    public static TimeSpan CacheADGroupsFor = new TimeSpan(0, minutes: 30, 0);

    static ConcurrentDictionary<Lite<UserEntity>, (DateTime date, List<SimpleGroup> groups)> ADGroupsCache = new ConcurrentDictionary<Lite<UserEntity>, (DateTime date, List<SimpleGroup> groups)>();

    public static List<SimpleGroup> CurrentADGroups()
    {
        var oid = UserAzureADMixin.CurrentOID;
        if (oid == null)
            return new List<SimpleGroup>();

        var tuple = ADGroupsCache.AddOrUpdate(UserEntity.Current,
            addValueFactory: user => (Clock.Now, CurrentADGroupsInternal(oid.Value)),
            updateValueFactory: (user, old) => old.date.Add(CacheADGroupsFor) > Clock.Now ? old : (Clock.Now, CurrentADGroupsInternal(oid.Value)));

        return tuple.groups;
    }

    //Uses application permissions
    public static List<SimpleGroup> CurrentADGroupsInternal(Guid oid)
    {
        using (HeavyProfiler.Log("Microsoft Graph", () => "CurrentADGroups for OID: " + oid))
        {
            var tokenCredential = GetTokenCredential();
            GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);
            var result = graphClient.Users[oid.ToString()].TransitiveMemberOf.GraphGroup.GetAsync(req =>
            {
                req.QueryParameters.Top = 999;
                req.QueryParameters.Select = new[] { "id", "displayName", "ODataType" };
            }).Result;

            return result!.Value!.Select(di => new SimpleGroup(Guid.Parse(di.Id!), di.DisplayName)).ToList();
        }
    }

    //Uses delegated permissions
    public static List<SimpleGroup> CurrentADGroupsInternal(string accessToken)
    {
        using (HeavyProfiler.Log("Microsoft Graph", () => "CurrentADGroups for OID: " + accessToken))
        {
            var tokenCredential = new AccessTokenCredential(accessToken);
            GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);
            var result = graphClient.Me.TransitiveMemberOf.GraphGroup.GetAsync(req =>
            {
                req.QueryParameters.Top = 999;
                req.QueryParameters.Select = new[] { "id", "displayName", "ODataType" };
            }).ResultSafe();

            return result!.Value!.Select(di => new SimpleGroup(Guid.Parse(di.Id!), di.DisplayName)).ToList();
        }
    }




    public static UserEntity CreateUserFromAD(ActiveDirectoryUser adUser)
    {
        var adAuthorizer = (AzureADAuthorizer)AuthLogic.Authorizer!;
        var config = adAuthorizer.GetConfig();

        var acuCtx = GetMicrosoftGraphContext(adUser);

        using (Security.ExecutionMode.Global())
        {
            using (var tr = new Transaction())
            {
                var user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserAzureADMixin>().OID == acuCtx.OID);
                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == acuCtx.UserName) ??
                           (acuCtx.UserName.Contains("@") && config!.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == acuCtx.UserName || a.UserName == acuCtx.UserName.Before("@")) : null);
                }

                if (user != null)
                {
                    adAuthorizer.UpdateUser(user, acuCtx);

                    return tr.Commit(user);
                }

                var result = adAuthorizer.OnCreateUser(acuCtx);

                return tr.Commit(result);
            }
        }
    }

    private static MicrosoftGraphCreateUserContext GetMicrosoftGraphContext(ActiveDirectoryUser adUser)
    {
        var tokenCredential = GetTokenCredential();
        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);
        var msGraphUser = graphClient.Users[adUser.ObjectID.ToString()].GetAsync().Result;

        return new MicrosoftGraphCreateUserContext(msGraphUser!);
    }


    public static Task<MemoryStream> GetUserPhoto(Guid oid, int size)
    {
        var tokenCredential = GetTokenCredential();
        GraphServiceClient graphClient = new GraphServiceClient(tokenCredential);
        int imageSize = ToAzureSize(size);

        return graphClient.Users[oid.ToString()].Photos[$"{imageSize}x{imageSize}"].Content.GetAsync().ContinueWith(photo =>
        {
            MemoryStream ms = new MemoryStream();
            photo.Result!.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public static int ToAzureSize(int size) => 
        size <= 48 ? 48 :
        size <= 64 ? 64 :
        size <= 96 ? 96 :
        size <= 120 ? 120 :
        size <= 240 ? 240 :
        size <= 360 ? 360 :
        size <= 432 ? 432 :
        size <= 504 ? 504 : 648;
}

public record SimpleGroup(Guid Id, string? DisplayName);

public class MicrosoftGraphCreateUserContext : IAutoCreateUserContext
{
    public MicrosoftGraphCreateUserContext(User user)
    {
        User = user;
    }

    public User User { get; set; }

    public string UserName => User.UserPrincipalName!;
    public string? EmailAddress => User.UserPrincipalName;

    public string FirstName => User.GivenName ?? User.DisplayName.TryBefore(" ") ?? User.DisplayName!;
    public string LastName => User.Surname ?? User.DisplayName.TryAfter(" ") ?? User.DisplayName!;

    public Guid? OID => Guid.Parse(User.Id!);

    public string? SID => null;
}



[Serializable]
public class ODataException : Exception
{
    public ODataException() { }
    public ODataException(ODataError error) : base(error.Error?.Message ?? error.Message)
    {
        Data["MainError"] = error.Error;
    }
}


public class OnPremisesExtensionAttributesModel : ModelEntity
{
    public string? ExtensionAttribute1 { get; set; }
    public string? ExtensionAttribute2 { get; set; }
    public string? ExtensionAttribute3 { get; set; }
    public string? ExtensionAttribute4 { get; set; }
    public string? ExtensionAttribute5 { get; set; }
    public string? ExtensionAttribute6 { get; set; }
    public string? ExtensionAttribute7 { get; set; }
    public string? ExtensionAttribute8 { get; set; }
    public string? ExtensionAttribute9 { get; set; }
    public string? ExtensionAttribute10 { get; set; }
    public string? ExtensionAttribute11 { get; set; }
    public string? ExtensionAttribute12 { get; set; }
    public string? ExtensionAttribute13 { get; set; }
    public string? ExtensionAttribute14 { get; set; }
    public string? ExtensionAttribute15 { get; set; }
}
