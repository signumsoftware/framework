using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Xml.Linq;
using System.IO;
using Signum.Engine.Mailing;
using Signum.Engine.Scheduler;
using Signum.Entities.Mailing;
using Signum.Engine.Cache;
using System.Xml;
using DocumentFormat.OpenXml.Presentation;

namespace Signum.Engine.Authorization;

public static class AuthLogic
{
    public static event Action<UserEntity>? UserLogingIn;
    public static ICustomAuthorizer? Authorizer;

    

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

    static ResetLazy<DirectedGraph<Lite<RoleEntity>>> rolesGraph = null!;
    static ResetLazy<DirectedGraph<Lite<RoleEntity>>> rolesInverse = null!;
    static ResetLazy<Dictionary<string, Lite<RoleEntity>>> rolesByName = null!;
    public static ResetLazy<Dictionary<Lite<RoleEntity>, RoleEntity>> RolesByLite = null!;



    class RoleData
    {
        public bool DefaultAllowed;
        public MergeStrategy MergeStrategy;
    }

    static ResetLazy<Dictionary<Lite<RoleEntity>, RoleData>> mergeStrategies = null!;

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => AuthLogic.Start(null!, null, null)));
    }

    public static void Start(SchemaBuilder sb, string? systemUserName, string? anonymousUserName)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            SystemUserName = systemUserName;
            AnonymousUserName = anonymousUserName;

            RoleEntity.RetrieveFromCache = r => RolesByLite.Value.GetOrThrow(r);

            UserWithClaims.FillClaims += (userWithClaims, user)=>
            {
                userWithClaims.Claims["Role"] = ((UserEntity)user).Role;
                userWithClaims.Claims["Culture"] = ((UserEntity)user).CultureInfo?.Name;
            };

            if(MixinDeclarations.IsDeclared(typeof(UserEntity), typeof(UserADMixin)))
            {
                UserWithClaims.FillClaims += (userWithClaims, user) =>
                {
                    var mixin = ((UserEntity)user).Mixin<UserADMixin>();
                    userWithClaims.Claims["OID"] = mixin.OID;
                    userWithClaims.Claims["SID"] = mixin.SID;
                };

                var lambda = As.GetExpression((UserEntity u) => u.ToString());

                if (lambda.Body is MemberExpression me && me.Member is PropertyInfo pi && pi.Name == nameof(UserEntity.UserName))
                {
                    Lite.RegisterLiteModelConstructor((UserEntity u) => new UserLiteModel
                    {
                        UserName = u.UserName,
                        ToStringValue = null,
                        OID = u.Mixin<UserADMixin>().OID,
                        SID = u.Mixin<UserADMixin>().SID,
                    });
                }
                else
                {
                    Lite.RegisterLiteModelConstructor((UserEntity u) => new UserLiteModel
                    {
                        UserName = u.UserName,
                        ToStringValue = u.ToString(),
                        OID = u.Mixin<UserADMixin>().OID,
                        SID = u.Mixin<UserADMixin>().SID,
                    });
                }
             
            }

            CultureInfoLogic.AssertStarted(sb);

            sb.Include<UserEntity>()
              .WithExpressionFrom((RoleEntity r) => r.Users())
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

            sb.Schema.Table<RoleEntity>().PreDeleteSqlSync += Role_PreDeleteSqlSync;
  


            RolesByLite = sb.GlobalLazy(() => Database.Query<RoleEntity>().ToDictionaryEx(a => a.ToLite()), new InvalidateWith(typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);
            rolesByName = sb.GlobalLazy(() => RolesByLite.Value.Keys.ToDictionaryEx(a => a.ToString()!), new InvalidateWith(typeof(RoleEntity)));
            rolesGraph = sb.GlobalLazy(()=> CacheRoles(RolesByLite.Value), new InvalidateWith(typeof(RoleEntity)));
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

                return result;
            }, new InvalidateWith(typeof(RoleEntity)));

            sb.Schema.EntityEvents<RoleEntity>().Saving += Schema_Saving;

            UserGraph.Register();

            EmailModelLogic.RegisterEmailModel<UserLockedMail>(() => new EmailTemplateEntity
            {
                Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
                {
                    Text =
                        "<p>{0}</p>".FormatWith(AuthEmailMessage.YourAccountHasBeenLockedDueToSeveralFailedLogins.NiceToString()) +
                        "<p>{0}</p>".FormatWith(AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow.NiceToString()) +
                        "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                    Subject = AuthEmailMessage.YourAccountHasBeenLocked.NiceToString()
                }).ToMList()
            });
        }
    }

    static SqlPreCommand? Role_PreDeleteSqlSync(Entity entity)
    {
        return Administrator.UnsafeDeletePreCommandMList((RoleEntity rt) => rt.InheritsFrom, Database.MListQuery((RoleEntity rt) => rt.InheritsFrom).Where(mle => mle.Element.Is((RoleEntity)entity)));
    }

    public static Lite<RoleEntity> GetOrCreateTrivialMergeRole(List<Lite<RoleEntity>> roles, Dictionary<string, Lite<RoleEntity>>? newRoles = null)
    {
        roles = roles.Distinct().ToList();

        if (roles.Count == 1)
            return roles.SingleEx();

        var flatRoles = roles
            .Select(a => RolesByLite.Value.GetOrThrow(a))
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
                            problems.ToString("\r\n"));
                }

                var dic = allRoles.ToDictionary(a => a.ToLite());

                var problems2 = allRoles.SelectMany(r => r.InheritsFrom.Where(inh => RolesByLite.Value.GetOrThrow(inh).IsTrivialMerge).Select(inh => new { r, inh })).ToList();
                if (problems2.Any())
                    throw new ApplicationException(
                        problems2.GroupBy(a => a.r, a => a.inh)
                        .Select(gr => AuthAdminMessage.Role0InheritsFromTrivialMergeRole1.NiceToString(gr.Key, gr.CommaAnd()))
                        .ToString("\r\n"));
            }

            if (!role.IsTrivialMerge)
            {
                var trivialDependant = rolesInverse.Value.IndirectlyRelatedTo(role.ToLite())
                    .Select(r => RolesByLite.Value.GetOrThrow(r))
                    .Where(a => a.IsTrivialMerge);

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

    static DirectedGraph<Lite<RoleEntity>> CacheRoles(Dictionary<Lite<RoleEntity>, RoleEntity> rolesLite)
    {
        var graph = DirectedGraph<Lite<RoleEntity>>.Generate(rolesLite.Keys, r => rolesLite.GetOrThrow(r).InheritsFrom);

        var problems = graph.FeedbackEdgeSet().Edges.ToList();

        if (problems.Count > 0)
            throw new ApplicationException(
                AuthAdminMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                problems.ToString("\r\n"));

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

    public static Func<string, UserEntity?> RetrieveUserByUsername = (username) => Database.Query<UserEntity>().Where(u => u.UserName == username).SingleOrDefaultEx();

    public static UserEntity? RetrieveUser(string username)
    {
        var result = RetrieveUserByUsername(username);

        if (result != null && result.State == UserState.Deactivated)
            throw new ApplicationException(LoginAuthMessage.User0IsDisabled.NiceToString().FormatWith(result.UserName));

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

    static bool gloaballyEnabled = true;
    public static bool GloballyEnabled
    {
        get { return gloaballyEnabled; }
        set { gloaballyEnabled = value; }
    }

    static readonly Variable<bool> tempDisabled = Statics.ThreadVariable<bool>("authTempDisabled");

    public static IDisposable? Disable()
    {
        if (tempDisabled.Value) return null;
        tempDisabled.Value = true;
        return new Disposable(() => tempDisabled.Value = false);
    }

    public static IDisposable? Enable()
    {
        if (!tempDisabled.Value) return null;
        tempDisabled.Value = false;
        return new Disposable(() => tempDisabled.Value = true);
    }

    public static bool IsEnabled
    {
        get { return !tempDisabled.Value && gloaballyEnabled; }
    }

    public static event Action? OnRulesChanged;

    public static void NotifyRulesChanged()
    {
        OnRulesChanged?.Invoke();
    }

    public static UserEntity Login(string username, byte[] passwordHash, out string authenticationType)
    {
        using (AuthLogic.Disable())
        {
            UserEntity user = RetrieveUser(username, passwordHash);

            OnUserLogingIn(user);

            authenticationType = "database";

            return user;
        }
    }

    public static void OnUserLogingIn(UserEntity user)
    {
        UserLogingIn?.Invoke(user);
    }

    public static UserEntity RetrieveUser(string username, byte[] passwordHash)
    {
        using (AuthLogic.Disable())
        {
            UserEntity? user = RetrieveUser(username);

            if (user == null)
                throw new IncorrectUsernameException(LoginAuthMessage.Username0IsNotValid.NiceToString().FormatWith(username));


            if (user.PasswordHash == null || !user.PasswordHash.SequenceEqual(passwordHash))
            {
                using (UserHolder.UserSession(SystemUser!))
                {
                    user.LoginFailedCounter++;
                    user.Execute(UserOperation.Save);

                    if (MaxFailedLoginAttempts.HasValue &&
                        user.LoginFailedCounter == MaxFailedLoginAttempts &&
                        user.State == UserState.Active)
                    {
                        var config = EmailLogic.Configuration;
                        var request = ResetPasswordRequestLogic.ResetPasswordRequest(user);
                        var url = $"{config.UrlLeft}/auth/resetPassword?code={request.Code}";

                        var mail = new UserLockedMail(user, url);
                        mail.SendMailAsync();

                        user.Execute(UserOperation.Deactivate);

                        throw new UserLockedException(LoginAuthMessage.User0IsDisabled.NiceToString()
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

            return user;
        }
    }

    public static UserEntity? TryRetrieveUser(string username, byte[] passwordHash)
    {
        using (AuthLogic.Disable())
        {
            UserEntity? user = RetrieveUser(username);
            if (user == null)
                return null;

            if (user.PasswordHash == null || !user.PasswordHash.SequenceEqual(passwordHash))
                return null;

            return user;
        }
    }

    public static void StartAllModules(SchemaBuilder sb, bool activeDirectoryIntegration = false)
    {
        TypeAuthLogic.Start(sb);
        PropertyAuthLogic.Start(sb);
        QueryAuthLogic.Start(sb);
        OperationAuthLogic.Start(sb);
        PermissionAuthLogic.Start(sb);

        if (activeDirectoryIntegration)
        {
            PermissionAuthLogic.RegisterTypes(typeof(ActiveDirectoryPermission));
        }
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
                    RolesInOrder(includeTrivialMerge: true).Select(r => new XElement("Role",
                        new XAttribute("Name", r.ToString()!),
                        GetMergeStrategy(r) == MergeStrategy.Intersection ? new XAttribute("MergeStrategy", MergeStrategy.Intersection) : null!,
                        RolesByLite.Value.GetOrCreate(r).IsTrivialMerge ? new XAttribute("IsTrivialMerge", true) : null!,
                        new XAttribute("Contains", rolesGraph.Value.RelatedTo(r).ToString(",")),
                        rolesDic.TryGetC(r)?.Description?.Let(d => new XAttribute("Description", d))
                        ))),
                 ExportToXml?.GetInvocationListTyped().Select(a => a(exportAll)).NotNull().OrderBy(a => a.Name.ToString())!));
    }

    public static SqlPreCommand? ImportRulesScript(XDocument doc, bool interactive)
    {
        Replacements replacements = new Replacements { Interactive = interactive };

        Dictionary<string, Lite<RoleEntity>> rolesDic = rolesGraph.Value.ToDictionary(a => a.ToString()!);
        Dictionary<string, XElement> rolesXml = doc.Root!.Element("Roles")!.Elements("Role").ToDictionary(x => x.Attribute("Name")!.Value);

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
            throw new InvalidRoleGraphException("The role graph does not match:\r\n" + ex.Message);
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

    public static void SynchronizeRoles(XDocument doc)
    {
        Table table = Schema.Current.Table(typeof(RoleEntity));
        TableMList relationalTable = table.TablesMList().Single();

        Dictionary<string, XElement> rolesXml = doc.Root!.Element("Roles")!.Elements("Role").ToDictionary(x => x.Attribute("Name")!.Value);

        Dictionary<string, RoleEntity> rolesDic = Database.Query<RoleEntity>().ToDictionary(a => a.ToString());
        Replacements replacements = new Replacements();
        replacements.AskForReplacements(rolesDic.Keys.ToHashSet(), rolesXml.Keys.ToHashSet(), "Roles");
        rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");

        Dictionary<string, XElement> trivialXmls = rolesXml.Extract((k, xml) => xml.Attribute("IsTrivialMerge")?.Value.ToBool() == true);
        Dictionary<string, RoleEntity> trivialRoles = rolesDic.Extract(k => trivialXmls.ContainsKey(k));


        {
            Console.WriteLine("Part 1: Syncronize roles without relationships");

            var roleInsertsDeletes = Synchronizer.SynchronizeScript(Spacing.Double,
                rolesXml,
                rolesDic,
                createNew: (name, xElement) => table.InsertSqlSync(new RoleEntity
                {
                    Name = name,
                    MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union,
                    Description = xElement.Attribute("Description")?.Value,
                    IsTrivialMerge = false,
                }, includeCollections: false),

                removeOld: (name, role) => table.DeleteSqlSync(role, r => r.Name == role.Name),
                mergeBoth: (name, xElement, role) =>
                {
                    var oldName = role.Name;
                    role.Name = name;
                    role.MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union;
                    role.Description = xElement.Attribute("Description")?.Value;
                    role.IsTrivialMerge = false;
                    return table.UpdateSqlSync(role, r => r.Name == oldName, includeCollections: false, comment: oldName);
                });

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
                SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
            }
        }

        CacheLogic.ForceReset();
        GlobalLazy.ResetAll();

        {
            Console.WriteLine("Part 2: Syncronize roles relationships and trivial merges");
            rolesDic = Database.Query<RoleEntity>().ToDictionary(a => a.ToString());
            rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");
            trivialRoles = rolesDic.Extract(k => trivialXmls.ContainsKey(k));

            MList<Lite<RoleEntity>> ParseInheritedFrom(string contains)
            {
                return contains.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(rs => rolesDic.GetOrThrow(rs).ToLite()).ToMList();
            }


            var roleRelationships = Synchronizer.SynchronizeScript(Spacing.Double,
               rolesXml,
               rolesDic,
                createNew: (name, xelement) => { throw new InvalidOperationException("No new roles should be at this stage. Did you execute the script?"); },
                removeOld: (name, role) => { throw new InvalidOperationException("No old roles should be at this stage. Did you execute the script?"); },
                mergeBoth: (name, xElement, role) =>
                {
                    var should = ParseInheritedFrom(xElement.Attribute("Contains")!.Value);

                    if (!role.InheritsFrom.ToHashSet().SetEquals(should))
                        role.InheritsFrom = should;

                    return table.UpdateSqlSync(role, r => r.Name == role.Name);
                });


            var trivialMerges = Synchronizer.SynchronizeScript(Spacing.Double,
                trivialXmls,
                trivialRoles,
                createNew: (name, xElement) => table.InsertSqlSync(new RoleEntity
                {
                    Name = name,
                    MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union,
                    Description = xElement.Attribute("Description")?.Value,
                    IsTrivialMerge = true,
                    InheritsFrom = ParseInheritedFrom(xElement.Attribute("Contains")!.Value)
                }),

                removeOld: (name, role) => table.DeleteSqlSync(role, r => r.Name == role.Name),
                mergeBoth: (name, xElement, role) =>
                {
                    var oldName = role.Name;
                    role.Name = name;
                    role.MergeStrategy = xElement.Attribute("MergeStrategy")?.Value.ToEnum<MergeStrategy>() ?? MergeStrategy.Union;
                    role.Description = xElement.Attribute("Description")?.Value;
                    role.IsTrivialMerge = true;

                    var should = ParseInheritedFrom(xElement.Attribute("Contains")!.Value);

                    if (!role.InheritsFrom.ToHashSet().SetEquals(should))
                        role.InheritsFrom = should.ToMList();

                    return table.UpdateSqlSync(role, r => r.Name == oldName, comment: oldName);
                });

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

        CacheLogic.ForceReset();
        GlobalLazy.ResetAll();
    }

   
    public static void AutomaticImportAuthRules()
    {
        AutomaticImportAuthRules("AuthRules.xml");
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

            CacheLogic.ForceReset();
            GlobalLazy.ResetAll();
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

            SynchronizeRoles(doc);
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

                            if (table is Table t && t.Type.IsInstanceOfType(typeof(RuleEntity<,>))) //Should have no rules
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

public interface ICustomAuthorizer
{
    UserEntity Login(string userName, string password, out string authenticationType);
}

public class InvalidRoleGraphException : Exception
{
    public InvalidRoleGraphException() { }
    public InvalidRoleGraphException(string message) : base(message) { }
    public InvalidRoleGraphException(string message, Exception inner) : base(message, inner) { }
    protected InvalidRoleGraphException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context)
        : base(info, context) { }
}

public class UserLockedMail : EmailModel<UserEntity>
{
    public string Url;

    public UserLockedMail(UserEntity entity) : this(entity, "http://testurl.com") { }

    public UserLockedMail(UserEntity entity, string url) : base(entity)
    {
        this.Url = url;
    }

    public override List<EmailOwnerRecipientData> GetRecipients()
    {
        return SendTo(Entity.EmailOwnerData);
    }
}
