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
    public static class EntityLineKeys
    {
        public const string DDL = "sfDDL";
    }

    public class EntityLine : EntityBase
    {
        private bool autocomplete = true;
        public bool Autocomplete
        {
            get { return autocomplete; }
            set { autocomplete = value; }
        }

        public EntityLine()
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

        bool reloadOnChange = false;
        public bool ReloadOnChange
        {
            get { return reloadOnChange; }
            set { reloadOnChange = value; }
        }
    }

    public static class EntityLineHelper
    {

        internal static string InternalEntityLine(this HtmlHelper helper, string idValueField, Type type, object value, EntityLine settings)
        {
            if (!settings.Visible)
                return null;

            idValueField = helper.GlobalName(idValueField);
            //string divASustituir = helper.ViewData[ViewDataKeys.DivASustituir].TryCC(d => d.ToString()) ?? helper.GlobalName("divASustituir");
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
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

            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "function(){{ReloadEntity('{0}','{1}');}}".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());

            string popupOpeningParameters = "'{0}','{1}','{2}',function(){{OnPopupOK('{3}','{2}',{4});}},function(){{OnPopupCancel('{2}');}}".Formato("Signum/PopupView", divASustituir, idValueField, "Signum/TrySavePartial", reloadOnChangeFunction);

            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(type);
            bool isLazy = typeof(Lazy).IsAssignableFrom(type);
            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    idValueField + TypeContext.Separator + TypeContext.Id, 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString("")
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id).ToString()) + "\n");

                if (helper.ViewData.ContainsKey(ViewDataKeys.LoadAll) && value != null)
                {
                    //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                    sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" style=\"display:none\" >\n");

                    EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(isIdentifiable ? type : Reflector.ExtractLazy(type)).ThrowIfNullC("No hay una vista asociada al tipo: " + type);

                    sb.Append(
                        helper.RenderPartialToString(
                            "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx",
                            new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.MainControlUrl, es.PartialViewName},
                            { ViewDataKeys.PopupPrefix, idValueField}
                        }
                        )
                    );
                    sb.Append("</div>\n");
                }
                else
                    sb.Append(helper.Div(idValueField + TypeContext.Separator + EntityBaseKeys.Entity, "", "", new Dictionary<string, object> { { "style", "display:none" } }));
                
                sb.Append(helper.TextBox(
                    idValueField + TypeContext.Separator + EntityBaseKeys.ToStr, 
                    (isIdentifiable) 
                        ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToStr) 
                        : ((Lazy)(object)value).TryCC(i => i.ToStr), 
                    new Dictionary<string, object>() 
                    { 
                        { "class", "valueLine" }, 
                        { "autocomplete", "off" }, 
                        { "style", "display:" + ((value==null) ? "block" : "none")}
                    }));
                sb.Append("\n");

                if (settings.Autocomplete && Navigator.NameToType.ContainsKey((Reflector.ExtractLazy(type) ?? type).Name))
                    sb.Append(helper.AutoCompleteExtender(idValueField + TypeContext.Separator + EntityLineKeys.DDL,
                                                      idValueField + TypeContext.Separator + EntityBaseKeys.ToStr,
                                                      (Reflector.ExtractLazy(type) ?? type).Name,
                                                      (settings.Implementations != null) ? settings.Implementations.ToString(t => t.Name, ",") : "",
                                                      idValueField + TypeContext.Separator + TypeContext.Id,
                                                      "Signum/Autocomplete", 1, 5, 500, reloadOnChangeFunction));
                
                if (settings.Implementations != null) //Interface with several possible implementations
                {
                    sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" style=\"display:none\" >\n");

                    //List<SelectListItem> types = new List<SelectListItem>{new SelectListItem{Text="Select type",Value="",Selected=true}};
                    string strButtons = "";
                    foreach(Type t in settings.Implementations)
                    {
                        strButtons += "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(t.Name, Navigator.TypesToURLNames.TryGetC(t) ?? t.Name);
                    }
                    //string ddlStr = helper.DropDownList(idValueField + TypeContext.Separator + EntityBaseKeys.ImplementationsDDL, types);
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
                sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" style=\"display:none\" >\n");

                EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(type).ThrowIfNullC("No hay una vista asociada al tipo: " + type);
            
                sb.Append(
                    helper.RenderPartialToString(
                        "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx", 
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.MainControlUrl, es.PartialViewName},
                            { ViewDataKeys.PopupPrefix, idValueField}
                        }
                    )
                );
                sb.Append("</div>\n");

                sb.Append(helper.Span(idValueField + TypeContext.Separator + EntityBaseKeys.ToStr, value.ToString(), "valueLine", new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "block" : "none") } }));
            }

            if (settings.Implementations != null || settings.View)
            {
                string viewingUrl = "javascript:OpenPopup(" + popupOpeningParameters + ");";
                sb.Append(
                        helper.Href(idValueField + TypeContext.Separator + EntityBaseKeys.ToStrLink,
                            (value != null) ? value.ToString() : "&nbsp;",
                            viewingUrl,
                            "View",
                            "valueLine",
                            new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));

                sb.Append("<script type=\"text/javascript\">var " + idValueField + "_sfEntityTemp = \"\"</script>\n");
            }

            if (settings.Implementations != null || settings.Create)
                {
                    string creatingUrl = (settings.Implementations == null) ?
                        "NewPopup({0},'{1}');".Formato(popupOpeningParameters, (typeof(EmbeddedEntity).IsAssignableFrom(type))) :
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
                                  "lineButton",
                                  (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
                }

            if (settings.Implementations != null || settings.Remove)
                    sb.Append(
                        helper.Button(idValueField + "_btnRemove",
                                  "x",
                                  "RemoveContainedEntity('{0}',{1});".Formato(idValueField, reloadOnChangeFunction),
                                  "lineButton",
                                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>()));

            if (settings.Implementations != null || (settings.Find && (isIdentifiable || isLazy)))
                {
                    string popupFindingParameters = "'{0}','{1}','false',function(){{OnSearchOk('{2}','{3}',{4});}},function(){{OnSearchCancel('{2}','{3}');}},'{3}','{2}'".Formato("Signum/PartialFind", Navigator.TypesToURLNames.TryGetC(Reflector.ExtractLazy(type) ?? type), idValueField, divASustituir, reloadOnChangeFunction);
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
                                     "lineButton",
                                     (value == null) ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "style", "display:none" } }));
                }

            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            return sb.ToString();
        }

        public static void EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
            //where S : Modifiable 
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

            EntityLine el = new EntityLine();
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType , false);

            Common.FireCommonTasks(el, typeof(T), context);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityLineOptions<S>(helper, context, el));
        }

        public static void EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
            //where S : Modifiable
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

            EntityLine el = new EntityLine();
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType, false);

            Common.FireCommonTasks(el, typeof(T), context);

            settingsModifier(el);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityLineOptions<S>(helper, context, el));
        }

        private static string SetEntityLineOptions<S>(HtmlHelper helper, TypeContext<S> context, EntityLine el)
        {
            if (el != null && el.StyleContext != null)
            {
                using (el.StyleContext)
                    return helper.InternalEntityLine(context.Name, typeof(S), context.Value, el);
            }
            else
                return helper.InternalEntityLine(context.Name, typeof(S), context.Value, el);
        }

    }


}
