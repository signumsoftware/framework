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
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        public static event Action<UserDN> UserLogingIn;
        public static event Func<string> LoginMessage;


        public static string SystemUserName { get; private set; }
        static ResetLazy<UserDN> systemUserLazy = GlobalLazy.Create(() => SystemUserName == null ? null :
            Database.Query<UserDN>().Where(u => u.UserName == SystemUserName)
            .SingleEx(() => "SystemUser with name '{0}' not found".Formato(SystemUserName)));
        public static UserDN SystemUser
        {
            get { return systemUserLazy.Value; }
        }

        public static string AnonymousUserName { get; private set; }
        static ResetLazy<UserDN> anonymousUserLazy = GlobalLazy.Create(() => AnonymousUserName == null ? null :
            Database.Query<UserDN>().Where(u => u.UserName == AnonymousUserName)
            .SingleEx(() => "AnonymousUser with name '{0}' not found".Formato(AnonymousUserName)));
        public static UserDN AnonymousUser
        {
            get { return anonymousUserLazy.Value; }
        }

        public static readonly ResetLazy<DirectedGraph<Lite<RoleDN>>> roles = GlobalLazy.Create(Cache).InvalidateWith(typeof(RoleDN));

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

                sb.Include<UserDN>();
                sb.Include<RoleDN>();

                sb.Schema.EntityEvents<RoleDN>().Saving += Schema_Saving;

                dqm[typeof(RoleDN)] = (from r in Database.Query<RoleDN>()
                                       select new
                                       {
                                           Entity = r,
                                           r.Id,
                                           r.Name,
                                       }).ToDynamic();

                dqm[RoleQueries.ReferedBy] = (from r in Database.Query<RoleDN>()
                                              from rc in r.Roles
                                              select new
                                              {
                                                  Entity = r,
                                                  r.Id,
                                                  r.Name,
                                                  Refered = rc,
                                              }).ToDynamic();

                dqm[typeof(UserDN)] = (from e in Database.Query<UserDN>()
                                       select new
                                       {
                                           Entity = e,
                                           e.Id,
                                           e.UserName,
                                           e.Email,
                                           e.Role,
                                           e.PasswordNeverExpires,
                                           e.PasswordSetDate,
                                           e.Related,
                                       }).ToDynamic();

                UserGraph.Register();

                new BasicExecute<RoleDN>(RoleOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (r, args) => { }
                }.Register();

                new BasicDelete<RoleDN>(RoleOperation.Delete)
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
            if (!role.IsNew && role.Roles != null && role.Roles.SelfModified)
            {
                using (new EntityCache(true))
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
                            Signum.Engine.Extensions.Properties.Resources._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.Formato(problems.Count) +
                            problems.ToString("\r\n"));
                }
            }
        }

        static DirectedGraph<Lite<RoleDN>> Cache()
        {
            using (AuthLogic.Disable())
            {
                DirectedGraph<Lite<RoleDN>> newRoles = new DirectedGraph<Lite<RoleDN>>();

                using (new EntityCache(true))
                    foreach (var role in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(role.ToLite(), r => r.Retrieve().Roles);
                    }

                var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException(
                        Signum.Engine.Extensions.Properties.Resources._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.Formato(problems.Count) +
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
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.Username0IsNotValid.Formato(username));
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
            return Database.Query<UserDN>().SingleOrDefaultEx(u => u.UserName == username);
        }

        public static IEnumerable<Lite<RoleDN>> RolesInOrder()
        {
            return roles.Value.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.ToString()));
        }

        internal static DirectedGraph<Lite<RoleDN>> RolesGraph()
        {
            return roles.Value;
        }


        public static int Compare(Lite<RoleDN> role1, Lite<RoleDN> role2)
        {
            if (roles.Value.IndirectlyRelatedTo(role1).Contains(role2))
                return 1;

            if (roles.Value.IndirectlyRelatedTo(role2).Contains(role1))
                return -1;

            return 0;
        }

        public static IEnumerable<Lite<RoleDN>> RelatedTo(Lite<RoleDN> role)
        {
            return roles.Value.RelatedTo(role);
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
                    throw new IncorrectUsernameException(Resources.Username0IsNotValid.Formato(username));

                if (user.PasswordHash != passwordHash)
                    throw new IncorrectPasswordException(Resources.IncorrectPassword);

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

        public static void StartAllModules(SchemaBuilder sb, DynamicQueryManager dqm, params Type[] serviceInterfaces)
        {
            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb, true);

            if (serviceInterfaces != null && serviceInterfaces.Any())
                FacadeMethodAuthLogic.Start(sb, serviceInterfaces);

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

        public static event Func<XElement> ExportToXml;
        public static event Func<XElement, Dictionary<string, Lite<RoleDN>>, Replacements, SqlPreCommand> ImportFromXml;
        public static event Func<Action<Lite<RoleDN>>> SuggestRuleChanges;

        public static void SuggestChanges()
        {
            if (SuggestRuleChanges == null)
                return;

            foreach (var item in SuggestRuleChanges.GetInvocationList().Cast<Func<Action<RoleDN>>>())
            {
                SafeConsole.WriteLineColor(ConsoleColor.White, "{0}:".Formato(item.Method.DeclaringType.Name));

                foreach (var role in RolesInOrder())
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Gray, "Suggestions for {0}", role);

                    foreach (Action<Lite<RoleDN>> action in actions)
                    {
                        action(role);
                    }
                }
            }

            var actions = SuggestRuleChanges.GetInvocationList().Cast<Func<Action<RoleDN>>>().Select(f => f()).ToList();


        }

        public static XDocument ExportRules()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Auth",
                    new XElement("Roles",
                        RolesInOrder().Select(r => new XElement("Role",
                            new XAttribute("Name", r.ToString()),
                            new XAttribute("Contains", roles.Value.RelatedTo(r).ToString(","))))),
                     ExportToXml == null ? null : ExportToXml.GetInvocationList().Cast<Func<XElement>>().Select(a => a()).NotNull().OrderBy(a => a.Name.ToString())));
        }

        public static SqlPreCommand ImportRulesScript(XDocument doc)
        {
            Replacements replacements = new Replacements();

            var rolesDic = roles.Value.ToDictionary(a => a.ToString());
            var rolesXml = doc.Root.Element("Roles").Elements("Role").ToDictionary(x => x.Attribute("Name").Value);

            replacements.AskForReplacements(rolesXml.Keys.ToHashSet(), rolesDic.Keys.ToHashSet(), "Roles");

            rolesDic = replacements.ApplyReplacementsToNew(rolesDic, "Roles");

            var xmlOnly = rolesXml.Keys.Except(rolesDic.Keys).ToList();
            if (xmlOnly.Any())
                throw new InvalidOperationException("Roles not found in database: {0}".Formato(xmlOnly.ToString(", ")));

            foreach (var kvp in rolesXml)
            {
                var r = rolesDic[kvp.Key];

                EnumerableExtensions.JoinStrict(
                    roles.Value.RelatedTo(r),
                    kvp.Value.Attribute("Contains").Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    sr => sr.ToString(),
                    s => rolesDic[s].ToString(),
                    (sr, s) => 0,
                    "Checking SubRoles of {0}".Formato(r));
            }

            var dbOnlyWarnings = rolesDic.Keys.Except(rolesXml.Keys).Select(n =>
                    new SqlPreCommandSimple("-- Alien role {0} not configured!!".Formato(n))
                ).Combine(Spacing.Simple);



            var result = ImportFromXml.GetInvocationList()
                .Cast<Func<XElement, Dictionary<string, Lite<RoleDN>>, Replacements, SqlPreCommand>>()
                .Select(inv => inv(doc.Root, rolesDic, replacements)).Combine(Spacing.Triple);

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

        public static void ImportExportAuthRules()
        {
            ImportExportAuthRules("AuthRules.xml");
        }

        public static void ImportExportAuthRules(string fileName)
        {
            Console.WriteLine("You want to export (e), import (i) or suggest (s) AuthRules? (nothing to exit)".Formato(fileName));

            string answer = Console.ReadLine();

            if (answer.ToLower() == "e")
            {
                var doc = ExportRules();
                doc.Save(fileName);
                Console.WriteLine("Sucesfully exported to {0}".Formato(fileName));
            }
            else if (answer.ToLower() == "i")
            {
                Console.Write("Reading {0}...".Formato(fileName));
                var doc = XDocument.Load(fileName);
                Console.WriteLine("Ok");
                Console.Write("Importing...");
                SqlPreCommand command = ImportRulesScript(doc);
                Console.WriteLine("Ok");

                if (command == null)
                    Console.WriteLine("No changes necessary!");
                else
                    command.OpenSqlFileRetry();
            }
            else if (answer.ToLower() == "s")
            {
                SuggestChanges();
                if (SafeConsole.Ask("Export now?", "yes", "no") == "yes")
                {
                    var doc = ExportRules();
                    doc.Save(fileName);
                    Console.WriteLine("Sucesfully exported to {0}".Formato(fileName));
                }
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
    }
}
