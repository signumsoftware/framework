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
#endregion

namespace Signum.Web.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager, bool contextualMenuInSearchWindow)
        {
            Manager = operationManager;

            Navigator.RegisterArea(typeof(OperationClient));

            ButtonBarEntityHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarElement);

            Constructor.ConstructorManager.GeneralConstructor += new Func<Type, ModifiableEntity>(Manager.ConstructorManager_GeneralConstructor);
            Constructor.ConstructorManager.VisualGeneralConstructor += new Func<ConstructContext, ActionResult>(Manager.ConstructorManager_VisualGeneralConstructor); 
            ButtonBarQueryHelper.GetButtonBarForQueryName += Manager.ButtonBar_GetButtonBarForQueryName;

            if (contextualMenuInSearchWindow)
                OperationsContextualItemsHelper.Start();
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        internal ToolBarButton[] ButtonBar_GetButtonBarElement(ControllerContext controllerContext, ModifiableEntity entity, string partialViewName, string prefix)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var contexts =
                    from oi in list
                    let os = (EntityOperationSettings)Settings.TryGetC(oi.Key)
                    let ctx = new EntityOperationContext
                    {
                         Entity = ident,
                         OperationSettings = os,
                         OperationInfo = oi,
                         PartialViewName = partialViewName,
                         Prefix = prefix
                    }
                    where os == null || os.IsVisible == null || os.IsVisible(ctx)
                    select ctx;

            List<ToolBarButton> buttons = contexts
                .Where(oi => oi.OperationInfo.OperationType != OperationType.ConstructorFrom || 
                            (oi.OperationInfo.OperationType == OperationType.ConstructorFrom && oi.OperationSettings != null && !oi.OperationSettings.GroupInMenu))
                .Select(ctx => OperationButtonFactory.Create(ctx))
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
                    Items = constructFroms.Select(ctx => OperationButtonFactory.Create(ctx)).ToList()
                });
            }

            return buttons.ToArray();
        }

        internal ToolBarButton[] ButtonBar_GetButtonBarForQueryName(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            if (entityType == null || queryName == null)
                return null;

            var list = OperationLogic.ServiceGetQueryOperationInfos(entityType);
            var contexts = (from oi in list
                           let os = (QueryOperationSettings)Settings.TryGetC(oi.Key)
                           let ctx = new QueryOperationContext
                           {
                               OperationSettings = os,
                               OperationInfo = oi,
                               Prefix = prefix
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

            OperationInfo constructor = OperationLogic.ServiceGetConstructorOperationInfos(type).SingleOrDefault();

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
