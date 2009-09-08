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

        internal static string InternalEntityLineDetail(this HtmlHelper helper, string idValueField, Type type, object value, EntityLineDetail settings)
        {
            if (!settings.Visible)
                return null;

            idValueField = helper.GlobalName(idValueField);

            string defaultDetailDiv = idValueField + "DetailDiv";
            if (!settings.DetailDiv.HasText())
                settings.DetailDiv = defaultDetailDiv;

            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();

            sb.Append("<div class='EntityLineDetail'>\n");

            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, (Reflector.ExtractLazy(type) ?? type).Name));
             
            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Label(idValueField + "lbl", settings.LabelText ?? "", idValueField + "_sfToStr", TypeContext.CssLineLabel));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.RuntimeType, runtimeType));

            string popupOpeningParameters = "'{0}','{1}','{2}','{3}'".Formato("Signum/PartialView", divASustituir, idValueField, settings.DetailDiv);

            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(type);
            bool isLazy = typeof(Lazy).IsAssignableFrom(type);
            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    idValueField + TypeContext.Separator + TypeContext.Id, 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString("")
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id).ToString()) + "\n");

                sb.Append(helper.Div(idValueField + TypeContext.Separator + EntityBaseKeys.Entity, "", "", new Dictionary<string, object>()));
              
                if (settings.Implementations != null) //Interface with several possible implementations
                {
                    sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" style=\"display:none\" >\n");

                    string strButtons = "";
                    foreach(Type t in settings.Implementations)
                    {
                        strButtons += "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(t.Name, Navigator.TypesToURLNames.TryGetC(t) ?? t.Name);
                    }
                    sb.Append(helper.RenderPartialToString(
                        "~/Plugin/Signum.Web.dll/Signum.Web.Views.OKCancelPopup.ascx",
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.CustomHtml, strButtons},
                            { ViewDataKeys.PopupPrefix, idValueField},
                        }
                    ));
                    sb.Append("</div>\n");
                }
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" >\n");

                string url = settings.Url ?? 
                    Navigator.Manager.EntitySettings.TryGetC(type).ThrowIfNullC("No hay una vista asociada al tipo: " + type).PartialViewName;
            
                sb.Append(
                    helper.RenderPartialToString(
                        "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx", 
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.MainControlUrl, url},
                            { ViewDataKeys.PopupPrefix, idValueField}
                        }
                    )
                );
                sb.Append("</div>\n");
            }

            if (settings.Implementations != null || settings.View)
            {
                string viewingUrl = "javascript:OpenDetail(" + popupOpeningParameters + ");";
                sb.Append("<script type=\"text/javascript\">var " + idValueField + "_sfEntityTemp = \"\"</script>\n");
            }

            if (settings.Implementations != null || settings.Create)
                {
                    string creatingUrl = (settings.Implementations == null) ?
                        "NewDetail({0},'{1}'{2});".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(type), (settings.Url.HasText() ? ",'" + settings.Url + "'" : "")) :
                        "$('#{0} :button').each(function(){{".Formato(idValueField + TypeContext.Separator + EntityBaseKeys.Implementations) +
                            "$('#' + this.id).unbind('click').click(function(){" +
                                "OnImplementationsOk({0},'{1}',this.id);".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(type)) +
                            "});" +
                        "});" +
                        "ChooseImplementation('{0}','{1}',function(){{}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField);

                    sb.Append(
                        helper.Button(idValueField + "_btnCreate",
                                  "+",
                                  creatingUrl,
                                  "lineButton create",
                                  (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
                }

            if (settings.Implementations != null || settings.Remove)
                    sb.Append(
                        helper.Button(idValueField + "_btnRemove",
                                  "x",
                                  "RemoveDetailContainedEntity('{0}','{1}');".Formato(idValueField, settings.DetailDiv),
                                  "lineButton remove",
                                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>()));

            if (settings.Implementations != null || (settings.Find && (isIdentifiable || isLazy)))
            {
                string popupFindingParameters = "'{0}','{1}','false',function(){{OnDetailSearchOk('{4}','{2}','{3}','{5}'{6});}},function(){{OnSearchCancel('{2}','{3}');}},'{3}','{2}','{5}'".Formato("Signum/PartialFind", Navigator.TypesToURLNames.TryGetC(Reflector.ExtractLazy(type) ?? type), idValueField, divASustituir, "Signum.aspx/PartialView", settings.DetailDiv, (settings.Url.HasText() ? ",'" + settings.Url + "'" : ""));
                string findingUrl = (settings.Implementations == null) ?
                    "Find({0});".Formato(popupFindingParameters) :
                    "$('#{0} :button').each(function(){{".Formato(idValueField + TypeContext.Separator + EntityBaseKeys.Implementations) +
                        "$('#' + this.id).unbind('click').click(function(){" +
                            "OnSearchImplementationsOk({0},this.id);".Formato(popupFindingParameters) +
                        "});" +
                    "});" +
                    "ChooseImplementation('{0}','{1}',function(){{}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField);
                    
                sb.Append(
                    helper.Button(idValueField + "_btnFind",
                                 "O",
                                 findingUrl,
                                 "lineButton find",
                                 (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
            }

            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            string controlHtml = null;
            if (value != null)
                controlHtml = helper.RenderPartialToString(
                        settings.Url ?? Navigator.Manager.EntitySettings[value.GetType()].PartialViewName,
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.PopupPrefix, idValueField},
                        });

            if (settings.DetailDiv == defaultDetailDiv)
                sb.Append("<div id='{0}' name='{0}'>{1}</div>\n".Formato(settings.DetailDiv, controlHtml ?? ""));
            else if (controlHtml != null)
                sb.Append("<script type=\"text/javascript\" >\n" +
                        "$(document).ready(function() {\n" +
                        "$('#" + settings.DetailDiv + "').html(" + controlHtml + ");\n" +
                        "});\n" +
                        "</script>\n");

            sb.Append("</div>\n"); //Closing tag of <div class='EntityLineDetail'>

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
            if (el != null && el.StyleContext != null)
            {
                using (el.StyleContext)
                    return helper.InternalEntityLineDetail(context.Name, typeof(S), context.Value, el);
            }
            else
                return helper.InternalEntityLineDetail(context.Name, typeof(S), context.Value, el);
        }

    }


}
