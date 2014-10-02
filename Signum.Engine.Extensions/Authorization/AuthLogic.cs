using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System.Security.Principal;
using System.Threading;
using Signum.Services;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Cache;
using System.IO;
using Signum.Entities.Mailing;
using Signum.Engine.Translation;

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        public static event Action<UserDN> UserLogingIn;
        public static event Func<string> LoginMessage;

        public static string SystemUserName { get; private set; }
        static ResetLazy<UserDN> systemUserLazy = GlobalLazy.WithoutInvalidations(() => SystemUserName == null ? null :
            Database.Query<UserDN>().Where(u => u.UserName == SystemUserName)
            .SingleEx(() => "SystemUser with name '{0}'".Formato(SystemUserName)));
        public static UserDN SystemUser
        {
            get { return systemUserLazy.Value; }
        }

        public static string AnonymousUserName { get; private set; }
        static ResetLazy<UserDN> anonymousUserLazy = GlobalLazy.WithoutInvalidations(() => AnonymousUserName == null ? null :
            Database.Query<UserDN>().Where(u => u.UserName == AnonymousUserName)
            .SingleEx(() => "AnonymousUser with name '{0}'".Formato(AnonymousUserName)));
        public static UserDN AnonymousUser
        {
            get { return anonymousUserLazy.Value; }
        }


        

        static ResetLazy<DirectedGraph<Lite<RoleDN>>> roles;

        class RoleData
        {
            public bool DefaultAllowed;
            public MergeStrategy MergeStrategy;
        }

        static ResetLazy<Dictionary<Lite<RoleDN>, RoleData>> mergeStrategies;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => AuthLogic.Start(null, null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, string systemUserName, string anonymousUserName)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SystemUserName = systemUserName;
                AnonymousUserName = anonymousUserName;

                CultureInfoLogic.AssertStarted(sb); 

                sb.Include<UserDN>();
                
                sb.Include<RoleDN>();
                sb.Include<LastAuthRulesImportDN>(); 

                roles = sb.GlobalLazy(CacheRoles, new InvalidateWith(typeof(RoleDN)));
                mergeStrategies = sb.GlobalLazy(() =>
                {
                    var strategies = Database.Query<RoleDN>().Select(r => KVP.Create(r.ToLite(), r.MergeStrategy)).ToDictionary();

                    var graph = roles.Value;

                    Dictionary<Lite<RoleDN>, RoleData> result = new Dictionary<Lite<RoleDN>, RoleData>();
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
                },new InvalidateWith(typeof(RoleDN)));

                sb.Schema.EntityEvents<RoleDN>().Saving += Schema_Saving;

                dqm.RegisterQuery(typeof(RoleDN), () =>
                    from r in Database.Query<RoleDN>()
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.Name,
                    });

                dqm.RegisterQuery(RoleQuery.RolesReferedBy, () =>
                    from r in Database.Query<RoleDN>()
                    from rc in r.Roles
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.Name,
                        Refered = rc,
                    });

                dqm.RegisterQuery(typeof(UserDN), () =>
                    from e in Database.Query<UserDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.UserName,
                        e.Email,
                        e.Role,
                        e.State,
                    });

                UserGraph.Register();

                new Graph<RoleDN>.Execute(RoleOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (r, args) => { }
                }.Register();

                new Graph<RoleDN>.Delete(RoleOperation.Delete)
                {
                    Delete = (r, args) =>
                    {
                        r.Delete();
                    }
                }.Register();
            }
        }

        static void Schema_Saving(RoleDN role)
        {
            if (!role.IsNew && role.Roles != null && role.Roles.IsGraphModified)
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    EntityCache.AddFullGraph(role);

                    DirectedGraph<RoleDN> newRoles = new DirectedGraph<RoleDN>();

                    newRoles.Expand(role, r1 => r1.Roles.Select(a => a.Retrieve()));
                    foreach (var r in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(r, r1 => r1.Roles.Select(a => a.Retrieve()));
                    }

                    var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                    if (problems.Count > 0)
                        throw new ApplicationException(
                            AuthMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().Formato(problems.Count) +
                            problems.ToString("\r\n"));
                }
            }
        }

        static DirectedGraph<Lite<RoleDN>> CacheRoles()
        {
            using (AuthLogic.Disable())
            {
                DirectedGraph<Lite<RoleDN>> newRoles = new DirectedGraph<Lite<RoleDN>>();

                using (new EntityCache(EntityCacheType.ForceNewSealed))
                    foreach (var role in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(role.ToLite(), r => r.Retrieve().Roles);
                    }

                var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException(
                        AuthMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.NiceToString().Formato(problems.Count) +
                        problems.ToString("\r\n"));

                return newRoles;
            }
        }

        public static IDisposable UnsafeUserSession(string username)
        {
            UserDN user;
            using (AuthLogic.Disable())
            {
                user = RetrieveUser(username);
                if (user == null)
                    throw new ApplicationException(AuthMessage.Username0IsNotValid.NiceToString().Formato(username));
            }

            return UserSession(user);
        }

        public static IDisposable UserSession(UserDN user)
        {
            var result = ScopeSessionFactory.OverrideSession();
            UserDN.Current = user;
            return result;
        }

        public static UserDN RetrieveUser(string username)
        {
            var result = Database.Query<UserDN>().SingleOrDefaultEx(u => u.UserName == username);

            if (result != null && result.State == UserState.Disabled)
                throw new ApplicationException(AuthMessage.User0IsDisabled.NiceToString().Formato(result.UserName));

            return result; 
        }

        public static IEnumerable<Lite<RoleDN>> RolesInOrder()
        {
            return roles.Value.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.ToString()));
        }

        internal static DirectedGraph<Lite<RoleDN>> RolesGraph()
        {
            return roles.Value;
        }

        public static IEnumerable<Lite<RoleDN>> RelatedTo(Lite<RoleDN> role)
        {
            return roles.Value.RelatedTo(role);
        }

        public static MergeStrategy GetMergeStrategy(Lite<RoleDN> role)
        {
            return mergeStrategies.Value.GetOrThrow(role).MergeStrategy;
        }

        public static bool GetDefaultAllowed(Lite<RoleDN> role)
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

        public static IDisposable Disable()
        {
            if (tempDisabled.Value) return null;
            tempDisabled.Value = true;
            return new Disposable(() => tempDisabled.Value = false);
        }

        public static IDisposable Enable()
        {
            if (!tempDisabled.Value) return null;
            tempDisabled.Value = false;
            return new Disposable(() => tempDisabled.Value = true);
        }

        public static bool IsEnabled
        {
            get { return !tempDisabled.Value && gloaballyEnabled; }
        }

        public static UserDN Login(string username, string passwordHash)
        {
            using (AuthLogic.Disable())
            {
                UserDN user = RetrieveUser(username, passwordHash);

                if (UserLogingIn != null)
                    UserLogingIn(user);

                return user;
            }
        }

        public static UserDN RetrieveUser(string username, string passwordHash)
        {
            using (AuthLogic.Disable())
            {
                UserDN user = RetrieveUser(username);
                if (user == null)
                    throw new IncorrectUsernameException(AuthMessage.Username0IsNotValid.NiceToString().Formato(username));

                if (user.PasswordHash != passwordHash)
                    throw new IncorrectPasswordException(AuthMessage.IncorrectPassword.NiceToString());

                return user;
            }
        }

        public static UserDN ChangePasswordLogin(string username, string passwordHash, string newPasswordHash)
        {
            var userEntity = RetrieveUser(username, passwordHash);
            userEntity.PasswordHash = newPasswordHash;
            using (AuthLogic.Disable())
                userEntity.Execute(UserOperation.Save);

            return Login(username, newPasswordHash);
        }

        public static void ChangePassword(Lite<UserDN> user, string passwordHash, string newPasswordHash)
        {
            var userEntity = user.RetrieveAndForget();
            userEntity.PasswordHash = newPasswordHash;
            using (AuthLogic.Disable())
                userEntity.Execute(UserOperation.Save);
        }

        public static void StartAllModules(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb, true);
            QueryAuthLogic.Start(sb, dqm);
            OperationAuthLogic.Start(sb);
            PermissionAuthLogic.Start(sb);
        }

        public static HashSet<Lite<RoleDN>> CurrentRoles()
        {
            return roles.Value.IndirectlyRelatedTo(RoleDN.Current.ToLite(), true);
        }

        internal static int Rank(Lite<RoleDN> role)
        {
            return roles.Value.IndirectlyRelatedTo(role).Count;
        }

        public static event Func<bool, XElement> ExportToXml;
        public static event Func<XElement, Dictionary<string, Lite<RoleDN>>, Replacements, SqlPreCommand> ImportFromXml;
        public static event Func<Action<Lite<RoleDN>>> SuggestRuleChanges;

        public static void SuggestChanges()
        {
            foreach (var item in SuggestRuleChanges.GetInvocationListTyped())
            {
                if (SafeConsole.Ask("{0}?".Formato(item.Method.Name.NiceName())))
                {
                    var action = item();

                    foreach (var role in RolesInOrder())
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.DarkYellow, "Suggestions for {0}", role);

                        action(role);
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        public static XDocument ExportRules(bool exportAll = false)
        {
            var imported = Database.Query<LastAuthRulesImportDN>().SingleOrDefault();

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Auth",
                    imported == null ? null : new XElement("Imported", new XAttribute("On", imported.Date.ToString("s"))),
                    new XElement("Exported", new XAttribute("On", TimeZoneManager.Now.ToString("s"))),
                    new XElement("Roles",
                        RolesInOrder().Select(r => new XElement("Role",
                            new XAttribute("Name", r.ToString()),
                            GetMergeStrategy(r) == MergeStrategy.Intersection? new XAttribute("MergeStrategy", MergeStrategy.Intersection) : null,
                            new XAttribute("Contains", roles.Value.RelatedTo(r).ToString(","))))),
                     ExportToXml.GetInvocationListTyped().Select(a => a(exportAll)).NotNull().OrderBy(a => a.Name.ToString())));
        }

        public static SqlPreCommand ImportRulesScript(XDocument doc)
        {
           Replacements replacements = new Replacements();

            Dictionary<string, Lite<RoleDN>> rolesDic = roles.Value.ToDictionary(a => a.ToString());
            Dictionary<string, XElement> rolesXml = doc.Root.Element("Roles").Elements("Role").ToDictionary(x => x.Attribute("Name").Value);

            replacements.AskForReplacements(rolesXml.Keys.ToHashSet(), rolesDic.Keys.ToHashSet(), "Roles");

            rolesDic = replacements.ApplyReplacementsToNew(rolesDic, "Roles");

            try
            {
                var xmlOnly = rolesXml.Keys.Except(rolesDic.Keys).ToList();
                if (xmlOnly.Any())
                    throw new InvalidOperationException("roles {0} not found on the database".Formato(xmlOnly.ToString(", ")));

                foreach (var kvp in rolesXml)
                {
                    var r = rolesDic[kvp.Key];

                    var current = GetMergeStrategy(r);
                    var should = kvp.Value.Attribute("MergeStrategy").Try(t => t.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union;

                    if (current != should)
                        throw new InvalidOperationException("Merge strategy of {0} is {1} in the database but is {2} in the file".Formato(r, current, should));

                    EnumerableExtensions.JoinStrict(
                        roles.Value.RelatedTo(r),
                        kvp.Value.Attribute("Contains").Value.Split(new []{','},  StringSplitOptions.RemoveEmptyEntries),
                        sr => sr.ToString(),
                        s => rolesDic[s].ToString(),
                        (sr, s) => 0,
                        "subRoles of {0}".Formato(r));
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidRoleGraphException("The role graph does not match:\r\n" + ex.Message); 
            }

            var dbOnlyWarnings = rolesDic.Keys.Except(rolesXml.Keys).Select(n =>
                    new SqlPreCommandSimple("-- Alien role {0} not configured!!".Formato(n))
                ).Combine(Spacing.Simple);

            SqlPreCommand result = ImportFromXml.GetInvocationListTyped()
                .Select(inv => inv(doc.Root, rolesDic, replacements)).Combine(Spacing.Triple);

            result = SqlPreCommand.Combine(Spacing.Triple, result, UpdateLastAuthRules(doc.Root.Element("Exported")));
            

            if (replacements.Values.Any(a => a.Any()))
                SafeConsole.WriteLineColor(ConsoleColor.Red, "There are renames! Remember to export after executing the script");

            if (result == null && dbOnlyWarnings == null)
                return null;

            var declareParent = result.Leaves().Any(l => l.Sql.StartsWith("SET @idParent")) ? new SqlPreCommandSimple("DECLARE @idParent INT") : null;

            return SqlPreCommand.Combine(Spacing.Triple,
                new SqlPreCommandSimple("-- BEGIN AUTH SYNC SCRIPT"),
                new SqlPreCommandSimple("use {0}".Formato(Connector.Current.DatabaseName())),
                dbOnlyWarnings,
                declareParent,
                result,
                new SqlPreCommandSimple("-- END AUTH SYNC SCRIPT"));
        }

        private static SqlPreCommand UpdateLastAuthRules(XElement exported)
        {
            var table = Schema.Current.Table(typeof(LastAuthRulesImportDN)); 

            LastAuthRulesImportDN last = Database.Query<LastAuthRulesImportDN>().SingleOrDefaultEx();

            if (exported == null)
            {
                if (last == null)
                    return null;

                return table.DeleteSqlSync(last);
            }

            DateTime dt =  DateTime.ParseExact(exported.Attribute("On").Value, "s", null).FromUserInterface();

            if (last == null)
                return table.InsertSqlSync(new LastAuthRulesImportDN { Date = dt });

            last.Date = dt;

            return table.UpdateSqlSync(last); 
        }

        public static void LoadRoles(XDocument doc)
        {
            var roleInfos = doc.Root.Element("Roles").Elements("Role").Select(x => new
            {
                Name = x.Attribute("Name").Value,
                MergeStrategy = x.Attribute("MergeStrategy").Try(ms => ms.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union,
                SubRoles = x.Attribute("Contains").Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            }).ToList();

            var roles = roleInfos.ToDictionary(a => a.Name, a => new RoleDN { Name = a.Name, MergeStrategy = a.MergeStrategy });

            foreach (var ri in roleInfos)
            {
                roles[ri.Name].Roles = ri.SubRoles.Select(r => roles.GetOrThrow(r).ToLiteFat()).ToMList();
            }

            using (OperationLogic.AllowSave<RoleDN>())
                roles.Values.SaveList();
        }

        public static void SynchronizeRoles(XDocument doc)
        {
            Table table = Schema.Current.Table(typeof(RoleDN));
            TableMList relationalTable = table.TablesMList().Single();

            Dictionary<string, XElement> rolesXml = doc.Root.Element("Roles").Elements("Role").ToDictionary(x => x.Attribute("Name").Value);

            {
                Dictionary<string, RoleDN> rolesDic = Database.Query<RoleDN>().ToDictionary(a => a.ToString());
                Replacements replacements = new Replacements();
                replacements.AskForReplacements(rolesDic.Keys.ToHashSet(), rolesXml.Keys.ToHashSet(), "Roles");
                rolesDic = replacements.ApplyReplacementsToOld(rolesDic, "Roles");

                Console.WriteLine("Part 1: Syncronize roles without relationships");

                var roleInsertsDeletes = Synchronizer.SynchronizeScript(rolesXml, rolesDic,
                    (name, xelement) => table.InsertSqlSync(new RoleDN { Name = name }, includeCollections: false),
                    (name, role) => SqlPreCommand.Combine(Spacing.Simple,
                            new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                                .Formato(relationalTable.Name, ((IColumn)relationalTable.Field).Name.SqlEscape(), role.Id, role.Name)),
                            table.DeleteSqlSync(role)),
                    (name, xElement, role) =>
                    {
                        var oldName = role.Name;
                        role.Name = name;
                        role.MergeStrategy = xElement.Attribute("MergeStrategy").Try(t => t.Value.ToEnum<MergeStrategy>()) ?? MergeStrategy.Union;
                        return table.UpdateSqlSync(role, includeCollections: false, comment: oldName);
                    }, Spacing.Double);

                if (roleInsertsDeletes != null)
                {
                    SqlPreCommand.Combine(Spacing.Triple,
                       new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                       new SqlPreCommandSimple("use {0}".Formato(Connector.Current.DatabaseName())),
                       roleInsertsDeletes,
                       new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT")).OpenSqlFileRetry();

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
                Dictionary<string, RoleDN> rolesDic = Database.Query<RoleDN>().ToDictionary(a => a.ToString());

                var roleRelationships = Synchronizer.SynchronizeScript(rolesXml, rolesDic,
                 (name, xelement) => { throw new InvalidOperationException("No new roles should be at this stage. Did you execute the script?"); },
                 (name, role) => { throw new InvalidOperationException("No old roles should be at this stage. Did you execute the script?"); },
                 (name, xElement, role) =>
                 {
                     var should = xElement.Attribute("Contains").Value.Split(new []{','},  StringSplitOptions.RemoveEmptyEntries);
                     var current = role.Roles.Select(a=>a.ToString());

                     if(should.OrderBy().SequenceEqual(current.OrderBy()))
                         return null;

                     role.Roles = should.Select(rs => rolesDic.GetOrThrow(rs).ToLite()).ToMList();

                     return table.UpdateSqlSync(role);
                 }, Spacing.Double);

                if (roleRelationships != null)
                {
                    SqlPreCommand.Combine(Spacing.Triple,
                       new SqlPreCommandSimple("-- BEGIN ROLE SYNC SCRIPT"),
                       new SqlPreCommandSimple("use {0}".Formato(Connector.Current.DatabaseName())),
                       roleRelationships,
                       new SqlPreCommandSimple("-- END ROLE  SYNC SCRIPT")).OpenSqlFileRetry();

                    Console.WriteLine("Press [Enter] when executed...");
                    Console.ReadLine();
                }
                else
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
                }
            }
        }


        public static void ImportExportAuthRules()
        {
            ImportExportAuthRules("AuthRules.xml");
        }

        public static void ImportExportAuthRules(string fileName)
        {
            Console.WriteLine("You want to export (e), import (i), sync roles (r) or suggest (s) AuthRules? (nothing to exit)".Formato(fileName));

            string answer = Console.ReadLine();

            switch (answer.ToLower())
            {
                case "e":
                    {
                        var doc = ExportRules();
                        doc.Save(fileName);
                        Console.WriteLine("Sucesfully exported to {0}".Formato(fileName));

                        if (SafeConsole.Ask("Publish to Load?"))
                            File.Copy(fileName, "../../" + Path.GetFileName(fileName), overwrite: true);

                        break;
                    }
                case "i":
                    {
                        Console.Write("Reading {0}...".Formato(fileName));
                        var doc = XDocument.Load(fileName);
                        Console.WriteLine("Ok");

                        Console.WriteLine("Generating SQL script to import auth rules (without modifying the role graph or entities):");
                        SqlPreCommand command;
                        try
                        {
                             command = ImportRulesScript(doc);
                        }
                        catch (InvalidRoleGraphException ex)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Red, ex.Message);

                            if(SafeConsole.Ask("Import roles first?"))
                                goto case "r";

                            return;
                        }

                        if (command == null)
                            SafeConsole.WriteLineColor(ConsoleColor.Green, "Already syncronized");
                        else
                            command.OpenSqlFileRetry();

                        break;
                    }
                case "r":
                    {
                        Console.Write("Reading {0}...".Formato(fileName));
                        var doc = XDocument.Load(fileName);
                        Console.WriteLine("Ok");


                        Console.WriteLine("Generating script to synchronize roles...");

                        SynchronizeRoles(doc);
                        if (SafeConsole.Ask("Import rules now?"))
                            goto case "i";

                        break;
                    }
                case "s":
                    {
                        SuggestChanges();
                        if (SafeConsole.Ask("Export now?"))
                            goto case "e";

                        break;
                    }
                default:
                    break;
            }
        }

        public static string OnLoginMessage()
        {
            if (AuthLogic.LoginMessage != null)
                return AuthLogic.LoginMessage();

            return null;
        }

        public static bool IsLogged()
        {
            return UserDN.Current != null && !UserDN.Current.Is(AnonymousUser);
        }

        public static int Compare(Lite<RoleDN> role1, Lite<RoleDN> role2)
        {
            if (roles.Value.IndirectlyRelatedTo(role1).Contains(role2))
                return 1;

            if (roles.Value.IndirectlyRelatedTo(role2).Contains(role1))
                return -1;

            return 0;
        }
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
