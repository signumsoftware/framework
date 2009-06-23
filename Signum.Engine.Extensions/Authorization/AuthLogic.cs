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

namespace Signum.Engine.Authorization
{
    public static class AuthLogic
    {
        static DirectedGraph<RoleDN> _roles;
        static DirectedGraph<RoleDN> Roles
        {
            get { return Sync.Initialize(ref _roles, () => Cache()); }
        }

        public static event InitEventHandler RolesModified; 

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<UserDN>())
            {
                sb.Include<UserDN>();
                sb.Include<RoleDN>();
                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Saving += Schema_Saving;
                sb.Schema.Saved += Schema_Saved;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _roles = Cache();
        }

        static void Schema_Saving(Schema sender, IdentifiableEntity ident)
        {
            RoleDN role = ident as RoleDN;
            if (role != null && !role.IsNew && role.Roles.Modified && role.Roles.Except(Roles.RelatedTo(role)).Any())
            {
                 using(new EntityCache())
                 {
                    EntityCache.AddFullGraph(ident);

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

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is RoleDN)
            {
                Transaction.RealCommit += () => _roles = null;

                if (RolesModified != null)
                    RolesModified(sender);
            }
        }

        static DirectedGraph<RoleDN> Cache()
        {
            using (AuthLogic.Disable())
            {
                DirectedGraph<RoleDN> newRoles = new DirectedGraph<RoleDN>();

                using (new EntityCache())
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
            temporallyDisabled = true;
            return new Disposable(() => temporallyDisabled = false);
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
                    throw new ApplicationException("Username {0} is not valid".Formato(username));

                if (user.PasswordHash != passwordHash)
                    throw new ApplicationException("Incorrect password");

                return user; 
            }
        }
    }
}
