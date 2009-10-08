using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.Configuration;

namespace Signum.Web
{
    public class EntityLineDetail : EntityBase
    {
        string detailDiv;
        public string DetailDiv
        {
            get { return detailDiv; }
            set { detailDiv = value; }
        }

        private bool autocomplete = true;
        public bool Autocomplete
        {
            get { return autocomplete; }
            set { autocomplete = value; }
        }

        string url;
        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public EntityLineDetail()
        {
        }

        public override void SetReadOnly()
        {
            Find = false;
            Create = false;
            Remove = false;
            Autocomplete = false;
            Implementations = null;
        }
    }

    public static class EntityLineDetailHelper
    {
        internal static string InternalEntityLineDetail<T>(this HtmlHelper helper, TypeContext<T> typeContext, EntityLineDetail settings)
        {
            if (!settings.Visible)
                return null;

            string idValueField = helper.GlobalName(typeContext.Name);
            Type type = typeContext.ContextType;
            T value = typeContext.Value;
            
            string defaultDetailDiv = idValueField + "DetailDiv";
            if (!settings.DetailDiv.HasText())
                settings.DetailDiv = defaultDetailDiv;

            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div class='EntityLineDetail'>");

            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), (Reflector.ExtractLazy(type) ?? type).Name));
             
            if (StyleContext.Current.LabelVisible)
                sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText ?? "", TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), TypeContext.CssLineLabel));

            string runtimeType = "";
            Type cleanRuntimeType = null;
            if (value != null)
            {
                cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.RuntimeType), runtimeType));

            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && !StyleContext.Current.ReadOnly && (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText()))
                sb.Append("<input type='hidden' id='{0}' name='{0}' value='{1}' />".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), helper.GetChangeTicks(idValueField) ?? 0));

            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "function(){{ReloadEntity('{0}','{1}');}}".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());

            string popupOpeningParameters = "'{0}','{1}','{2}','{3}'".Formato("Signum/PartialView", divASustituir, idValueField, settings.DetailDiv);

            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(type);
            bool isLazy = typeof(Lazy).IsAssignableFrom(type);
            if (isIdentifiable || isLazy)
            {
                sb.AppendLine(helper.Hidden(
                    TypeContext.Compose(idValueField, TypeContext.Id), 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString("")
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id).ToString()) + "\n");

                sb.AppendLine(helper.Div(TypeContext.Compose(idValueField, EntityBaseKeys.Entity), "", "", new Dictionary<string, object>()));
              
                if (settings.Implementations != null) //Interface with several possible implementations
                {
                    sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)));

                    string strButtons = "";
                    foreach(Type t in settings.Implementations)
                    {
                        strButtons += "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(t.Name, Navigator.TypesToURLNames.TryGetC(t) ?? t.Name);
                    }
                    sb.AppendLine(helper.RenderPartialToString(
                        Navigator.Manager.OKCancelPopulUrl,
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.CustomHtml, strButtons},
                            { ViewDataKeys.PopupPrefix, idValueField},
                        }
                    ));
                    sb.AppendLine("</div>");
                }
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.AppendLine("<div id='{0}' name='{0}'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Entity)));

                string url = settings.Url ?? 
                    Navigator.Manager.EntitySettings.TryGetC(type).ThrowIfNullC("No hay una vista asociada al tipo: " + type).PartialViewName;

                using (var sc = StyleContext.RegisterCleanStyleContext(true))
                {
                    ViewDataDictionary vdd = new ViewDataDictionary(typeContext) //value
                    { 
                        //{ ViewDataKeys.MainControlUrl, url},
                        { ViewDataKeys.PopupPrefix, idValueField}
                    };
                    helper.PropagateSFKeys(vdd);
                    sb.Append(helper.RenderPartialToString(url, vdd));
                }
                sb.AppendLine("</div>");
            }

            if (settings.Implementations != null || settings.View)
            {
                //string viewingUrl = "javascript:OpenDetail(" + popupOpeningParameters + ");";
                sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(idValueField, EntityBaseKeys.EntityTemp) + " = '';</script>");
            }

            if (settings.Implementations != null || settings.Create)
                {
                    string creatingUrl = (settings.Implementations == null) ?
                        "NewDetail({0},'{1}'{2});".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(type), (settings.Url.HasText() ? ",'" + settings.Url + "'" : "")) :
                        "$('#{0} :button').each(function(){{".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) +
                            "$('#' + this.id).unbind('click').click(function(){" +
                                "OnImplementationsOk({0},'{1}',this.id);".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(type)) +
                            "});" +
                        "});" +
                        ((settings.Implementations.Count() == 1) ? 
                            "$('#{0} :button').click();".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) :
                            "ChooseImplementation('{0}','{1}',function(){{}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField));

                    sb.AppendLine(
                        helper.Button(TypeContext.Compose(idValueField, "btnCreate"),
                                  "+",
                                  creatingUrl,
                                  "lineButton create",
                                  (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
                }

            if (settings.Implementations != null || settings.Remove)
                    sb.AppendLine(
                        helper.Button(TypeContext.Compose(idValueField, "btnRemove"),
                                  "x",
                                  "RemoveDetailContainedEntity('{0}','{1}',{2});".Formato(idValueField, settings.DetailDiv, reloadOnChangeFunction),
                                  "lineButton remove",
                                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>()));

            if (settings.Implementations != null || (settings.Find && (isIdentifiable || isLazy)))
            {
                string popupFindingParameters = "'{0}','{1}','false',function(){{OnDetailSearchOk('{4}','{2}','{3}',{5},'{6}'{7});}},function(){{OnSearchCancel('{2}','{3}');}},'{3}','{2}','{6}'".Formato("Signum/PartialFind", Navigator.TypesToURLNames.TryGetC(Reflector.ExtractLazy(type) ?? type), idValueField, divASustituir, "Signum.aspx/PartialView", reloadOnChangeFunction, settings.DetailDiv, (settings.Url.HasText() ? ",'" + settings.Url + "'" : ""));
                string findingUrl = (settings.Implementations == null) ?
                    "Find({0});".Formato(popupFindingParameters) :
                    "$('#{0} :button').each(function(){{".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) +
                        "$('#' + this.id).unbind('click').click(function(){" +
                            "OnSearchImplementationsOk({0},this.id);".Formato(popupFindingParameters) +
                        "});" +
                    "});" +
                    ((settings.Implementations.Count() == 1) ? 
                            "$('#{0} :button').click();".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) :
                            "ChooseImplementation('{0}','{1}',function(){{}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField));
                    
                sb.AppendLine(
                    helper.Button(TypeContext.Compose(idValueField, "btnFind"),
                                 "O",
                                 findingUrl,
                                 "lineButton find",
                                 (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
            }

            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            string controlHtml = null;
            if (value != null)
            {
                ViewDataDictionary vdd = new ViewDataDictionary(typeContext) //value
                { 
                    { ViewDataKeys.PopupPrefix, helper.ParentPrefix() }, // idValueField},
                };
                helper.PropagateSFKeys(vdd);
                controlHtml = helper.RenderPartialToString(
                        settings.Url ?? Navigator.Manager.EntitySettings[value.GetType()].PartialViewName,
                        vdd);
            }

            if (settings.DetailDiv == defaultDetailDiv)
                sb.AppendLine("<div id='{0}' name='{0}'>{1}</div>".Formato(settings.DetailDiv, controlHtml ?? ""));
            else if (controlHtml != null)
                sb.AppendLine("<script type=\"text/javascript\">\n" +
                        "$(document).ready(function() {\n" +
                        "$('#" + settings.DetailDiv + "').html(" + controlHtml + ");\n" +
                        "});\n" +
                        "</script>");

            sb.AppendLine("</div>"); //Closing tag of <div class='EntityLineDetail'>

            return sb.ToString();
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            EntityLineDetail el = new EntityLineDetail();
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType , false);

            Common.FireCommonTasks(el, typeof(T), context);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityLineDetailOptions<S>(helper, context, el));
        }

        public static void EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLineDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLazy(runtimeType) ?? runtimeType;
            }

            EntityLineDetail el = new EntityLineDetail();
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType, false);

            Common.FireCommonTasks(el, typeof(T), context);

            settingsModifier(el);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityLineDetailOptions<S>(helper, context, el));
        }

        private static string SetEntityLineDetailOptions<S>(HtmlHelper helper, TypeContext<S> context, EntityLineDetail el)
        {
            if (el != null)
                using (el)
                    return helper.InternalEntityLineDetail(context, el);
            else
                return helper.InternalEntityLineDetail(context, el);
        }
    }
}
