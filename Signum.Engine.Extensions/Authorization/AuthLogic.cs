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

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        public static int MinRequiredPasswordLength = 6;

        public static string SystemUserName { get; set; }
        public static UserDN systemUser;
        public static UserDN SystemUser
        {
            get { return systemUser.ThrowIfNullC("SystemUser not loaded, Initialize to Level1SimpleEntities"); }
            set { systemUser = value; }
        }

        public static string AnonymousUserName { get; set; }
        public static UserDN anonymousUser;
        public static UserDN AnonymousUser
        {
            get { return anonymousUser.ThrowIfNullC("AnonymousUser not loaded, Initialize to Level1SimpleEntities"); }
            set { anonymousUser = value; }
        }


        static DirectedGraph<Lite<RoleDN>> _roles;
        static DirectedGraph<Lite<RoleDN>> Roles
        {
            get { return Sync.Initialize(ref _roles, () => Cache()); }
        }

        public static event Action RolesModified;

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
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RoleDN>().Saving += Schema_Saving;

                dqm[typeof(RoleDN)] = (from r in Database.Query<RoleDN>()
                                       select new
                                       {
                                           Entity = r.ToLite(),
                                           r.Id,
                                           r.Name,
                                       }).ToDynamic();

                dqm[RoleQueries.ReferedBy] = (from r in Database.Query<RoleDN>()
                                              from rc in Database.Query<RoleDN>()
                                              where r.Roles.Contains(rc.ToLite())
                                              select new
                                              {
                                                  Entity = r.ToLite(),
                                                  r.Id,
                                                  r.Name,
                                                  Refered = rc.ToLite(),
                                              }).ToDynamic();

                dqm[typeof(UserDN)] = (from e in Database.Query<UserDN>()
                                       select new
                                       {
                                           Entity = e.ToLite(),
                                           e.Id,
                                           e.UserName,
                                           e.Email,
                                           Rol = e.Role.ToLite(),
                                           Related = e.Related.ToLite(),
                                       }).ToDynamic();
            }
        }

        public static void StartUserOperations(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                new UserGraph().Register();
                OperationLogic.Register(new BasicExecute<UserDN>(UserOperation.NewPassword)
                {
                    Lite = false,
                    Execute = (u, _) => { throw new InvalidOperationException("This is an IU operation, not meant to be called in logic"); }
                });
            }
        }

        public static void StartResetPasswordRequest(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ResetPasswordRequestDN>();

                dqm[typeof(ResetPasswordRequestDN)] = (from e in Database.Query<ResetPasswordRequestDN>()
                                                       select new
                                                       {
                                                           Entity = e.ToLite(),
                                                           e.Id,
                                                           e.RequestDate,
                                                           e.Code,
                                                           User = e.User.ToLite(),
                                                           e.User.Email
                                                       }).ToDynamic();

                EmailLogic.AssertStarted(sb);

                EmailLogic.RegisterTemplate<ResetPasswordRequestMail>(model => 
                {
                    return new EmailContent
                    {
                         Subject = Resources.ResetPasswordCode,
                         Body = EmailRenderer.Replace(EmailRenderer.ReadFromResourceStream(typeof(AuthLogic).Assembly,
                            "Signum.Engine.Extensions.Authorization.ResetPasswordRequestMail.htm"), 
                                model, null, Resources.ResourceManager)
                    }; 
                });
            }
        }

        public static void StartSimpleResetPassword(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                EmailLogic.RegisterTemplate<ResetPasswordMail>(model =>
                {
                    return new EmailContent
                    {
                        Subject = Resources.ResetPasswordCode,
                        Body = EmailRenderer.Replace(EmailRenderer.ReadFromResourceStream(typeof(AuthLogic).Assembly, 
                            "Signum.Engine.Extensions.Authorization.ResetPasswordMail.htm"),
                               model, null, Resources.ResourceManager)
                    };
                });
            }
        }

        static void Schema_Initializing(Schema schema)
        {
            _roles = Cache();

            if (SystemUserName != null || AnonymousUserName != null)
            {
                using (new EntityCache())
                using (AuthLogic.Disable())
                {
                    SystemUser = Database.Query<UserDN>().Single(a => a.UserName == SystemUserName);
                    if (AnonymousUserName != null) AnonymousUser = Database.Query<UserDN>().Single(a => a.UserName == AnonymousUserName); //TODO: OLMO hay que proporcianarlo siempre?
                }
            }
        }

        static void Schema_Saving(RoleDN role, bool isRoot)
        {
            if (!role.IsNew && role.Roles != null && role.Roles.SelfModified)
            {
                using (new EntityCache())
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

            if (role.Modified.Value)
            {
                Transaction.RealCommit -= InvalidateCache;
                Transaction.RealCommit += InvalidateCache;
            }
        }



        public static void InvalidateCache()
        {
            _roles = null;

            if (RolesModified != null)
                RolesModified();
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

        public static IDisposable UnsafeUser(string username)
        {
            UserDN user;
            using (AuthLogic.Disable())
            {
                user = Database.Query<UserDN>().SingleOrDefault(u => u.UserName == username);
                if (user == null)
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.Username0IsNotValid.Formato(username));
            }

            return User(user);
        }

        public static IDisposable User(UserDN user)
        {
            IPrincipal old = Thread.CurrentPrincipal;
            Thread.CurrentPrincipal = user;
            return new Disposable(() =>
            {
                Thread.CurrentPrincipal = old;
            });
        }

        public static IEnumerable<Lite<RoleDN>> RolesInOrder()
        {
            return Roles.CompilationOrder();
        }

        public static int Compare(Lite<RoleDN> role1, Lite<RoleDN> role2)
        {
            if (Roles.IndirectlyRelatedTo(role1).Contains(role2))
                return 1;

            if (Roles.IndirectlyRelatedTo(role2).Contains(role1))
                return -1;

            return 0;
        }

        public static IEnumerable<Lite<RoleDN>> RelatedTo(Lite<RoleDN> role)
        {
            return Roles.RelatedTo(role);
        }

        static bool gloaballyEnabled = true;
        public static bool GloballyEnabled
        {
            get { return gloaballyEnabled; }
            set { gloaballyEnabled = value; }
        }

        [ThreadStatic]
        static bool temporallyDisabled;

        public static IDisposable Disable()
        {
            bool lastValue = temporallyDisabled;
            temporallyDisabled = true;
            return new Disposable(() => temporallyDisabled = lastValue);
        }

        public static IDisposable Enabled()
        {
            bool lastValue = temporallyDisabled;
            temporallyDisabled = false;
            return new Disposable(() => temporallyDisabled = lastValue);
        }

        public static bool IsEnabled
        {
            get { return !temporallyDisabled && gloaballyEnabled; }
        }

        public static UserDN Login(string username, string passwordHash)
        {
            using (AuthLogic.Disable())
            {
                UserDN user = Database.Query<UserDN>().SingleOrDefault(u => u.UserName == username);
                if (user == null)
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.Username0IsNotValid.Formato(username));

                if (user.PasswordHash != passwordHash)
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.IncorrectPassword);

                return user;
            }
        }

        public static UserDN UserToRememberPassword(string username, string email)
        {
            UserDN user = null;
            using (AuthLogic.Disable())
            {
                user = Database.Query<UserDN>().SingleOrDefault(u => u.UserName == username);
                if (user == null)
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.Username0IsNotValid.Formato(username));

                if (user.Email != email)
                    throw new ApplicationException(Signum.Engine.Extensions.Properties.Resources.EmailIsNotValid);
            }
            return user;
        }

        public static void StartAllModules(SchemaBuilder sb, DynamicQueryManager dqm, params Type[] serviceInterfaces)
        {
            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb, true);

            if (serviceInterfaces != null)
                FacadeMethodAuthLogic.Start(sb, serviceInterfaces);

            QueryAuthLogic.Start(sb, dqm);
            OperationAuthLogic.Start(sb);
            PermissionAuthLogic.Start(sb);
        }

        public static void ResetPasswordRequest(UserDN user, string fullyQualifiedApplicationPath)
        {
            //Remove old previous requests
            Database.Query<ResetPasswordRequestDN>()
                .Where(r => r.User.Is(user) && r.RequestDate < TimeZoneManager.Now.AddMonths(1))
                .UnsafeDelete();

            ResetPasswordRequestDN rpr = new ResetPasswordRequestDN()
            {
                Code = MyRandom.Current.NextString(5),
                User = user,
                RequestDate = TimeZoneManager.Now,
            }.Save();


            new ResetPasswordRequestMail
            {
                To = user, 
                Link = fullyQualifiedApplicationPath  + "Auth/ResetPasswordCode?email=" + user.Email + "&code=" + rpr.Code, 
            }.Send();
        }

        internal static Lite<RoleDN>[] CurrentRoles()
        {
            return Roles.IndirectlyRelatedTo(RoleDN.Current.ToLite()).And(RoleDN.Current.ToLite()).ToArray();
        }

        internal static int Rank(Lite<RoleDN> role)
        {
            return Roles.IndirectlyRelatedTo(role).Count; 
        }

        public static event Func<XElement> ExportToXml;
        public static event Func<XElement, Dictionary<string, Lite<RoleDN>>, SqlPreCommand> ImportFromXml;

        public static XDocument ExportRules()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Auth",
                    new XElement("Roles", 
                        RolesInOrder().Select(r=>new XElement("Role", 
                            new XAttribute("Name", r.ToStr),  
                            new XAttribute("Contains",  Roles.RelatedTo(r).ToString(","))))),
                     ExportToXml == null ? null : ExportToXml.GetInvocationList().Cast<Func<XElement>>().Select(a => a()).NotNull()));
        }

        public static SqlPreCommand ImportRules(XDocument doc)
        {
            EnumerableExtensions.JoinStrict(
                Roles,
                doc.Root.Element("Roles").Elements("Role"),
                r => r.ToStr,
                x => x.Attribute("Name").Value,
                (r, x) => EnumerableExtensions.JoinStrict(
                            Roles.RelatedTo(r),
                            x.Attribute("Contains").Value.Split(','),
                            sr => sr.ToStr,
                            s => s,
                            (sr, s) => 0,
                            "Checking SubRoles of {0}".Formato(r)),
                "Checking Roles");

            var rolesDic = Roles.ToDictionary(a => a.ToStr);

            var result = ImportFromXml.GetInvocationList()
                .Cast<Func<XElement, Dictionary<string, Lite<RoleDN>>, SqlPreCommand>>()
                .Select(inv => inv(doc.Root, rolesDic)).Combine(Spacing.Triple);

            return SqlPreCommand.Combine(Spacing.Triple,
                new SqlPreCommandSimple("-- BEGIN AUTH SYNC SCRIPT"),
                new SqlPreCommandSimple("use {0}".Formato(ConnectionScope.Current.DatabaseName())),
                result,
                new SqlPreCommandSimple("-- END AUTH SYNC SCRIPT")); 
        }

        public static void ImportExportAuthRules()
        {
            ImportExportAuthRules("AuthRules.xml");
        }

        public static void ImportExportAuthRules(string fileName)
        {
            Console.WriteLine("You want to export (e), import (i) or exit (nothing) {0}?".Formato(fileName));

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
                SqlPreCommand command = ImportRules(doc);
                Console.WriteLine("Ok");

                command.OpenSqlFileRetry();
            }
        }
    }

    public class ResetPasswordMail : EmailModel<UserDN>
    {
        public string NewPassord;
    }

    public class ResetPasswordRequestMail : EmailModel<UserDN>
    {
        public string Link;
    }
}
