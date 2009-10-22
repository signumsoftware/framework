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

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        public static UserDN SystemUser { get; set; }
        public static string SystemUserName { get; set; }

        static DirectedGraph<RoleDN> _roles;
        static DirectedGraph<RoleDN> Roles
        {
            get { return Sync.Initialize(ref _roles, () => Cache()); }
        }

        public static event Action RolesModified;

        public static void AssertIsStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(typeof(AuthLogic).GetMethod("Start"));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, string systemUserName)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SystemUserName = systemUserName; 

                sb.Include<UserDN>();
                sb.Include<RoleDN>();
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RoleDN>().Saving += Schema_Saving;
                sb.Schema.EntityEvents<RoleDN>().Saved += Schema_Saved;

                dqm[typeof(RoleDN)] = (from r in Database.Query<RoleDN>()
                                       select new
                                       {
                                           Entity = r.ToLazy(),
                                           r.Id,
                                           r.Name,                                          
                                       }).ToDynamic();

                dqm[typeof(UserDN)] = (from e in Database.Query<UserDN>()
                                       select new
                                       {
                                           Entity = e.ToLazy(),
                                           e.Id,
                                           e.UserName,
                                           Rol = e.Role.ToLazy(),
                                           //Empleado = e.Related.ToString(),
                                       }).ToDynamic();
            }
        }

  
        static void Schema_Initializing(Schema schema)
        {
            _roles = Cache();

            if (SystemUserName != null)
                using (new EntityCache())
                using (AuthLogic.Disable())
                {
                    SystemUser = Database.Query<UserDN>().SingleOrDefault(a => a.UserName == SystemUserName);
                }
        }

        static void Schema_Saving(RoleDN role, ref bool graphModified)
        {
            if (!role.IsNew && role.Roles != null && role.Roles.Modified && role.Roles.Except(Roles.RelatedTo(role)).Any())
            {
                using (new EntityCache())
                {
                    EntityCache.AddFullGraph(role);

                    DirectedGraph<RoleDN> newRoles = new DirectedGraph<RoleDN>();

                    newRoles.Expand(role, r1 => r1.Roles);
                    foreach (var r in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(r, r1 => r1.Roles);
                    }

                    var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                    if (problems.Count > 0)
                        throw new ApplicationException("Some cycles have been found in the graph of Roles due to the relationships:\r\n{1}"
                            .Formato(problems.Count, problems.ToString("\r\n")));
                }
            }
        }

        static void Schema_Saved(RoleDN role)
        {
            Transaction.RealCommit += () => _roles = null;

            if (RolesModified != null)
                RolesModified();
        }

        static DirectedGraph<RoleDN> Cache()
        {
            using (AuthLogic.Disable())
            {
                DirectedGraph<RoleDN> newRoles = new DirectedGraph<RoleDN>();

                using (new EntityCache(true))
                    foreach (var role in Database.RetrieveAll<RoleDN>())
                    {
                        newRoles.Expand(role, r => r.Roles);
                    }

                var problems = newRoles.FeedbackEdgeSet().Edges.ToList();

                if (problems.Count > 0)
                    throw new ApplicationException("Some cycles have been found in the graph of Roles due to the relationships:\r\n{1}"
                        .Formato(problems.Count, problems.ToString("\r\n")));

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
                    throw new ApplicationException(Resources.Username0IsNotValid.Formato(username));
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

        public static IEnumerable<RoleDN> RolesInOrder()
        {
            return Roles.CompilationOrder();
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
                    throw new ApplicationException(Resources.Username0IsNotValid.Formato(username));

                if (user.PasswordHash != passwordHash)
                    throw new ApplicationException(Resources.IncorrectPassword);

                return user;
            }
        }
    }
}
