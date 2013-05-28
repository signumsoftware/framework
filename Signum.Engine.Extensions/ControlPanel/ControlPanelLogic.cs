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
using Signum.Entities.UserQueries;

namespace Signum.Engine.ControlPanel
{
    public static class ControlPanelLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueryLogic.Start(sb, dqm);

                PermissionAuthLogic.RegisterPermissions(ControlPanelPermission.ViewControlPanel);

                UserAssetsImporter.UserAssetNames.Add("ControlPanel", typeof(ControlPanelDN));

                UserAssetsImporter.PartNames.AddRange(new Dictionary<string, Type>
                {
                    {"UserChartPart", typeof(UserChartPartDN)},
                    {"UserQueryPart", typeof(UserQueryPartDN)},
                    {"LinkListPart", typeof(LinkListPartDN)},
                    {"CountSearchControlPart", typeof(CountSearchControlPartDN)},
                }); 

                sb.Include<ControlPanelDN>();


                dqm.RegisterQuery(typeof(ControlPanelDN), () =>
                    from cp in Database.Query<ControlPanelDN>()
                    select new
                    {
                        Entity = cp,
                        cp.DisplayName,
                        cp.EntityType,
                        cp.Related,
                    });

                dqm.RegisterQuery(typeof(LinkListPartDN), () =>
                    from cp in Database.Query<LinkListPartDN>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.Links.Count
                    });

                dqm.RegisterQuery(typeof(CountSearchControlPartDN), () =>
                    from cp in Database.Query<CountSearchControlPartDN>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.UserQueries.Count
                    });

                ControlPanelGraph.Register();
            }
        }

        class ControlPanelGraph : Graph<ControlPanelDN>
        {
            public static void Register()
            {
                new Construct(ControlPanelOperation.Create)
                {
                    Construct = (_) => new ControlPanelDN { Related = UserQueryUtils.DefaultRelated() }
                }.Register();

                new Execute(ControlPanelOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (cp, _) => { }
                }.Register();

                new Delete(ControlPanelOperation.Delete)
                {
                    Lite = false,
                    Delete = (cp, _) =>
                    {
                        var parts = cp.Parts.Select(a => a.Content).ToList();
                        cp.Delete();
                        Database.DeleteList(parts);
                    }
                }.Register();

                new ConstructFrom<ControlPanelDN>(ControlPanelOperation.Clone)
                {
                    Lite = true,
                    AllowsNew = false,
                    Construct = (cp, _) => cp.Clone()
                }.Register();
            }
        }

        public static ControlPanelDN GetHomePageControlPanel()
        {
            var cps = Database.Query<ControlPanelDN>()
                .Where(a => a.HomePagePriority.HasValue)
                .OrderByDescending(a => a.HomePagePriority)
                .Select(a => a.ToLite())
                .FirstOrDefault();

            if (cps == null)
                return null;

            return cps.Retrieve(); //I assume this simplifies the cross applys.
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
                uq => AuthLogic.CurrentRoles().Contains(uq.Related));

            TypeConditionLogic.Register<CountSearchControlPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));

            TypeConditionLogic.Register<LinkListPartDN>(newEntityGroupKey,
                 uq => Database.Query<ControlPanelDN>().WhereCondition(newEntityGroupKey).Any(cp => cp.ContainsContent(uq)));
        }

        public static List<Lite<ControlPanelDN>> GetControlPanelsEntity(Type entityType)
        {
            return (from er in Database.Query<ControlPanelDN>()
                    where er.EntityType == entityType.ToTypeDN().ToLite()
                    select er.ToLite()).ToList();
        }

        public static List<Lite<ControlPanelDN>> Autocomplete(string subString, int limit)
        {
            return Database.Query<ControlPanelDN>().Where(cp => cp.EntityType == null).Autocomplete(subString, limit);
        }
    }
}
