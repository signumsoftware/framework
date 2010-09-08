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
                                                Related = cp.Related.ToLite(),
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

        public static ControlPanelDN GetHomePageControlPanel()
        {
            UserDN currentUser = UserDN.Current;
            if (currentUser == null)
                return null;

            var panel = Database.Query<ControlPanelDN>().FirstOrDefault(cp => cp.Related == currentUser && cp.HomePage);
            if (panel != null)
                return panel;

            return Database.Query<ControlPanelDN>().FirstOrDefault(cp => cp.Related == currentUser.Role && cp.HomePage);
        }
    }
}
