using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Engine.Reports;
using Signum.Entities.ControlPanel;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;

namespace Signum.Engine.ControlPanel
{
    public static class ControlPanelLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueryLogic.Start(sb, dqm);

                sb.Include<ControlPanelDN>();

                dqm[typeof(ControlPanelDN)] = (from cp in Database.Query<ControlPanelDN>()
                                            select new
                                            {
                                                Entity = cp.ToLite(),
                                                cp.DisplayName,
                                                cp.Related,
                                            }).ToDynamic();

                dqm[typeof(LinkListPartDN)] = (from cp in Database.Query<LinkListPartDN>()
                                               select new
                                               {
                                                   Entity = cp.ToLite(),
                                                   cp.ToStr,
                                                   Links = cp.Links.Count
                                               }).ToDynamic();

                dqm[typeof(CountSearchControlPartDN)] = (from cp in Database.Query<CountSearchControlPartDN>()
                                               select new
                                               {
                                                   Entity = cp.ToLite(),
                                                   cp.ToStr,
                                                   Links = cp.UserQueries.Count
                                               }).ToDynamic(); 
            }
        }

        public static Lite<ControlPanelDN> GetHomePageControlPanel()
        {
            UserDN currentUser = UserDN.Current;
            if (currentUser == null)
                return null;

            var panel = Database.Query<ControlPanelDN>().Where(cp => cp.Related.ToLite<UserDN>().RefersTo(currentUser) && cp.HomePage).Select(a => a.ToLite()).FirstOrDefault();
            if (panel != null)
                return panel;

            var panels = Database.Query<ControlPanelDN>().Where(cp => cp.Related.Entity is RoleDN && cp.HomePage)
                .Select(cp => new { ControlPanel = cp.ToLite(), Role = ((RoleDN)cp.Related.Entity).ToLite() }).ToList();

            return panels.OrderByDescending(p => AuthLogic.Rank(p.Role)).FirstOrDefault().TryCC(p => p.ControlPanel);
        }

        public static void RegisterUserEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((ControlPanelDN uq) => uq.Related, typeof(UserDN));

            EntityGroupLogic.Register<ControlPanelDN>(newEntityGroupKey,
                uq => uq.Related.RefersTo(UserDN.Current),
                uq => uq.Related != null && uq.Related.Entity is UserDN);

            EntityGroupLogic.Register<CountSearchControlPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereInGroup(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)),
                 uq => Database.Query<ControlPanelDN>().WhereIsApplicable(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));

            EntityGroupLogic.Register<LinkListPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereInGroup(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)),
                 uq => Database.Query<ControlPanelDN>().WhereIsApplicable(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));
        }

        public static void RegisterRoleEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((ControlPanelDN uq) => uq.Related, typeof(RoleDN));

            EntityGroupLogic.Register<ControlPanelDN>(newEntityGroupKey,
                uq => AuthLogic.CurrentRoles().Contains(uq.Related.ToLite<RoleDN>()),
                uq => uq.Related != null && uq.Related.Entity is RoleDN);

            EntityGroupLogic.Register<CountSearchControlPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereInGroup(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)),
                 uq => Database.Query<ControlPanelDN>().WhereIsApplicable(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));

            EntityGroupLogic.Register<LinkListPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereInGroup(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)),
                 uq => Database.Query<ControlPanelDN>().WhereIsApplicable(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));
        }
    }
}
