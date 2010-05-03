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
using Signum.Entities.Extensions.Properties;
using Signum.Engine.Mailing;

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {

        public static int MinRequiredPasswordLength = 6;

        public static UserDN SystemUser { get; set; }
        public static string SystemUserName { get; set; }

        public static UserDN AnonymousUser { get; set; }
        public static string AnonymousUserName { get; set; }

        static DirectedGraph<Lite<RoleDN>> _roles;
        static DirectedGraph<Lite<RoleDN>> Roles
        {
            get { return Sync.Initialize(ref _roles, () => Cache()); }
        }

        public static event Action RolesModified;

        public static void AssertIsStarted(SchemaBuilder sb)
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
                sb.Schema.EntityEvents<RoleDN>().Saved += Schema_Saved;

                dqm[typeof(RoleDN)] = (from r in Database.Query<RoleDN>()
                                       select new
                                       {
                                           Entity = r.ToLite(),
                                           r.Id,
                                           r.Name,                                          
                                       }).ToDynamic();

                dqm[typeof(UserDN)] = (from e in Database.Query<UserDN>()
                                       select new
                                       {
                                           Entity = e.ToLite(),
                                           e.Id,
                                           e.UserName,
                                           Rol = e.Role.ToLite(),
                                           //Empleado = e.Related.ToString(),
                                       }).ToDynamic();
            }
        }

        public static void StartPortal(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                //starts password modification feature
                sb.Include<ResetPasswordRequestDN>();

                dqm[typeof(ResetPasswordRequestDN)] = (from e in Database.Query<ResetPasswordRequestDN>()
                                                       select new
                                                       {
                                                           Entity = e.ToLite(),
                                                           e.Id,
                                                           e.RequestDate,
                                                           e.Code,
                                                           e.Email
                                                       }).ToDynamic();

                EmailLogic.Start(sb, dqm);

                EmailLogic.RegisterTemplate(UserMailTemplate.ResetPassword, (eo, args) =>
                {
                    ResetPasswordRequestDN request = (ResetPasswordRequestDN)eo;
                    return EmailLogic.RenderWebMail(Signum.Engine.Extensions.Properties.Resources.ResetPasswordCode,
                        "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.ResetPasswordMail.ascx", eo, args);
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
                    SystemUser = Database.Query<UserDN>().SingleOrDefault(a => a.UserName == SystemUserName);
                    AnonymousUser = Database.Query<UserDN>().SingleOrDefault(a => a.UserName == AnonymousUserName);
                }
            }
        }

        static void Schema_Saving(RoleDN role, bool isRoot)
        {
            if (!role.IsNew && role.Roles != null && role.Roles.Modified)
            {
                using (new EntityCache())
                {
                    EntityCache.AddFullGraph(role);

                    DirectedGraph<RoleDN> newRoles = new DirectedGraph<RoleDN>();

                    newRoles.Expand(role, r1 => r1.Roles.Select(a=>a.Retrieve()));
                    foreach (var r in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(r, r1 => r1.Roles.Select(a => a.Retrieve()));
                    }

                    var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                    if (problems.Count > 0)
                        throw new InvalidOperationException(
                            Signum.Engine.Extensions.Properties.Resources._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships.Formato(problems.Count) +
                            problems.ToString("\r\n"));
                }
            }
        }

        static void Schema_Saved(RoleDN role, bool isRoot)
        {
            Transaction.RealCommit += () => _roles = null;

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
                    throw new InvalidOperationException(
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

        public static void StartAllModules(SchemaBuilder sb, Type serviceInterface, DynamicQueryManager dqm)
        {
            TypeAuthLogic.Start(sb);
            PropertyAuthLogic.Start(sb, true);
            FacadeMethodAuthLogic.Start(sb, serviceInterface);
            QueryAuthLogic.Start(sb, dqm);
            OperationAuthLogic.Start(sb);
            PermissionAuthLogic.Start(sb);
        }
    }
}
