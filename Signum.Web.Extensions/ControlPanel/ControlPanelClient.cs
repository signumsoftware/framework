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
        public static string AdminViewPrefix = "~/ControlPanel/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/ControlPanel/Views/{0}.cshtml";

        public static Dictionary<Type, string> PanelPartViews = new Dictionary<Type, string>()
        {
            {typeof(UserQueryPartDN), ViewPrefix.Formato("SearchControlPart") },
            {typeof(CountSearchControlPartDN), ViewPrefix.Formato("CountSearchControlPart") },
            {typeof(LinkListPartDN), ViewPrefix.Formato("LinkListPart") },
        };

        public static Dictionary<Type, string> PanelPartAdminViews = new Dictionary<Type, string>()
        {
            {typeof(UserQueryPartDN), AdminViewPrefix.Formato("SearchControlPart") },
            {typeof(CountSearchControlPartDN), AdminViewPrefix.Formato("CountSearchControlPart") },
            {typeof(LinkListPartDN), AdminViewPrefix.Formato("LinkListPart") },
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
                    new EmbeddedEntitySettings<PanelPart>() { PartialViewName = e => AdminViewPrefix.Formato("PanelPart") },
                    
                    new EntitySettings<UserQueryPartDN>(EntityType.Default),

                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("CountSearchControlPart") },
                    new EmbeddedEntitySettings<CountUserQueryElement>() { PartialViewName = e => AdminViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("LinkListPart") },
                    new EmbeddedEntitySettings<LinkElement>() { PartialViewName = e => AdminViewPrefix.Formato("LinkElement") },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });


                //Navigator.EntitySettings<ControlPanelDN>().MappingDefault.AsEntityMapping()
                //    .SetProperty(cp => cp.Parts, new MListDictionaryMapping<PanelPart, IIdentifiable>(pp => pp.Content, "Content"));

                ButtonBarEntityHelper.RegisterEntityButtons<ControlPanelDN>((controllerCtx, panel, viewName, prefix) => 
                {
                    return new ToolBarButton[]
                    {
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(prefix, "CreatePart"),
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
