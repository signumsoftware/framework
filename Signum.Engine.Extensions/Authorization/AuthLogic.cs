using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Engine.Operations;
using System.Xml.Linq;
using System.IO;
using Signum.Engine.Scheduler;
using Signum.Engine;
using System.Linq.Expressions;

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        public static event Action<UserEntity>? UserLogingIn;
        public static event Func<string?>? LoginMessage;
        public static ICustomAuthorizer? Authorizer;

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

        static ResetLazy<DirectedGraph<Lite<RoleEntity>>> roles = null!;
        static ResetLazy<DirectedGraph<Lite<RoleEntity>>> rolesInverse = null!;
        static ResetLazy<Dictionary<string, Lite<RoleEntity>>> rolesByName = null!;

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
                    });

                roles = sb.GlobalLazy(CacheRoles, new InvalidateWith(typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);
                rolesInverse = sb.GlobalLazy(()=>roles.Value.Inverse(), new InvalidateWith(typeof(RoleEntity)));
                rolesByName = sb.GlobalLazy(() => roles.Value.ToDictionaryEx(a => a.ToString()!), new InvalidateWith(typeof(RoleEntity)));
                mergeStrategies = sb.GlobalLazy(() =>
                {
                    var strategies = Database.Query<RoleEntity>().Select(r => KeyValuePair.Create(r.ToLite(), r.MergeStrategy)).ToDictionary();

                    var graph = roles.Value;

                    Dictionary<Lite<RoleEntity>, RoleData> result = new Dictionary<Lite<RoleEntity>, RoleData>();
                    foreach (var r in graph.CompilationOrder())
                    {
                        var strat = strategies.GetOrThrow(r);

                        var baseValues = graph.RelatedTo(r).Select(r2=>result[r2].DefaultAllowed);

                        result.Add(r, new RoleData
                        {
                            MergeStrategy = strat,
                            DefaultAllowed = strat == MergeStrategy.Union ? baseValues.Any(a => a) : baseValues.All(a => a)
                        });
                    }

                    return result;
                }, new InvalidateWith(typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);

                sb.Schema.EntityEvents<RoleEntity>().Saving += Schema_Saving;

                QueryLogic.Queries.Register(RoleQuery.RolesReferedBy, () =>
                    from r in Database.Query<RoleEntity>()
                    from rc in r.Roles
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.Name,
                        Refered = rc,
                    });


                UserGraph.Register();
            }
        }

        static void Schema_Saving(RoleEntity role)
        {
            if (!role.IsNew && role.Roles.IsGraphModified)
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    EntityCache.AddFullGraph(role);
                    var allRoles = Database.RetrieveAll<RoleEntity>();

                    var roleGraph = DirectedGraph<RoleEntity>.Generate(allRoles, r => r.Roles.Select(sr => sr.RetrieveAndRemember()));

                    var problems = roleGraph.FeedbackEdgeSet().Edges.ToList();

                    if (problems.Count > 0)
                        throw new ApplicationException(
                            AuthMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                            problems.ToString("\r\n"));
                }
            }
        }

        static DirectedGraph<Lite<RoleEntity>> CacheRoles()
        {
            using (AuthLogic.Disable())
            {
                DirectedGraph<Lite<RoleEntity>> newRoles = new DirectedGraph<Lite<RoleEntity>>();

                using (new EntityCache(EntityCacheType.ForceNewSealed))
                    foreach (var role in Database.RetrieveAll<RoleEntity>())
                    {
                        newRoles.Expand(role.ToLite(), r => r.RetrieveAndRemember().Roles);
                    }

                var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException(
                        AuthMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().FormatWith(problems.Count) +
                        problems.ToString("\r\n"));

                return newRoles;
            }
        }

        public static IDisposable UnsafeUserSession(string username)
        {
            UserEntity? user;
            using (AuthLogic.Disable())
            {
                user = RetrieveUser(username);
                if (user == null)
                    throw new ApplicationException(AuthMessage.Username0IsNotValid.NiceToString().FormatWith(username));
            }

            return UserHolder.UserSession(user);
        }

        public static Func<string, UserEntity?> RetrieveUserByUsername = (username) => Database.Query<UserEntity>().Where(u => u.UserName == username).SingleOrDefaultEx();

        public static UserEntity? RetrieveUser(string username)
        {
            var result = RetrieveUserByUsername(username);

            if (result != null && result.State == UserState.Disabled)
                throw new ApplicationException(AuthMessage.User0IsDisabled.NiceToString().FormatWith(result.UserName));

            return result;
        }

        public static IEnumerable<Lite<RoleEntity>> RolesInOrder()
        {
            return roles.Value.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.ToString()));
        }

        internal static DirectedGraph<Lite<RoleEntity>> RolesGraph()
        {
            return roles.Value;
        }

        public static Lite<RoleEntity> GetRole(string roleName)
        {
            return rolesByName.Value.GetOrThrow(roleName);
        }

        public static IEnumerable<Lite<RoleEntity>> RelatedTo(Lite<RoleEntity> role)
        {
            return roles.Value.RelatedTo(role);
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
                    throw new IncorrectUsernameException(AuthMessage.Username0IsNotValid.NiceToString().FormatWith(username));

                if (!user.PasswordHash.SequenceEqual(passwordHash))
                    throw new IncorrectPasswordException(AuthMessage.IncorrectPassword.NiceToString());

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

                if (!user.PasswordHash.SequenceEqual(passwordHash))
                    return null;

                return user;
            }
        }

        public static void ChangePassword(Lite<UserEntity> user, byte[] passwordHash, byte[] newPasswordHash)
        {
            var userEntity = user.RetrieveAndForget();
            userEntity.PasswordHash = newPasswordHash;
            using (AuthLogic.Disable())
                userEntity.Execute(UserOperation.Save);
        }

        public static void StartAllModules(SchemaBuilder sb)
        {
            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb);
            QueryAuthLogic.Start(sb);
            OperationAuthLogic.Start(sb);
            PermissionAuthLogic.Start(sb);
        }

        public static HashSet<Lite<RoleEntity>> CurrentRoles()
        {
            return roles.Value.IndirectlyRelatedTo(RoleEntity.Current, true);
        }

        public static HashSet<Lite<RoleEntity>> IndirectlyRelated(Lite<RoleEntity> role)
        {
            return roles.Value.IndirectlyRelatedTo(role, true);
        }

        public static HashSet<Lite<RoleEntity>> InverseIndirectlyRelated(Lite<RoleEntity> role)
        {
            return rolesInverse.Value.IndirectlyRelatedTo(role, true);
        }

        internal static int Rank(Lite<RoleEntity> role)
        {
            return roles.Value.IndirectlyRelatedTo(role).Count;
        }

        public static event Func<bool, XElement>? ExportToXml;
        public static event Func<XElement, Dictionary<string, Lite<RoleEntity>>, Replacements, SqlPreCommand?>? ImportFromXml;

        public static XDocument ExportRules(bool exportAll = false)
        {
            SystemEventLogLogic.Log("Export AuthRules");

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Auth",
                    new XElement("Roles",
                        RolesInOrder().Select(r => new XElement("Role",
                            new XAttribute("Name", r.ToString()),
                            GetMergeStrategy(r) == MergeStrategy.Intersection? new XAttribute("MergeStrategy", MergeStrategy.Intersection) : null,
                            new XAttribute("Contains", roles.Value.RelatedTo(r).ToString(","))))),
                     ExportToXml?.GetInvocationListTyped().Select(a => a(exportAll)).NotNull().OrderBy(a => a.Name.ToString())));
        }

        public static SqlPreCommand? ImportRulesScript(XDocument doc, bool interactive)
        {
            Replacements replacements = new Replacements { Interactive = interactive };

            Dictionary<string, Lite<RoleEntity>> rolesDic = roles.Value.ToDictionary(a => a.ToString()!);
            Dictionary<string, XElement> rolesXml = doc.Root.Element("Roles").Elements("Role").ToDictionary(x => x.Attribute("Name").Value);

            replacements.AskForReplacements(rolesXml.Keys.ToHashSet(), rolesDic.Keys.ToHashSet(), "Roles");

            rolesDic = replacements.ApplyReplacementsToNew(rolesDic, "Roles");

            try
            {
                var xmlOnly = rolesXml.Keys.Except(rolesDic.Keys).ToList();
                if (xmlOnly.Any())
                    throw new InvalidOperationException("roles {0} not found on the database".FormatWith(xmlOnly.ToString(", ")));

                foreach (var kvp in rolesXml)
                {
                    var r = rolesDic[kvp.Key];

                    var current = GetMergeStrategy(r);
                    var should = kvp.Value.Attribute("MergeStrategy")?.Let(t => t.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union;

                    if (current != should)
                        throw new InvalidOperationException("Merge strategy of {0} is {1} in the database but is {2} in the file".FormatWith(r, current, should));

                    EnumerableExtensions.JoinStrict(
                        roles.Value.RelatedTo(r),
                        kvp.Value.Attribute("Contains").Value.Split(new []{','},  StringSplitOptions.RemoveEmptyEntries),
                        sr => sr.ToString(),
                        s => rolesDic[s].ToString(),
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
                new SqlPreCommandSimple("use {0}".FormatWith(Connector.Current.DatabaseName())),
                dbOnlyWarnings,
                result,
                new SqlPreCommandSimple("-- END AUTH SYNC SCRIPT"));
        }

        public static void LoadRoles(XDocument doc)
        {
            var roleInfos = doc.Root.Element("Roles").Elements("Role").Select(x => new
            {
                Name = x.Attribute("Name").Value,
                MergeStrategy = x.Attribute("MergeStrategy")?.Let(ms => ms.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union,
                SubRoles = x.Attribute("Contains").Value.SplitNoEmpty(',' )
            }).ToList();

            var roles = roleInfos.ToDictionary(a => a.Name!, a => new RoleEntity { Name = a.Name!, MergeStrategy = a.MergeStrategy }); /*CSBUG*/

            foreach (var ri in roleInfos)
            {
                roles[ri.Name].Roles = ri.SubRoles.Select(r => roles.GetOrThrow(r).ToLiteFat()).ToMList();
            }

            using (OperationLogic.AllowSave<RoleEntity>())
                roles.Values.SaveList();
        }

        public static void SynchronizeRoles(XDocument doc)
        {
            Table table = Schema.Current.Table(typeof(RoleEntity));
            TableMList relationalTable = table.TablesMList().Single();

            Dictionary<string, XElement> rolesXml = doc.Root.Element("Roles").Elements("Role").ToDictionary(x => x.Attribute("Name").Value);

            {
                Dictionary<string, RoleEntity> rolesDic = Database.Query<RoleEntity>().ToDictionary(a => a.ToString());
                Replacements replacements = new Replacements();
                replacements.AskForReplacements(rolesDic.Keys.ToHashSet(), rolesXml.Keys.ToHashSet(), "Roles");
                rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");

                Console.WriteLine("Part 1: Syncronize roles without relationships");

                var roleInsertsDeletes = Synchronizer.SynchronizeScript(Spacing.Double, rolesXml, rolesDic,
                    createNew: (name, xElement) => table.InsertSqlSync(new RoleEntity {
                        Name = name,
                        MergeStrategy = xElement.Attribute("MergeStrategy")?.Let(t => t.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union
                    }, includeCollections: false),

                    removeOld: (name, role) => table.DeleteSqlSync(role, r => r.Name == role.Name),
                    mergeBoth: (name, xElement, role) =>
                    {
                        var oldName = role.Name;
                        role.Name = name;
                        role.MergeStrategy = xElement.Attribute("MergeStrategy")?.Let(t => t.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union;
                        return table.UpdateSqlSync(role, r => r.Name == oldName, includeCollections: false, comment: oldName);
                    });

                if (roleInsertsDeletes != null)
                {
                    SqlPreCommand.Combine(Spacing.Triple,
                       new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                       new SqlPreCommandSimple("use {0}".FormatWith(Connector.Current.DatabaseName())),
                       roleInsertsDeletes,
                       new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT"))!.OpenSqlFileRetry();

                    Console.WriteLine("Press [Enter] when executed...");
                    Console.ReadLine();
                }
                else
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
                }
            }

            {
                Console.WriteLine("Part 2: Syncronize roles relationships");
                Dictionary<string, RoleEntity> rolesDic = Database.Query<RoleEntity>().ToDictionary(a => a.ToString());

                var roleRelationships = Synchronizer.SynchronizeScript(Spacing.Double, rolesXml, rolesDic,
                 createNew: (name, xelement) => { throw new InvalidOperationException("No new roles should be at this stage. Did you execute the script?"); },
                 removeOld: (name, role) => { throw new InvalidOperationException("No old roles should be at this stage. Did you execute the script?"); },
                 mergeBoth: (name, xElement, role) =>
                 {
                     var should = xElement.Attribute("Contains").Value.Split(new []{','},  StringSplitOptions.RemoveEmptyEntries);
                     var current = role.Roles.Select(a => a.ToString()!);

                     if(should.OrderBy().SequenceEqual(current.OrderBy()))
                         return null;

                     role.Roles = should.Select(rs => rolesDic.GetOrThrow(rs).ToLite()).ToMList();

                     return table.UpdateSqlSync(role, r => r.Name == role.Name);
                 });

                if (roleRelationships != null)
                {
                    SqlPreCommand.Combine(Spacing.Triple,
                       new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                       new SqlPreCommandSimple("use {0}".FormatWith(Connector.Current.DatabaseName())),
                       roleRelationships,
                       new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT"))!.OpenSqlFileRetry();

                    Console.WriteLine("Press [Enter] when executed...");
                    Console.ReadLine();
                }
                else
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
                }
            }
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

            }

            void Export()
            {
                var doc = ExportRules();
                doc.Save(fileName);
                Console.WriteLine("Sucesfully exported to {0}".FormatWith(fileName));

                if (SafeConsole.Ask("Publish to Load?"))
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

            var action = new ConsoleSwitch<char, Action>("What do you want to do with AuthRules?")
            {
                { 'i', Import, "Import into database" },
                { 'e', Export, "Export to local folder" },
                { 'r', SyncRoles, "Sync roles"},
            }.Choose();

            action?.Invoke();
        }

        public static string? OnLoginMessage()
        {
            if (AuthLogic.LoginMessage != null)
                return AuthLogic.LoginMessage();

            return null;
        }

        public static bool IsLogged()
        {
            return UserEntity.Current != null && !UserEntity.Current.Is(AnonymousUser);
        }

        public static int Compare(Lite<RoleEntity> role1, Lite<RoleEntity> role2)
        {
            if (roles.Value.IndirectlyRelatedTo(role1).Contains(role2))
                return 1;

            if (roles.Value.IndirectlyRelatedTo(role2).Contains(role1))
                return -1;

            return 0;
        }
    }

    public interface ICustomAuthorizer
    {
        UserEntity Login(string userName, string password, out string authenticationType);
    }

    [Serializable]
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
}
