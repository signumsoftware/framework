using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.ControlPanel;
using Signum.Entities.Authorization;
using System.Reflection;
using System.Web.Routing;
using Signum.Web.UserQueries;
using Signum.Entities;
using Signum.Entities.Reports;
using Signum.Web.Extensions.Properties;
using Signum.Web.Controllers;

namespace Signum.Web.ControlPanel
{
    public class ControlPanelClient
    {
        public static long RefreshMilliseconds = 300000; //5 minutes

        public static string AdminViewPrefix = "~/ControlPanel/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/ControlPanel/Views/{0}.cshtml";

        public struct PartViews
        {
            public PartViews(string frontEnd, string admin)
            {
                FrontEnd = frontEnd;
                Admin = admin;
            }

            public string FrontEnd;
            public string Admin;
        }

        public static Dictionary<Type, PartViews> PanelPartViews = new Dictionary<Type, PartViews>()
        {
            { typeof(UserQueryPartDN), new PartViews(ViewPrefix.Formato("SearchControlPart"), AdminViewPrefix.Formato("SearchControlPart")) },
            { typeof(CountSearchControlPartDN), new PartViews(ViewPrefix.Formato("CountSearchControlPart"), AdminViewPrefix.Formato("CountSearchControlPart")) },
            { typeof(LinkListPartDN), new PartViews(ViewPrefix.Formato("LinkListPart"), AdminViewPrefix.Formato("LinkListPart")) },
        };

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueriesClient.Start();

                Navigator.RegisterArea(typeof(ControlPanelClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("ControlPanelAdmin") },
                    new EmbeddedEntitySettings<PanelPart>(),
                    
                    new EntitySettings<UserQueryPartDN>(EntityType.Default),

                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default),
                    new EmbeddedEntitySettings<CountUserQueryElement>() { PartialViewName = e => AdminViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default),
                    new EmbeddedEntitySettings<LinkElement>() { PartialViewName = e => AdminViewPrefix.Formato("LinkElement") },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });

                ButtonBarEntityHelper.RegisterEntityButtons<ControlPanelDN>((ctx, panel) => 
                {
                    return new ToolBarButton[]
                    {
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "CreatePart"),
                            Text = Resources.ControlPanel_CreateNewPart,
                            Enabled = panel.IsNew ? false : true,
                            AltText = panel.IsNew ? Resources.ControlPanel_YouMustSaveThePanelBeforeAddingParts : Resources.ControlPanel_CreateNewPart,
                            OnClick = panel.IsNew ? "" : 
                                JsValidator.EntityIsValid(new JsValidatorOptions
                                {
                                    ControllerUrl = RouteHelper.New().Action<SignumController>(sc => sc.Validate())
                                }, new JsFunction() 
                                {
                                    Js.OpenTypeChooser(
                                        "New", 
                                        new JsFunction("chosen") 
                                        {  
                                            Js.Submit(RouteHelper.New().Action<ControlPanelController>(cpc => cpc.AddNewPart()), "{ newPartType: chosen }"),
                                        },
                                        PanelPartViews.Keys.Select(t => Navigator.ResolveWebTypeName(t)).ToArray()
                                    )
                                }).ToJS()
                        }
                    };
                });
            }
        }
    }
}
