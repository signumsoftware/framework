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
using Signum.Engine.UserQueries;
using Signum.Engine.Operations;

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
                                                Entity = cp,
                                                cp.DisplayName,
                                                cp.Related,
                                            }).ToDynamic();

                dqm[typeof(LinkListPartDN)] = (from cp in Database.Query<LinkListPartDN>()
                                               select new
                                               {
                                                   Entity = cp,
                                                   ToStr = cp.ToString(),
                                                   Links = cp.Links.Count
                                               }).ToDynamic();

                dqm[typeof(CountSearchControlPartDN)] = (from cp in Database.Query<CountSearchControlPartDN>()
                                               select new
                                               {
                                                   Entity = cp,
                                                   ToStr = cp.ToString(),
                                                   Links = cp.UserQueries.Count
                                               }).ToDynamic();

                RegisterOperations();
            }
        }

        private static void RegisterOperations()
        {
            new BasicExecute<ControlPanelDN>(ControlPanelOperations.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (cp, _) => { }
            }.Register();

            new BasicDelete<ControlPanelDN>(ControlPanelOperations.Delete)
            {
                Lite = false,
                Delete = (cp, _) =>
                {
                    var parts = cp.Parts.Select(a => a.Content).ToList();
                    cp.Delete();
                    Database.DeleteList(parts);
                }
            }.Register();

            new BasicConstructFrom<ControlPanelDN, ControlPanelDN>(ControlPanelOperations.Clone)
            {
                Lite = false,
                Construct = (cp, _) => cp.Clone()
            }.Register();
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

            var hs = AuthLogic.RolesGraph().IndirectlyRelatedTo(RoleDN.Current.ToLite(), true);

            return panels.Where(p => hs.Contains(p.Role)).OrderByDescending(p => AuthLogic.Rank(p.Role)).FirstOrDefault().TryCC(p => p.ControlPanel);
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((ControlPanelDN uq) => uq.Related, typeof(UserDN));

            TypeConditionLogic.Register<ControlPanelDN>(newEntityGroupKey,
                uq => uq.Related.RefersTo(UserDN.Current));

            TypeConditionLogic.Register<CountSearchControlPartDN>(newEntityGroupKey,
                 cscp => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(cscp)));

            TypeConditionLogic.Register<LinkListPartDN>(newEntityGroupKey,
                 llp => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(llp)));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((ControlPanelDN uq) => uq.Related, typeof(RoleDN));

            TypeConditionLogic.Register<ControlPanelDN>(newEntityGroupKey,
                uq => AuthLogic.CurrentRoles().Contains(uq.Related.ToLite<RoleDN>()));

            TypeConditionLogic.Register<CountSearchControlPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));

            TypeConditionLogic.Register<LinkListPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));
        }
    }
}
