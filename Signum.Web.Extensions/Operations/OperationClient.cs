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

            //SearchControl.GetCustomMenuItems += (qn, type) =>
            //{
            //    var list = OperationLogic.ServiceGetConstructorOperationInfosnfos(type).Where(oi =>
            //    {
            //        ConstructorFromManySettings set = (ConstructorFromManySettings)Manager.Settings.TryGetC(oi.Key);
            //        return set == null || set.IsVisible == null || set.IsVisible(qn, oi);
            //    }).ToList();

            //    if (list.Count == 0)
            //        return null;

            //    return new ConstructFromMenuItem { OperationInfos = list };
            //};
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, WebMenuItem> Settings = new Dictionary<Enum, WebMenuItem>();

        internal List<WebMenuItem> ButtonBar_GetButtonBarElement(HttpContextBase httpContext, object entity, string mainControlUrl)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var dic = (from oi in list
                       let os = (EntityOperationSettings)Settings.TryGetC(oi.Key)
                       where os == null || os.IsVisible == null || os.IsVisible(ident)
                       select new { OperationInfo = oi, OperationSettings = os }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;
            List<WebMenuItem> items = new List<WebMenuItem>();

            foreach (OperationInfo oi in list)
            {
                if (dic.ContainsKey(oi.Key))
                {
                    WebMenuItem item = new WebMenuItem
                    {
                        AltText = GetText(oi.Key, dic[oi.Key].OperationSettings),
                        Id = dic[oi.Key].OperationSettings.TryCC(os => os.Id),
                        ImgSrc = GetImage(oi.Key, dic[oi.Key].OperationSettings),
                        OnClick = dic[oi.Key].OperationSettings.TryCC(os => os.OnClick),
                        OnServerClickAjax = GetServerClickAjax(httpContext, oi.Key, dic[oi.Key].OperationSettings, ident),
                        OnServerClickPost = dic[oi.Key].OperationSettings.TryCC(os => os.OnServerClickPost)
                    };
                    item.HtmlProps.AddRange(dic[oi.Key].OperationSettings.TryCC(os => os.HtmlProps));

                    items.Add(item);
                }
            }
            
            return items;
        }

        protected internal virtual string GetText(Enum key, EntityOperationSettings os)
        {
            if (os != null && os.AltText != null)
                return os.AltText;

            return EnumExtensions.NiceToString(key);
        }

        protected internal virtual string GetImage(Enum key, EntityOperationSettings os)
        {
            if (os != null && os.ImgSrc != null)
                return os.ImgSrc;

            return null;
        }

        protected internal virtual string GetServerClickAjax(HttpContextBase httpContext, Enum key, EntityOperationSettings os, IdentifiableEntity ident)
        {
            string controllerUrl = "Operation.aspx/OperationExecute";
            if (os != null && os.OnServerClickAjax.HasText())
                controllerUrl = os.OnServerClickAjax;

            return "javascript:OperationExecute('{0}','{1}','{2}','{3}','{4}',{5},{6});".Formato(
                controllerUrl,
                ident.GetType().Name, 
                ident.IdOrNull.HasValue ? ident.IdOrNull.Value.ToString() : "",
                EnumDN.UniqueKey(key),
                httpContext.Request.Params["prefix"] ?? "",
                httpContext.Request.Params[ViewDataKeys.OnOk] ?? "",
                httpContext.Request.Params[ViewDataKeys.OnCancel] ?? ""
                );
        }

        internal object ConstructorManager_GeneralConstructor(Type type, Controller controller)
        {
            List<OperationInfo> list = OperationLogic.ServiceGetConstructorOperationInfos(type);

            var dic = (from oi in list
                       let os = (ConstructorSettings)Settings.TryGetC(oi.Key)
                       where os == null || os.IsVisible == null || os.IsVisible(oi)
                       select new { OperationInfo = oi, OperationSettings = os }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;

            Enum selected = null;
            if (list.Count == 1)
            {
                selected = dic.Keys.Single();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                string onOk = controller.Request.Params[ViewDataKeys.OnOk];
                string onCancel = controller.Request.Params[ViewDataKeys.OnCancel];
                string prefix = controller.Request.Params["prefix"];
                string onClick = "";
                foreach (OperationInfo oi in list)
                {
                    if (dic[oi.Key].OperationSettings.OnServerClickAjax != "")
                        onClick = "javascript:CallServer('{0}',{1},{2},'{3}');".Formato(dic[oi.Key].OperationSettings.OnServerClickAjax, onOk, onCancel, prefix);
                    else if (dic[oi.Key].OperationSettings.OnServerClickPost != "")
                        onClick= "javascript:PostServer('{0}',{1},{2},'{3}');".Formato(dic[oi.Key].OperationSettings.OnServerClickPost, onOk, onCancel, prefix);
                    sb.AppendLine("<input type='button' value='{0}' onclick=\"{1}\"/><br />".Formato(oi.Key.ToString(), onClick));
                }
                controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
                controller.ViewData[ViewDataKeys.CustomHtml] = sb.ToString();
                return new PartialViewResult
                {
                    ViewName = Navigator.Manager.OKCancelPopulUrl,
                    ViewData = controller.ViewData,
                    TempData = controller.TempData
                };
                //throw new ApplicationException("Not implemented: Todavía no se permiten varios constructores");
                //ConstructorSelectorWindow sel = new ConstructorSelectorWindow();
                //sel.ConstructorKeys = dic.Keys.ToArray();
                //if (sel.ShowDialog() != true)
                //    return null;

                //selected = sel.SelectedKey;
            }

            var pair = dic[selected];

            if (pair.OperationSettings != null && pair.OperationSettings.Constructor != null)
                return pair.OperationSettings.Constructor(pair.OperationInfo, controller);
            else
                return Constructor.Construct(type, controller);
        }   
    }
}
