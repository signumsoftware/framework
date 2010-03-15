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
#endregion

namespace Signum.Web.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager)
        {
            Manager = operationManager;

            ButtonBarHelper.GetButtonBarElement += Manager.ButtonBar_GetButtonBarElement;

            Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;

            ButtonBarHelper.GetButtonBarForQueryName += Manager.ButtonBar_GetButtonBarForQueryName;
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationButton> Settings = new Dictionary<Enum, OperationButton>();

        internal List<ToolBarButton> ButtonBar_GetButtonBarElement(HttpContextBase httpContext, object entity, string mainControlUrl)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            if (list == null || list.Count == 0)
                return null;

            var dic = (from oi in list
                       let omi = Settings.TryGetC(oi.Key)
                       where omi.TryCC(sett => sett.Settings).TryCC(oset => oset as EntityOperationSettings).TryCC(eoset => eoset.IsVisible) == null || ((EntityOperationSettings)omi.Settings).IsVisible(ident)
                       select new { OperationInfo = oi, OperationMenuItem = omi }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;

            List<ToolBarButton> items = new List<ToolBarButton>();

            foreach (OperationInfo oi in list)
            {
                if (dic.ContainsKey(oi.Key))
                {
                    OperationButton item = new OperationButton
                    {
                        OperationInfo = oi,
                        Settings = dic[oi.Key].OperationMenuItem.TryCC(set => set.Settings)
                    };

                    items.Add(item);
                }
            }
            
            return items;
        }

        internal List<ToolBarButton> ButtonBar_GetButtonBarForQueryName(HttpContextBase httpContext, object queryName, Type entityType)
        {
            if (entityType == null || queryName == null)
                return null;

            var list = OperationLogic.ServiceGetQueryOperationInfos(entityType);

            if (list == null || list.Count == 0)
                return null;

            var dic = (from oi in list
                       let omi = Settings.TryGetC(oi.Key)
                       where omi.TryCC(sett => sett.Settings).TryCC(oset => oset as ConstructorFromManySettings).TryCS(eoset => eoset.IsVisible(queryName, oi)) == null || ((ConstructorFromManySettings)omi.Settings).IsVisible(queryName, oi)
                       select new { OperationInfo = oi, OperationMenuItem = omi }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;

            List<ToolBarButton> items = new List<ToolBarButton>();

            foreach (OperationInfo oi in list)
            {
                if (dic.ContainsKey(oi.Key))
                {
                    ToolBarButton item = new OperationButton
                    {
                        OperationInfo = oi,
                        Settings = dic[oi.Key].OperationMenuItem.TryCC(set => set.Settings)
                    };
                    
                    items.Add(item);
                }
            }

            return items;
        }

        //protected internal virtual string GetServerClickAjax(HttpContextBase httpContext, Enum key, OperationButton omi, object queryName, Type entityType)
        //{
        //    if (omi == null || omi.TryCC(om => om.Settings).TryCS(set => set.Post) == true)
        //        return null;

        //    throw new NotImplementedException("ConstructorFromMany operations not supported yet");
        //    //string controllerUrl = "Operation/ConstructFromManyExecute";
        //    //if (omi.Settings.TryCC(set => set.Options).TryCC(opt => opt.ControllerUrl).HasText())
        //    //    controllerUrl = omi.Settings.Options.ControllerUrl;

        //    //return "javascript:ConstructFromManyExecute('{0}','{1}','{2}','{3}',{4},{5});".Formato(
        //    //    controllerUrl,
        //    //    entityType.Name,
        //    //    EnumDN.UniqueKey(key),
        //    //    httpContext.Request.Params["prefix"] ?? "",
        //    //    ((string)httpContext.Request.Params[ViewDataKeys.OnOk]).HasText() ? httpContext.Request.Params[ViewDataKeys.OnOk].Replace("\"","'") : "''",
        //    //    ((string)httpContext.Request.Params[ViewDataKeys.OnCancel]).HasText() ? httpContext.Request.Params[ViewDataKeys.OnCancel].Replace("\"","'") : "''"
        //    //    );
        //}

        internal object ConstructorManager_GeneralConstructor(Type type, Controller controller)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                return null;

            List<OperationInfo> list = OperationLogic.ServiceGetConstructorOperationInfos(type);

            var dic = (from oi in list
                       let omi = Settings.TryGetC(oi.Key)
                       where omi.TryCC(sett => sett.Settings).TryCC(oset => oset as ConstructorSettings).TryCC(eoset => eoset.IsVisible) == null || ((ConstructorSettings)omi.Settings).IsVisible(oi)
                       select new { OperationInfo = oi, OperationMenuItem = omi }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;

            Enum selected = null;
            if (list.Count == 1)
                selected = dic.Keys.Single();
            else
            {
                StringBuilder sb = new StringBuilder();
                string onOk = controller.Request.Params[ViewDataKeys.OnOk].Replace("\"", "'");
                string onCancel = controller.Request.Params[ViewDataKeys.OnCancel].Replace("\"", "'");
                string prefix = controller.Request.Params["prefix"];
                //string onClick = "";
                foreach (OperationInfo oi in list)
                {
                    throw new NotImplementedException("Constructor operations not supported yet");
                    //if (dic[oi.Key].OperationMenuItem.OnServerClickAjax != "")
                    //    onClick = "javascript:CloseChooser('{0}',{1},{2},'{3}');".Formato(dic[oi.Key].OperationSettings.OnServerClickAjax, onOk, onCancel, prefix);
                    //else if (dic[oi.Key].OperationMenuItem.OnServerClickPost != "")
                    //    onClick= "javascript:PostServer('{0}');".Formato(dic[oi.Key].OperationSettings.OnServerClickPost);
                    //sb.AppendLine("<input type='button' value='{0}' onclick=\"{1}\"/><br />".Formato(oi.Key.ToString(), onClick));
                }
                controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
                controller.ViewData[ViewDataKeys.CustomHtml] = sb.ToString();
                return new PartialViewResult
                {
                    ViewName = Navigator.Manager.ChooserPopupUrl,
                    ViewData = controller.ViewData,
                    TempData = controller.TempData
                };
            }

            var pair = dic[selected];

            if (pair.OperationMenuItem != null && ((ConstructorSettings)pair.OperationMenuItem.Settings).Constructor != null)
                return ((ConstructorSettings)pair.OperationMenuItem.Settings).Constructor(pair.OperationInfo, controller.HttpContext);
            else
                return OperationLogic.ServiceConstruct(type, selected);
        }   
    }
}
