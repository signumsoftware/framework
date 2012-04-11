#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
#endregion

namespace Signum.Web.Operations
{
    public static class OperationsClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager, bool contextualMenuInSearchWindow)
        {
            Manager = operationManager;

            Navigator.RegisterArea(typeof(OperationsClient));
            
            var scripts = Navigator.Manager.DefaultScripts();
            scripts.Add("~/Operations/Scripts/SF_Operations.js");
            Navigator.Manager.DefaultScripts += () => scripts;

            ButtonBarEntityHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarElement);

            Constructor.ConstructorManager.GeneralConstructor += new Func<Type, ModifiableEntity>(Manager.ConstructorManager_GeneralConstructor);
            Constructor.ConstructorManager.VisualGeneralConstructor += new Func<ConstructContext, ActionResult>(Manager.ConstructorManager_VisualGeneralConstructor); 
            ButtonBarQueryHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarForQueryName);

            if (contextualMenuInSearchWindow)
                OperationsContextualItemsHelper.Start();
        }

        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.AddOrThrow(setting.Key, setting, "EntitySettings {0} repeated");
        }

        public static void AddSettings(List<OperationSettings> settings)
        {
            Manager.Settings.AddRange(settings, s => s.Key, s => s, "EntitySettings");
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        internal ToolBarButton[] ButtonBar_GetButtonBarElement(EntityButtonContext ctx, ModifiableEntity entity)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var contexts =
                    from oi in list
                    let os = (EntityOperationSettings)Settings.TryGetC(oi.Key)
                    let octx = new EntityOperationContext
                    {
                         Entity = ident,
                         OperationSettings = os,
                         OperationInfo = oi,
                         PartialViewName = ctx.PartialViewName,
                         Prefix = ctx.Prefix
                    }
                    where (ctx.Buttons == ViewButtons.Save || os.TryCS(sett => sett.IsVisibleOnOkPopup) == true) &&
                          (os == null || os.IsVisible == null || os.IsVisible(octx))
                    select octx;

            List<ToolBarButton> buttons = contexts
                .Where(oi => oi.OperationInfo.OperationType != OperationType.ConstructorFrom || 
                            (oi.OperationInfo.OperationType == OperationType.ConstructorFrom && oi.OperationSettings != null && !oi.OperationSettings.GroupInMenu))
                .Select(octx => OperationButtonFactory.Create(octx))
                .ToList();

            var constructFroms = contexts.Where(oi => oi.OperationInfo.OperationType == OperationType.ConstructorFrom && 
                            (oi.OperationSettings == null || (oi.OperationSettings != null && oi.OperationSettings.GroupInMenu)));
            if (constructFroms.Any())
            {
                string createText = Resources.Create;
                buttons.Add(new ToolBarMenu
                {
                    Id = "tmConstructors",
                    AltText = createText,
                    Text = createText,
                    DivCssClass = ToolBarButton.DefaultEntityDivCssClass,
                    Items = constructFroms.Select(octx => OperationButtonFactory.Create(octx)).ToList()
                });
            }

            return buttons.ToArray();
        }

        internal ToolBarButton[] ButtonBar_GetButtonBarForQueryName(QueryButtonContext qbc)
        {
            if (qbc.EntityType == null || qbc.QueryName == null)
                return null;

            var list = OperationLogic.ServiceGetQueryOperationInfos(qbc.EntityType);
            var contexts = (from oi in list
                           let os = (QueryOperationSettings)Settings.TryGetC(oi.Key)
                           let ctx = new QueryOperationContext
                           {
                               OperationSettings = os,
                               OperationInfo = oi,
                               Prefix = qbc.Prefix
                           }
                           where os == null || os.IsVisible == null || os.IsVisible(ctx)
                           select ctx).ToList();

            if (contexts.Count == 1)
                return new ToolBarButton[] { OperationButtonFactory.Create(contexts[0]) };
            
            List<ToolBarButton> buttons = contexts
                .Where(oi => oi.OperationSettings != null && !oi.OperationSettings.GroupInMenu)
                .Select(ctx => OperationButtonFactory.Create(ctx))
                .ToList();

            var groupedConstructs = contexts.Where(oi => oi.OperationSettings == null || (oi.OperationSettings != null && oi.OperationSettings.GroupInMenu));
            if (groupedConstructs.Any())
            {
                string createText = Resources.Create;
                buttons.Add(new ToolBarMenu
                {
                    Id = "tmConstructors",
                    AltText = createText,
                    Text = createText,
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = groupedConstructs.Select(ctx => OperationButtonFactory.Create(ctx)).ToList()
                });
            }

            return buttons.ToArray();
        }

        internal ModifiableEntity ConstructorManager_GeneralConstructor(Type type)
        {
            if (!type.IsIIdentifiable())
                return null;

            OperationInfo constructor = OperationLogic.ServiceGetConstructorOperationInfos(type).SingleOrDefaultEx();

            if (constructor == null)
                return null;

            return (ModifiableEntity)OperationLogic.ServiceConstruct(type, constructor.Key);
        }

        internal ActionResult ConstructorManager_VisualGeneralConstructor(ConstructContext ctx)
        {
            var count = OperationLogic.ServiceGetConstructorOperationInfos(ctx.Type).Count;

            if (count == 0 || count == 1)
                return null;

            throw new NotImplementedException();  //show chooser
        }
    }
}
