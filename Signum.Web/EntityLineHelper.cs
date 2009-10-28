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
using Signum.Engine;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;

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
    }

    public static class EntityLineHelper
    {
        internal static string InternalEntityLine<T>(this HtmlHelper helper, TypeContext<T> typeContext, EntityLine settings)
        {
            if (!settings.Visible)
                return null;

            string idValueField = helper.GlobalName(typeContext.Name);
            Type type = typeContext.ContextType;
            T value = typeContext.Value;
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
            sb.Append(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), (Reflector.ExtractLite(type) ?? type).Name));
             
            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Label(idValueField + "lbl", settings.LabelText ?? "", TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), TypeContext.CssLineLabel));

            string runtimeType = "";
            Type cleanRuntimeType = null;
            if (value != null)
            {
                cleanRuntimeType = value.GetType();
                if (typeof(Lite).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lite).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.Append(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.RuntimeType), runtimeType));

            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && !StyleContext.Current.ReadOnly && (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText()))
                sb.Append("<input type='hidden' id='{0}' name='{0}' value='{1}' />".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), helper.GetChangeTicks(idValueField) ?? 0));
            
            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "function(){{ReloadEntity('{0}','{1}');}}".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());

            string popupOpeningParameters = "'{0}','{1}','{2}',function(){{OnPopupOK('{3}','{2}',{4});}},function(){{OnPopupCancel('{2}');}}".Formato("Signum/PopupView", divASustituir, idValueField, "Signum/ValidatePartial", reloadOnChangeFunction);

            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(type);
            bool isLite = typeof(Lite).IsAssignableFrom(type);
            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.Hidden(
                    TypeContext.Compose(idValueField, TypeContext.Id), 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull).TryToString("")
                       : ((Lite)(object)value).TryCS(i => i.Id).TrySS(id => id).ToString()));

                if ((helper.ViewData.ContainsKey(ViewDataKeys.LoadAll) && value != null) ||
                    (isIdentifiable && value != null && ((IIdentifiable)(object)value).IdOrNull == null))
                {
                    sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Entity)));

                    EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? Reflector.ExtractLite(type) ?? type).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? type));
                    TypeContext tc = typeContext;
                    if (isLite)
                    {
                        //ParameterExpression pe = Expression.Parameter(typeContext.ContextType, "p");
                        //Expression call = Expression.Call(pe, mi.MakeGenericMethod(Reflector.ExtractLite(type)),pe);
                        //LambdaExpression lambda = Expression.Lambda(call, pe);
                        //tc = Common.UntypedTypeContext(typeContext, lambda, cleanRuntimeType ?? Reflector.ExtractLite(type));
                        tc = typeContext.ExtractLite(); 
                    }
                    ViewDataDictionary vdd = new ViewDataDictionary(tc) //value
                    { 
                        { ViewDataKeys.MainControlUrl, es.PartialViewName},
                        //{ ViewDataKeys.PopupPrefix, idValueField}
                    };
                    helper.PropagateSFKeys(vdd);
                    if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                        vdd[ViewDataKeys.Reactive] = true;

                    using (var sc = StyleContext.RegisterCleanStyleContext(true))
                        sb.AppendLine(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));

                    sb.AppendLine("</div>");
                }
                else
                    sb.AppendLine(helper.Div(TypeContext.Compose(idValueField, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));
                
                sb.AppendLine(helper.TextBox(
                    TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), 
                    (isIdentifiable) 
                        ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToStr) 
                        : ((Lite)(object)value).TryCC(i => i.ToStr), 
                    new Dictionary<string, object>() 
                    { 
                        { "class", "valueLine" }, 
                        { "autocomplete", "off" }, 
                        { "style", "display:" + ((value==null) ? "block" : "none")}
                    }));

                if (settings.Autocomplete && Navigator.NameToType.ContainsKey((Reflector.ExtractLite(type) ?? type).Name))
                    sb.AppendLine(helper.AutoCompleteExtender(TypeContext.Compose(idValueField, EntityLineKeys.DDL),
                                                      TypeContext.Compose(idValueField, EntityBaseKeys.ToStr),
                                                      (Reflector.ExtractLite(type) ?? type).Name,
                                                      (settings.Implementations != null) ? settings.Implementations.ToString(t => t.Name, ",") : "",
                                                      TypeContext.Compose(idValueField, TypeContext.Id),
                                                      "Signum/Autocomplete", 1, 5, 500, reloadOnChangeFunction));
                
                if (settings.Implementations != null) //Interface with several possible implementations
                {
                    sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)));

                    string strButtons = "";
                    foreach(Type t in settings.Implementations)
                    {
                        strButtons += "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(t.Name, Navigator.TypesToURLNames.TryGetC(t) ?? t.Name);
                    }
                    sb.Append(helper.RenderPartialToString(
                        Navigator.Manager.OKCancelPopulUrl,
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.CustomHtml, strButtons},
                            //{ ViewDataKeys.PopupPrefix, idValueField},
                        }
                    ));
                    sb.AppendLine("</div>");
                }
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Entity)));

                EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(type).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(type));
                ViewDataDictionary vdd = new ViewDataDictionary(typeContext) //value
                { 
                    { ViewDataKeys.MainControlUrl, es.PartialViewName},
                    //{ ViewDataKeys.PopupPrefix, idValueField}
                };
                helper.PropagateSFKeys(vdd);
                if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                    vdd[ViewDataKeys.Reactive] = true;

                using (var sc = StyleContext.RegisterCleanStyleContext(true))
                    sb.Append(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));
                
                sb.AppendLine("</div>");

                sb.Append(helper.Span(TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), value.ToString(), "valueLine", new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "block" : "none") } }));
            }

            if (settings.View)
            {
                string viewingUrl = "javascript:OpenPopup(" + popupOpeningParameters + ");";
                sb.AppendLine(
                        helper.Href(TypeContext.Compose(idValueField, EntityBaseKeys.ToStrLink),
                            (value != null) ? value.ToString() : "&nbsp;",
                            viewingUrl,
                            "View",
                            "valueLine",
                            new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));
            }
            else
            {
                sb.AppendLine(
                        helper.Span(TypeContext.Compose(idValueField, EntityBaseKeys.ToStrLink),
                            (value != null) ? value.ToString() : "&nbsp;",
                            "valueLine",
                            new Dictionary<string, object> { { "style", "display:" + ((value == null) ? "none" : "block") } }));
            }
            sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(idValueField, EntityBaseKeys.EntityTemp) + " = '';</script>");

            if (settings.Implementations != null || settings.Create)
                {
                    string creatingUrl = (settings.Implementations == null) ?
                        "NewPopup({0},'{1}');".Formato(popupOpeningParameters, (typeof(EmbeddedEntity).IsAssignableFrom(type))) :
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
                                  "RemoveContainedEntity('{0}',{1});".Formato(idValueField, reloadOnChangeFunction),
                                  "lineButton remove",
                                  (value == null) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>()));

            if (settings.Implementations != null || (settings.Find && (isIdentifiable || isLite)))
                {
                    Type cleanType = Reflector.ExtractLite(type) ?? type;
                    string searchType = Navigator.TypesToURLNames.TryGetC(cleanType);
                    string popupFindingParameters = "'{0}','{1}','false',function(){{OnSearchOk('{2}','{3}',{4});}},function(){{OnSearchCancel('{2}','{3}');}},'{3}','{2}'".Formato("Signum/PartialFind", searchType, idValueField, divASustituir, reloadOnChangeFunction);
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

            return sb.ToString();
        }

        public static void EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lite).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lite).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
            }

            EntityLine el = new EntityLine();
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType , false);

            Common.FireCommonTasks(el, typeof(T), context);

            helper.ViewContext.HttpContext.Response.Write(
                SetEntityLineOptions<S>(helper, context, el));
        }

        public static void EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lite).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lite).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }
            else
            {
                runtimeType = Reflector.ExtractLite(runtimeType) ?? runtimeType;
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
            if (el != null)
                using (el)
                    return helper.InternalEntityLine(context, el);
            else
                return helper.InternalEntityLine(context, el);
        }
    }
}
