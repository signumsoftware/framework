using Microsoft.Extensions.Logging.Abstractions;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using Signum.Authorization.Rules;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Frozen;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Signum.Engine.Sync.Replacements;

namespace Signum.Authorization;

public static class AuthLogic
{
    public static event Action<UserEntity, string/*loginMethod*/>? UserLogingIn;
    public static ICustomAuthorizer? Authorizer;

    public static ResetLazy<HashSet<Lite<UserEntity>>> RecentlyUsersDisabled;

    public static void CheckUserActive(UserEntity user)
    {
        if (user.State != UserState.Active || AuthLogic.RecentlyUsersDisabled.Value.Contains(user.ToLite()))
            throw new UnauthorizedAccessException(UserMessage.UserIsNotActive.NiceToString());
    }

    /// <summary>
    /// Gets or sets the number of failed login attempts allowed before a user is locked out.
    /// </summary>
    public static int? MaxFailedLoginAttempts { get; set; }

    public static string? SystemUserName { get; private set; }
    static ResetLazy<UserEntity?> systemUserLazy = GlobalLazy.WithoutInvalidations(() => SystemUserName == null ? null :
        Database.Query<UserEntity>().Where(u => u.UserName == SystemUserName)
        .SingleEx(() => "SystemUser with name '{0}'".FormatWith(SystemUserName)));
    public static UserEntity? SystemUser
    {
        get { return systemUserLazy.Value; }
    }

    public static string? AnonymousUserName { get; private set; }
    static ResetLazy<UserEntity?> anonymousUserLazy = GlobalLazy.WithoutInvalidations(() => AnonymousUserName == null ? null :
        Database.Query<UserEntity>().Where(u => u.UserName == AnonymousUserName)
        .SingleEx(() => "AnonymousUser with name '{0}'".FormatWith(AnonymousUserName)));

    public static UserEntity? AnonymousUser
    {
        get { return anonymousUserLazy.Value; }
    }

    [AutoExpressionField]
    public static IQueryable<UserEntity> Users(this RoleEntity r) =>
        As.Expression(() => Database.Query<UserEntity>().Where(u => u.Role.Is(r)));

    [AutoExpressionField]
    public static IQueryable<RoleEntity> UsedByRoles(this RoleEntity r) =>
        As.Expression(() => Database.Query<RoleEntity>().Where(u => u.InheritsFrom.Contains(r.ToLite())));

    static ResetLazy<DirectedGraph<Lite<RoleEntity>>> rolesGraph = null!;
    static ResetLazy<DirectedGraph<Lite<RoleEntity>>> rolesInverse = null!;
    static ResetLazy<FrozenDictionary<string, Lite<RoleEntity>>> rolesByName = null!;
    public static ResetLazy<FrozenDictionary<Lite<RoleEntity>, RoleEntity>> RolesByLite = null!;


    class RoleData
    {
        public bool DefaultAllowed;
        public MergeStrategy MergeStrategy;
    }

    static ResetLazy<FrozenDictionary<Lite<RoleEntity>, RoleData>> mergeStrategies = null!;

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => AuthLogic.Start(null!, null, null)));
    }

    public static void Start(SchemaBuilder sb, string? systemUserName, string? anonymousUserName)
    {   
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        SystemUserName = systemUserName;
        AnonymousUserName = anonymousUserName;

        UserLogingIn += OnLogin_UpdateUserCulture;

        RoleEntity.RetrieveFromCache = r => RolesByLite.Value.GetOrThrow(r);

        UserWithClaims.FillClaims += (userWithClaims, user) =>
        {
            userWithClaims.Claims["Role"] = ((UserEntity)user).Role;
            userWithClaims.Claims["Culture"] = ((UserEntity)user).CultureInfo?.Name;
        };

        CultureInfoLogic.AssertStarted(sb);
        RecentlyUsersDisabled = sb.GlobalLazy(() => Database.Query<UserEntity>().Where(u => u.DisabledOn != null && AuthTokenServer.GetTokenLimitDate() < u.DisabledOn).Select(a => a.ToLite()).ToHashSet(),
         new InvalidateWith(null));

        sb.Include<UserEntity>()
          .WithExpressionFrom((RoleEntity r) => r.Users())
           .WithIndex(a => new { a.DisabledOn })
          .WithQuery(() => e => new
          {
              Entity = e,
              e.Id,
              e.UserName,
              e.Email,
              e.Role,
              e.State,
              e.CultureInfo,
          });

        QueryLogic.Expressions.Register((RoleEntity r) => r.UsedByRoles(), AuthAdminMessage.UsedByRoles);

        sb.Include<RoleEntity>()
            .WithSave(RoleOperation.Save)
            .WithDelete(RoleOperation.Delete)
            .WithQuery(() => r => new
            {
                Entity = r,
                r.Id,
                r.Name,
                r.Description,
            });

        RolesByLite = sb.GlobalLazy(() => Database./*Query*/RetrieveAll<RoleEntity>().ToFrozenDictionaryEx(a => a.ToLite()), new InvalidateWith(typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);
        rolesByName = sb.GlobalLazy(() => RolesByLite.Value.Keys.ToFrozenDictionaryEx(a => a.ToString()!), new InvalidateWith(typeof(RoleEntity)));
        rolesGraph = sb.GlobalLazy(() => CacheRoles(RolesByLite.Value), new InvalidateWith(typeof(RoleEntity)));
        rolesInverse = sb.GlobalLazy(() => rolesGraph.Value.Inverse(), new InvalidateWith(typeof(RoleEntity)));
        mergeStrategies = sb.GlobalLazy(() =>
        {
            var strategies = Database.Query<RoleEntity>().Select(r => KeyValuePair.Create(r.ToLite(), r.MergeStrategy)).ToDictionary();

            var graph = rolesGraph.Value;

            Dictionary<Lite<RoleEntity>, RoleData> result = new Dictionary<Lite<RoleEntity>, RoleData>();
            foreach (var r in graph.CompilationOrder())
            {
                var strat = strategies.GetOrThrow(r);

                var baseValues = graph.RelatedTo(r).Select(r2 => result[r2].DefaultAllowed);

                result.Add(r, new RoleData
                {
                    MergeStrategy = strat,
                    DefaultAllowed = strat == MergeStrategy.Union ? baseValues.Any(a => a) : baseValues.All(a => a)
                });
            }

            return result.ToFrozenDictionary();
        }, new InvalidateWith(typeof(RoleEntity)));

        sb.Schema.EntityEvents<RoleEntity>().Saving += Schema_Saving;
        UserGraph.Register();


    }

    private static void OnLogin_UpdateUserCulture(UserEntity user, string loginMethod)
    {
        if(user.CultureInfo == null && SignumCurrentContextFilter.CurrentContext is { } cc)
        {
            using (Disable())
            using (OperationLogic.AllowSave<UserEntity>())
            {
                user.CultureInfo = CultureServer.InferUserCulture(cc.HttpContext);
                UserHolder.Current = new UserWithClaims(user);
                user.Save();
            }
        }
    }

    public static Lite<RoleEntity> GetOrCreateTrivialMergeRole(List<Lite<RoleEntity>> roles, Dictionary<string, Lite<RoleEntity>>? newRoles = null)
    {
        roles = roles.Distinct().ToList();

        if (roles.Count == 1)
            return roles.SingleEx();

        var flatRoles = roles
            .Select(a => RolesByLite.Value.TryGetC(a) ?? a.EntityOrNull ?? a.Retrieve())
            .ToList()
            .SelectMany(a => a.IsTrivialMerge ? a.InheritsFrom.ToArray() : new[] { a.ToLite() })
            .Distinct()
            .ToList();

        if (flatRoles.Count == 1)
            return flatRoles.SingleEx();

        var newName = RoleEntity.CalculateTrivialMergeName(flatRoles);

        var db = rolesByName.Value.TryGetC(newName) ??
            newRoles?.TryGetC(newName);

        if (db != null)
            return db;

        using (AuthLogic.Disable()) 
        using (OperationLogic.AllowSave<RoleEntity>())
        {
            var result = new RoleEntity
            {
                Name = newName,
                MergeStrategy = MergeStrategy.Union,
                Description = null,
                IsTrivialMerge = true,
                InheritsFrom = flatRoles.ToMList()
            }.Save();

            return result.ToLite();
        }
    }

    static void Schema_Saving(RoleEntity role)
    {
        if (!role.IsNew)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                EntityCache.AddFullGraph(role);
                var allRoles = Database.RetrieveAll<RoleEntity>();

                if (role.InheritsFrom.IsGraphModified)
                {
                    var roleGraph = DirectedGraph<RoleEntity>.Generate(allRoles, r => r.InheritsFrom.Select(sr => sr.RetrieveAndRemember()));

                    var problems = roleGraph.FeedbackEdgeSet().Edges.ToList();

                    if (problems.Count > 0)
                        throw new ApplicationException(
                            AuthAdminMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString(problems.Count) +
                            problems.ToString("\n"));
                }

                var dic = allRoles.ToDictionary(a => a.ToLite());

                var problems2 = allRoles.SelectMany(r => r.InheritsFrom.Where(inh => RolesByLite.Value.GetOrThrow(inh).IsTrivialMerge).Select(inh => new { r, inh })).ToList();
                if (problems2.Any())
                    throw new ApplicationException(
                        problems2.GroupBy(a => a.r, a => a.inh)
                        .Select(gr => AuthAdminMessage.Role0InheritsFromTrivialMergeRole1.NiceToString(gr.Key, gr.CommaAnd()))
                        .ToString("\n"));
            }

            if (!role.IsTrivialMerge)
            {
                var trivialDependant = Database.Query<RoleEntity>().Where(a => a.IsTrivialMerge && a.InheritsFrom.Contains(role.ToLite())).ToList();

                if (trivialDependant.Any())
                {
                    if (role.Name != role.InDB(a => a.Name))
                    {
                        foreach (var item in trivialDependant)
                        {
                            var replaced = item.InheritsFrom.Select(r => r.Is(role) ? role.ToLite() : r);

                            var newName = RoleEntity.CalculateTrivialMergeName(replaced);

                            item.InDB().UnsafeUpdate(a => a.Name, a => newName);
                        }
                    }
                }
            }
        }
        else
        {
            if (role.InheritsFrom.Any())
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    var problems = role.InheritsFrom.Where(a => a.EntityOrNull?.IsTrivialMerge ?? a.InDB(a => a.IsTrivialMerge)).ToList();

                    if (problems.Any())
                    {
                        throw new ApplicationException(AuthAdminMessage.Role0InheritsFromTrivialMergeRole1.NiceToString(role, problems.CommaAnd()));
                    }
                }
            }
        }
    }

    static DirectedGraph<Lite<RoleEntity>> CacheRoles(FrozenDictionary<Lite<RoleEntity>, RoleEntity> rolesLite)
    {
        var graph = DirectedGraph<Lite<RoleEntity>>.Generate(rolesLite.Keys, r => rolesLite.GetOrThrow(r).InheritsFrom);

        var problems = graph.FeedbackEdgeSet().Edges.ToList();

        if (problems.Count > 0)
            throw new ApplicationException(
                AuthAdminMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                problems.ToString("\n"));

        return graph;

    }

    public static IDisposable UnsafeUserSession(string username)
    {
        UserEntity? user;
        using (AuthLogic.Disable())
        {
            user = RetrieveUser(username);
            if (user == null)
                throw new ApplicationException(LoginAuthMessage.Username0IsNotValid.NiceToString().FormatWith(username));
        }

        return UserHolder.UserSession(user);
    }

    public static Func<string, UserEntity?> RetrieveUserByUsername = (username) => Database.Query<UserEntity>().Where(u => u.UserName.ToLower() == username.ToLower()).SingleOrDefaultEx();

    public static UserEntity? RetrieveUser(string username)
    {
        var result = RetrieveUserByUsername(username);

        if (result != null && result.State == UserState.Deactivated)
            throw new ApplicationException(LoginAuthMessage.User0IsDeactivated.NiceToString().FormatWith(result.UserName));

        return result;
    }

    public static IEnumerable<Lite<RoleEntity>> RolesInOrder(bool includeTrivialMerge)
    {
        return rolesGraph.Value.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.ToString()))
            .Where(r => includeTrivialMerge || !RolesByLite.Value.GetOrCreate(r).IsTrivialMerge);
    }

    internal static DirectedGraph<Lite<RoleEntity>> RolesGraph()
    {
        return rolesGraph.Value;
    }

    public static Lite<RoleEntity> GetRole(string roleName)
    {
        return rolesByName.Value.GetOrThrow(roleName);
    }

    public static Lite<RoleEntity>? TryGetRole(string roleName)
    {
        return rolesByName.Value.TryGetC(roleName);
    }

    public static IEnumerable<Lite<RoleEntity>> RelatedTo(Lite<RoleEntity> role)
    {
        return rolesGraph.Value.RelatedTo(role);
    }

    public static MergeStrategy GetMergeStrategy(Lite<RoleEntity> role)
    {
        return mergeStrategies.Value.GetOrThrow(role).MergeStrategy;
    }

    public static bool GetDefaultAllowed(Lite<RoleEntity> role)
    {
        return mergeStrategies.Value.GetOrThrow(role).DefaultAllowed;
    }

    static bool globallyEnabled = true;
    public static bool GloballyEnabled
    {
        get { return globallyEnabled; }
        set { globallyEnabled = value; }
    }

    static readonly Variable<bool?> tempEnabled = Statics.ThreadVariable<bool?>("authTempDisabled");

    public static IDisposable Disable()
    {
        var oldValue = tempEnabled.Value;
        tempEnabled.Value = false;
        return new Disposable(() => tempEnabled.Value = oldValue);
    }

    public static IDisposable Enable()
    {
        var oldValue = tempEnabled.Value;
        tempEnabled.Value = true;
        return new Disposable(() => tempEnabled.Value = oldValue);
    }

    public static bool IsEnabled
    {
        get { return tempEnabled.Value ?? globallyEnabled; }
    }

    public static event Action? OnRulesChanged;

    public static void NotifyRulesChanged()
    {
        OnRulesChanged?.Invoke();
    }

    public static UserEntity Login(string username, IList<byte[]> passwordHashes, out string authenticationType)
    {
        using (AuthLogic.Disable())
        {
            UserEntity user = RetrieveUser(username, passwordHashes);

            OnUserLogingIn(user, nameof(Login));

            authenticationType = "database";

            return user;
        }
    }

    public static void OnUserLogingIn(UserEntity user, string loginMethod)
    {
        UserLogingIn?.Invoke(user, loginMethod);
    }

    public static Action<UserEntity>? OnDeactivateUser;

    public static UserEntity RetrieveUser(string username, IList<byte[]> passwordHashes)
    {
        using (AuthLogic.Disable())
        {
            UserEntity? user = RetrieveUser(username);

            if (user == null)
                throw new IncorrectUsernameException(LoginAuthMessage.Username0IsNotValid.NiceToString().FormatWith(username));


            if (user.PasswordHash == null || (!passwordHashes.Any(passwordHash => passwordHash.SequenceEqual(user.PasswordHash))))
            {
                using (UserHolder.UserSession(SystemUser!))
                {
                    user.LoginFailedCounter++;
                    user.Execute(UserOperation.Save);

                    if (MaxFailedLoginAttempts.HasValue &&
                        user.LoginFailedCounter == MaxFailedLoginAttempts &&
                        user.State == UserState.Active)
                    {
                        OnDeactivateUser?.Invoke(user);

                        user.Execute(UserOperation.Deactivate);

                        throw new UserLockedException(LoginAuthMessage.User0IsDeactivated.NiceToString()
                            .FormatWith(user.UserName));
                    }

                    throw new IncorrectPasswordException(LoginAuthMessage.IncorrectPassword.NiceToString());
                }
            }

            if (user.LoginFailedCounter > 0)
            {
                using (UserHolder.UserSession(SystemUser!))
                {
                    user.LoginFailedCounter = 0;
                    user.Execute(UserOperation.Save);
                }
            }

            if (!user.PasswordHash.SequenceEqual(passwordHashes.Last()))
            {
                user.PasswordHash = passwordHashes.Last();

                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    user.Save();
                }
            }

            return user;
        }
    }

    public static UserEntity? TryRetrieveUser(string username, IList<byte[]> passwordHashes)
    {
        using (AuthLogic.Disable())
        {
            UserEntity? user = RetrieveUser(username);
            if (user == null)
                return null;

            if (user.PasswordHash == null || !passwordHashes.Any(passwordHash => passwordHash.SequenceEqual(user.PasswordHash)))
                return null;

            return user;
        }
    }

    public static void StartAllModules(SchemaBuilder sb, Func<AuthTokenConfigurationEmbedded>? tokenConfig)
    {
        TypeAuthLogic.Start(sb);
        PropertyAuthLogic.Start(sb);
        QueryAuthLogic.Start(sb);
        OperationAuthLogic.Start(sb);
        PermissionAuthLogic.Start(sb);

        if (sb.WebServerBuilder != null && tokenConfig != null)
            AuthServer.Start(tokenConfig, sb.WebServerBuilder.AuthTokenEncryptionKey);
    }

    public static HashSet<Lite<RoleEntity>> CurrentRoles()
    {
        return rolesGraph.Value.IndirectlyRelatedTo(RoleEntity.Current, true);
    }

    public static HashSet<Lite<RoleEntity>> IndirectlyRelated(Lite<RoleEntity> role)
    {
        return rolesGraph.Value.IndirectlyRelatedTo(role, true);
    }

    public static HashSet<Lite<RoleEntity>> InverseIndirectlyRelated(Lite<RoleEntity> role)
    {
        return rolesInverse.Value.IndirectlyRelatedTo(role, true);
    }

    internal static int Rank(Lite<RoleEntity> role)
    {
        return rolesGraph.Value.IndirectlyRelatedTo(role).Count;
    }

    public static event Func<bool, XElement>? ExportToXml;
    public static event Func<XElement, Dictionary<string, Lite<RoleEntity>>, Replacements, SqlPreCommand?>? ImportFromXml;

    public static XDocument ExportRules(bool exportAll = false)
    {
        SystemEventLogLogic.Log("Export AuthRules");

        var rolesDic = Database.Query<RoleEntity>().ToDictionary(a => a.ToLite());

        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("Auth",
                new XElement("Roles",
                    RolesInOrder(includeTrivialMerge: false).Select(r => new XElement("Role",
                        new XAttribute("Name", r.ToString()!),
                        GetMergeStrategy(r) == MergeStrategy.Intersection ? new XAttribute("MergeStrategy", MergeStrategy.Intersection) : null!,
                        new XAttribute("Contains", rolesGraph.Value.RelatedTo(r).ToString(",")),
                        rolesDic.TryGetC(r)?.Description?.Let(d => new XAttribute("Description", d))
                        ))),
                 ExportToXml?.GetInvocationListTyped().Select(a => a(exportAll)).NotNull().OrderBy(a => a.Name.ToString())!));
    }

    public static SqlPreCommand? ImportRulesScript(XDocument doc, bool interactive)
    {
        Replacements replacements = new Replacements { Interactive = interactive };

        Dictionary<string, Lite<RoleEntity>> rolesDic = Database.Query<RoleEntity>().Where(a => a.IsTrivialMerge == false).Select(r => KeyValuePair.Create(r.ToString(), r.ToLite())).ToDictionaryEx();
        Dictionary<string, XElement> rolesXml = doc.Root!.Element("Roles")!.Elements("Role").Where(a => a.Attribute("IsTrivialMerge")?.Value.ToBool() != true).ToDictionary(x => x.Attribute("Name")!.Value);

        replacements.AskForReplacements(rolesXml.Keys.ToHashSet(), rolesDic.Keys.ToHashSet(), "Roles");

        rolesDic = replacements.ApplyReplacementsToNew(rolesDic, "Roles");

        try
        {
            var xmlOnly = rolesXml.Keys.Except(rolesDic.Keys).ToList();
            if (xmlOnly.Any())
                throw new InvalidOperationException("roles {0} not found on the database".FormatWith(xmlOnly.ToString(", ")));

            foreach (var kvp in rolesXml)
            {
                var r = rolesDic.GetOrThrow(kvp.Key);

                var xmlName = kvp.Value.Attribute("Name")!.Value;
                if (r.ToString() != xmlName)
                    throw new InvalidOperationException($"Role {r} has been renamed to {xmlName}");

                {
                    var currentMergeStrategy = GetMergeStrategy(r);
                    var shouldMergeStrategy = kvp.Value.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union;

                    if (currentMergeStrategy != shouldMergeStrategy)
                        throw new InvalidOperationException("Merge strategy of {0} is {1} in the database but is {2} in the file".FormatWith(r, currentMergeStrategy, shouldMergeStrategy));

                }

                {
                    var currentTrivialMerge = RolesByLite.Value.GetOrThrow(r).IsTrivialMerge;
                    var shouldTrivialMerge = kvp.Value.Attribute("IsTrivialMerge")?.Value.ToBool() ?? false;

                    if (currentTrivialMerge != shouldTrivialMerge)
                        throw new InvalidOperationException("{0} is Trivial Merge {1} in the database but is {2} in the file".FormatWith(r, currentTrivialMerge, shouldTrivialMerge));
                }

                EnumerableExtensions.JoinStrict(
                    rolesGraph.Value.RelatedTo(r),
                    kvp.Value.Attribute("Contains")!.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    sr => sr.ToString()!,
                    s => rolesDic.GetOrThrow(s).ToString()!,
                    (sr, s) => 0,
                    "subRoles of {0}".FormatWith(r));
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidRoleGraphException("The role graph does not match:\n" + ex.Message);
        }

        var dbOnlyWarnings = rolesDic.Keys.Except(rolesXml.Keys).Select(n =>
                new SqlPreCommandSimple("-- Alien role {0} not configured!!".FormatWith(n))
            ).Combine(Spacing.Simple);

        SqlPreCommand? result = ImportFromXml.GetInvocationListTyped()
            .Select(inv => inv(doc.Root, rolesDic, replacements)).Combine(Spacing.Triple);

        if (replacements.Values.Any(a => a.Any()))
            SafeConsole.WriteLineColor(ConsoleColor.Red, "There are renames! Remember to export after executing the script");

        if (result == null && dbOnlyWarnings == null)
            return null;


        return SqlPreCommand.Combine(Spacing.Triple,
            new SqlPreCommandSimple("-- BEGIN AUTH SYNC SCRIPT"),
            Connector.Current.SqlBuilder.UseDatabase(),
            dbOnlyWarnings,
            result,
            new SqlPreCommandSimple("-- END AUTH SYNC SCRIPT"));
    }


    public static void LoadRoles() => LoadRoles(XDocument.Load("AuthRules.xml"));
    public static void LoadRoles(XDocument doc)
    {
        var roleInfos = doc.Root!.Element("Roles")!.Elements("Role").Select(x => new
        {
            Name = x.Attribute("Name")!.Value,
            MergeStrategy = x.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union,
            IsTrivialMerge = x.Attribute("IsTrivialMerge")?.Value.ToBool() ?? false,
            SubRoles = x.Attribute("Contains")!.Value.SplitNoEmpty(','),
            Description = x.Attribute("Description")?.Value,
        }).ToList();

        var roles = roleInfos.ToDictionary(a => a.Name!, a => new RoleEntity
        {
            Name = a.Name!,
            MergeStrategy = a.MergeStrategy,
            IsTrivialMerge = a.IsTrivialMerge,
            Description = a.Description,
        });

        foreach (var ri in roleInfos)
        {
            roles[ri.Name].InheritsFrom = ri.SubRoles.Select(r => roles.GetOrThrow(r).ToLiteFat()).ToMList();
        }

        using (OperationLogic.AllowSave<RoleEntity>())
            roles.Values.SaveList();
    }

    public static void SynchronizeRoles(XDocument doc, bool interactive, Func<AutoReplacementContext, Selection?>? autoReplacement = null)
    {
        Table table = Schema.Current.Table(typeof(RoleEntity));
        TableMList relationalTable = table.TablesMList().Single();

        Dictionary<string, XElement> rolesXml = doc.Root!.Element("Roles")!.Elements("Role").ToDictionary(x => x.Attribute("Name")!.Value);

        Dictionary<string, RoleEntity> rolesDic = AuthLogic.RolesInOrder(includeTrivialMerge: false).Reverse().Select(r => AuthLogic.RolesByLite.Value.GetOrThrow(r)).ToDictionary(a => a.ToString());
        
        Replacements replacements = new Replacements { Interactive = interactive, AutoReplacement = autoReplacement };

        replacements.AskForReplacements(rolesDic.Keys.ToHashSet(), rolesXml.Keys.ToHashSet(), "Roles");
        rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");

        {
            Console.WriteLine("Part 1: Synchronize roles without relationships");

            var roleInsertsDeletes = Synchronizer.SynchronizeScript(Spacing.Double,
                rolesXml,
                rolesDic,
                createNew: (name, xElement) =>
                {
                    var newRole = new RoleEntity
                    {
                        Name = name,
                        MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union,
                        Description = xElement.Attribute("Description")?.Value,
                        IsTrivialMerge = false,
                    };

                    if (interactive)
                        return table.InsertSqlSync(newRole, includeCollections: false);
                    else
                    {
                        Console.WriteLine("Created:" + newRole.ToString());
                        newRole.Save();
                        return null;
                    }
                },

                removeOld: (name, role) =>
                {
                    if (interactive)
                    {
                        if (SafeConsole.Ask($"Delete role '{role}' from the database?"))
                            return new[]{
                                Administrator.UnsafeDeletePreCommandMList((RoleEntity e) => e.InheritsFrom, Database.MListQuery((RoleEntity e) => e.InheritsFrom).Where(r => r.Element.Entity.Name == role.Name)),
                                table.DeleteSqlSync(role, r => r.Name == role.Name)
                            }.Combine(Spacing.Simple);
                        else
                            return null;
                    }
                    else
                    {
                        if (Database.Query<UserEntity>().Where(u => u.Role.Is(role)).Any())
                        {
                            var alternative = role.InheritsFrom.FirstOrDefault()?.Retrieve() ??
                            Database.Query<RoleEntity>().FirstOrDefault(a => !a.IsTrivialMerge && a.MergeStrategy == MergeStrategy.Union && a.InheritsFrom.Count == 0) /*Min User*/ ??
                            throw new InvalidOperationException($"Unable to find alternative role for {role} to move the users to");

                            var updated = Database.Query<UserEntity>().Where(u => u.Role.Is(role)).UnsafeUpdate(a => a.Role, a => alternative.ToLite());

                            Console.WriteLine($"Moved {updated} users from role {role} to {alternative}");
                        }

                        foreach (var tm in Database.Query<RoleEntity>().Where(a=>a.IsTrivialMerge && a.InheritsFrom.Contains(role.ToLite())))
                        {
                            var alternative = AuthLogic.GetOrCreateTrivialMergeRole(tm.InheritsFrom.Where(a => !a.Is(role)).ToList());

                            if (Database.Query<UserEntity>().Where(u => u.Role.Is(tm)).Any())
                            {
                                var updated = Database.Query<UserEntity>().Where(u => u.Role.Is(tm)).UnsafeUpdate(a => a.Role, a => alternative);
                                Console.WriteLine($"Moved {updated} users from role {tm} to {alternative}");
                            }

                            tm.Delete();
                            Console.WriteLine("Deleted:" + tm.ToString());
                        }

                        Database.MListQuery((RoleEntity e) => e.InheritsFrom).Where(r => r.Element.Is(role)).UnsafeDeleteMList();
                        role.Delete();
                        Console.WriteLine("Deleted:" + role.ToString());
                        return null;
                    }
                },
                mergeBoth: (name, xElement, role) =>
                {
                    var oldName = role.Name;
                    role.Name = name;
                    role.MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union;
                    role.Description = xElement.Attribute("Description")?.Value;
                    role.IsTrivialMerge = false;
                    if (interactive)
                        return table.UpdateSqlSync(role, r => r.Name == oldName, includeCollections: false, comment: oldName);
                    else
                    {
                        if (role.IsGraphModified)
                        {
                            Console.WriteLine("Updated:" + role.ToString());
                            role.Save();
                        }
                        return null;
                    }
                });

            if (interactive)
            {
                if (roleInsertsDeletes != null)
                {
                    SqlPreCommand.Combine(Spacing.Triple,
                       new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                       Connector.Current.SqlBuilder.UseDatabase(),
                       roleInsertsDeletes,
                       new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT"))!.OpenSqlFileRetry();

                    if (!SafeConsole.Ask("Did you run the previous script (Sync Roles)?"))
                        return;
                }
                else
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "Already synchronized");
                }
                GlobalLazy.ResetAll(systemLog: false);
            }
        }

        {
            Console.WriteLine("Part 2: Synchronize roles relationships and trivial merges");
            rolesDic = Database.Query<RoleEntity>().Where(a => a.IsTrivialMerge == false).ToDictionary(a => a.ToString());
            rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");

            MList<Lite<RoleEntity>> ParseInheritedFrom(string contains)
            {
                return contains.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(rs => rolesDic.GetOrThrow(rs).ToLite()).ToMList();
            }

            var roleRelationships = Synchronizer.SynchronizeScript(Spacing.Double,
               rolesXml,
               rolesDic,
                createNew: (name, xElement) => { throw new InvalidOperationException("No new roles should be at this stage. Did you execute the script?"); },
                removeOld: (name, role) => { return null; },
                mergeBoth: (name, xElement, role) =>
                {
                    var should = ParseInheritedFrom(xElement.Attribute("Contains")!.Value);

                    if (!role.InheritsFrom.ToHashSet().SetEquals(should))
                        role.InheritsFrom = should;

                    if (interactive)
                    {
                        return table.UpdateSqlSync(role, r => r.Name == role.Name);
                    }
                    else
                    {
                        if (GraphExplorer.IsGraphModified(role))
                        {
                            Console.WriteLine("Updated:" + role.ToString());
                            role.Save();
                        }

                        return null;
                    }
                });



            var trivialMergeRoles = Database.Query<RoleEntity>().Where(a => a.IsTrivialMerge == true).ToList();

            var trivialMerges = trivialMergeRoles.Select(tr =>
            {
                var oldName = tr.Name;
                tr.Name = RoleEntity.CalculateTrivialMergeName(tr.InheritsFrom);
                if (!tr.IsGraphModified)
                    return null;

                if (interactive)
                    return table.UpdateSqlSync(tr, a => a.Name == oldName);
                else
                {
                    tr.Save();
                    return null;
                }
            }).Combine(Spacing.Double);

            if (roleRelationships != null || trivialMerges != null)
            {
                SqlPreCommand.Combine(Spacing.Triple,
                   new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                   Connector.Current.SqlBuilder.UseDatabase(),
                   roleRelationships,
                   trivialMerges,
                   new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT"))!.OpenSqlFileRetry();

                if (!SafeConsole.Ask("Did you run the previous script (Sync Roles Relationships)?"))
                    return;
            }
            else
            {
                SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
            }
        }

        GlobalLazy.ResetAll(systemLog: false);
    }


    public static void AutomaticImportAuthRules()
    {
        AutomaticImportAuthRules("AuthRules.xml");
    }

    public static void ImportAuthRules(XDocument authRules, bool interactive)
    {
        AuthLogic.ImportRulesScript(authRules, interactive: interactive)?.PlainSqlCommand().ExecuteLeaves();

        Schema.Current.InvalidateCache();
    }

    public static void AutomaticImportAuthRules(string fileName)
    {
        Schema.Current.Initialize();
        var script = AuthLogic.ImportRulesScript(XDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, fileName)), interactive: false);
        if (script == null)
        {
            SafeConsole.WriteColor(ConsoleColor.Green, "AuthRules already synchronized");
            return;
        }

        using (var tr = new Transaction())
        {
            SafeConsole.WriteColor(ConsoleColor.Yellow, "Executing AuthRules changes...");
            SafeConsole.WriteColor(ConsoleColor.DarkYellow, script.PlainSql());

            script.PlainSqlCommand().ExecuteLeaves();
            tr.Commit();
        }

        SystemEventLogLogic.Log("Import AuthRules");
    }

    public static void ImportExportAuthRules()
    {
        ImportExportAuthRules("AuthRules.xml");
    }

    public static void ImportExportAuthRules(string fileName)
    {
        void Import()
        {
            Console.Write("Reading {0}...".FormatWith(fileName));
            var doc = XDocument.Load(fileName);
            Console.WriteLine("Ok");

            Console.WriteLine("Generating SQL script to import auth rules (without modifying the role graph or entities):");
            SqlPreCommand? command;
            try
            {
                command = ImportRulesScript(doc, interactive: true);
            }
            catch (InvalidRoleGraphException ex)
            {
                SafeConsole.WriteLineColor(ConsoleColor.Red, ex.Message);

                if (SafeConsole.Ask("Sync roles first?"))
                    SyncRoles();

                return;
            }

            if (command == null)
                SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
            else
                command.OpenSqlFileRetry();

            GlobalLazy.ResetAll(systemLog: false);
        }

        void Export()
        {
            var doc = ExportRules();
            doc.Save(fileName);
            Console.WriteLine("Sucesfully exported to {0}".FormatWith(fileName));

            var info = new DirectoryInfo("../../../");

            if (info.Exists && SafeConsole.Ask($"Publish to '{info.Name}' directory (source code)?"))
                File.Copy(fileName, "../../../" + Path.GetFileName(fileName), overwrite: true);
        }

        void SyncRoles()
        {
            Console.Write("Reading {0}...".FormatWith(fileName));
            var doc = XDocument.Load(fileName);
            Console.WriteLine("Ok");


            Console.WriteLine("Generating script to synchronize roles...");

            SynchronizeRoles(doc, interactive: true);
            if (SafeConsole.Ask("Import rules now?"))
                Import();

        }

        void TrivialMergeRoles()
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser!))
            {
                using (var tr = new Transaction())
                {
                    var roles = AuthLogic.RolesByLite.Value;

                    var candidates = AuthLogic.RolesInOrder(false).Reverse().Where(r =>
                    {
                        var role = roles.GetOrThrow(r);
                        if (role.MergeStrategy == MergeStrategy.Intersection)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"{role} is Intersection");
                            return false;
                        }

                        if (role.InheritsFrom.Count == 0)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"{role} inherits from only 0 role");
                            return false;
                        }

                        foreach (var f in HasRuleOverridesEvent.GetInvocationListTyped())
                        {
                            if (f(r))
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"{role} has rules: " + f.Method.DeclaringType);
                                return false;
                            }
                        }

                        SafeConsole.WriteLineColor(ConsoleColor.Green, $"{role} can be converted to Trivial Merge Role");
                        return true;
                    }).ToList();

                    var nonCandidates = roles.Keys.Except(candidates).ToList();

                    IEnumerable<Lite<RoleEntity>> GetNonCandidates(Lite<RoleEntity> rol)
                    {
                        return AuthLogic.RelatedTo(rol).SelectMany(r => nonCandidates.Contains(r) ? new[] { r } : GetNonCandidates(r));
                    }

                    var newRoles = new Dictionary<string, Lite<RoleEntity>>();
                    foreach (var r in candidates)
                    {
                        var inheritFrom = GetNonCandidates(r).Distinct().ToList();

                        var newRole = AuthLogic.GetOrCreateTrivialMergeRole(inheritFrom, newRoles);

                        newRoles[newRole.ToString()!] = newRole;

                        Console.WriteLine($"{r} => {newRole}");
                        Administrator.MoveAllForeignKeys(r, newRole, shouldMove: (table, column) =>
                        {
                            if (table is TableMList tm && tm.BackReference.ReferenceTable.Type == typeof(RoleEntity)) //Candidates should be removed in the right order, a non-candidate inheriting from a candidate should produce an exception
                                return false;

                            if (table is Table t && t.Type.IsInstanceOfType(typeof(RuleEntity<>))) //Should have no rules
                                return false;

                            return true;
                        });

                        r.Delete();
                    }


                    if (SafeConsole.Ask("Commit transaction?"))
                        tr.Commit();
                }
            }

        }

        var action = new ConsoleSwitch<string, Action>("What do you want to do with AuthRules?")
        {
            { "i", Import, "Import into database" },
            { "e", Export, "Export to local folder" },
            { "r", SyncRoles, "Sync roles"},
            { "tmr", TrivialMergeRoles, "Refactor to use Trivial Merge Roles"},
        }.Choose();

        action?.Invoke();
    }

    public static Func<Lite<RoleEntity>, bool>? HasRuleOverridesEvent;


    public static bool IsLogged()
    {
        return UserEntity.Current != null && !UserEntity.Current.Is(AnonymousUser);
    }

    public static int Compare(Lite<RoleEntity> role1, Lite<RoleEntity> role2)
    {
        if (rolesGraph.Value.IndirectlyRelatedTo(role1).Contains(role2))
            return 1;

        if (rolesGraph.Value.IndirectlyRelatedTo(role2).Contains(role1))
            return -1;

        return 0;
    }

}




public class InvalidRoleGraphException : Exception
{
    public InvalidRoleGraphException() { }
    public InvalidRoleGraphException(string message) : base(message) { }
    public InvalidRoleGraphException(string message, Exception inner) : base(message, inner) { }
}

