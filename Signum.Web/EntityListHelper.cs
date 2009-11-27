using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using Signum.Web.Properties;

namespace Signum.Web
{
    public static class EntityListKeys
    {
        public const string Index = "sfIndex";
    }

    public class EntityList : EntityBase
    {
        public Type EntitiesType { get; set; }
        public string DetailDiv = null;

        public EntityList()
        {
        }

        public override void SetReadOnly()
        {
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }
    }

    public static class EntityListHelper
    {
        private static void InternalEntityList<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, EntityList settings)
        {
            if (!settings.Visible)
                return;
            
            string idValueField = helper.GlobalName(typeContext.Name);
            MList<T> value = typeContext.Value;         
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
            
            Type elementsCleanType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);
            
            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), elementsCleanType.Name));

            if (StyleContext.Current.LabelVisible)
                sb.AppendLine(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

            if (settings.ShowFieldDiv)
                sb.AppendLine("<div class='fieldList'>");

            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && !StyleContext.Current.ReadOnly && (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText()))
                sb.AppendLine("<input type='hidden' id='{0}' name='{0}' value='{1}' />".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), helper.GetChangeTicks(idValueField) ?? 0));
            
            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "function(){{ReloadEntity('{0}','{1}');}}".Formato("Signum/ReloadEntity", helper.ParentPrefix());
            
            string popupOpeningParameters = "'{0}','{1}','{2}',function(){{OnListPopupOK('{3}','{2}',this.id,{4});}},function(){{OnListPopupCancel(this.id);}}".Formato(settings.DetailDiv.HasText() ? "Signum/PartialView": "Signum/PopupView", divASustituir, idValueField, "Signum/ValidatePartial", reloadOnChangeFunction);

            if (settings.Implementations != null) //Interface with several possible implementations
            {
                sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)));

                string strButtons = "";
                foreach (Type t in settings.Implementations)
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

            string viewingUrl = "OpenPopupList(" + popupOpeningParameters + ",'{0}');".Formato(settings.DetailDiv);
            StringBuilder sbSelect = new StringBuilder();
            sbSelect.AppendLine("<select id='{0}' name='{0}' multiple='multiple' ondblclick=\"{1}\" class='entityList'>".Formato(idValueField, viewingUrl));

            if (value != null)
            {
                for (int i = 0; i < value.Count; i++)
                {
                    sb.Append(InternalListElement(helper, sbSelect, idValueField, value[i], i, settings, typeContext));
                }
            }

            sbSelect.AppendLine("</select>");

            sb.Append(sbSelect);

            StringBuilder sbBtns = new StringBuilder();

            if (settings.Create)
            {
                string creatingUrl = (settings.Implementations == null) ?
                    "NewPopupList({0},'{1}','{2}','{3}');".Formato(popupOpeningParameters, elementsCleanType.Name, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.DetailDiv) :
                    "$('#{0} :button').each(function(){{".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) +
                            "$('#' + this.id).unbind('click').click(function(){" +
                                "OnListImplementationsOk({0},'{1}','{2}',this.id);".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.DetailDiv) +
                            "});" +
                        "});" +
                        ((settings.Implementations.Count() == 1) ? 
                            "$('#{0} :button').click();".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Implementations)) :
                            "ChooseImplementation('{0}','{1}',function(){{}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField));

                sbBtns.AppendLine("<tr><td>");
                sbBtns.AppendLine(
                        helper.Button(TypeContext.Compose(idValueField, "btnCreate"),
                                  "+",
                                  creatingUrl,
                                  "lineButton create",
                                  new Dictionary<string, object>()));
                sbBtns.AppendLine("</td></tr>");
            }

            if (settings.Find && !typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType))
            {
                    string popupFindingParameters = "'{0}','{1}','true',function(){{OnListSearchOk('{2}','{3}');}},function(){{OnListSearchCancel('{2}','{3}');}},'{3}','{2}'".Formato("Signum/PartialFind", Navigator.TypesToURLNames.TryGetC(Reflector.ExtractLite(typeof(T)) ?? typeof(T)), idValueField, divASustituir);
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

                    sbBtns.AppendLine("<tr><td>");
                    sbBtns.AppendLine(
                            helper.Button(TypeContext.Compose(idValueField, "btnFind"),
                                        "O",
                                        findingUrl,
                                        "lineButton find",
                                        new Dictionary<string, object>()));
                    sbBtns.AppendLine("</td></tr>");
            }
            
            if (settings.Remove)
            {
                sbBtns.AppendLine("<tr><td>");
                sbBtns.AppendLine(
                        helper.Button(TypeContext.Compose(idValueField, "btnRemove"),
                                  "x",
                                  "RemoveListContainedEntity('{0}');".Formato(idValueField),
                                  "lineButton remove",
                                  (value == null || value.Count == 0) ? new Dictionary<string, object>() { { "style", "display:none" } } : new Dictionary<string, object>()));
                sbBtns.AppendLine("</td></tr>");
            }
            
            string sBtns = sbBtns.ToString();
            if (sBtns.HasText())
                sb.AppendLine("<table>\n" + sBtns + "</table>");

            if (settings.ShowFieldDiv)
                sb.Append("</div>");
            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalListElement<T>(this HtmlHelper helper, StringBuilder sbOptions, string idValueField, T value, int index, EntityList settings, TypeContext<MList<T>> typeContext)
        {
            StringBuilder sb = new StringBuilder();
            
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));

            string indexedPrefix = TypeContext.Compose(idValueField, index.ToString());

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lite).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lite).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, TypeContext.RuntimeType), runtimeType));
            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityListKeys.Index), index.ToString()));

            if (isIdentifiable || isLite)
            {
                sb.AppendLine(helper.Hidden(
                    TypeContext.Compose(indexedPrefix, TypeContext.Id),
                    (isIdentifiable)
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull)
                       : ((Lite)(object)value).TryCS(i => i.IdOrNull)));

                if (value != null && ((isIdentifiable && ((IIdentifiable)value).IsNew) || (isLite && ((Lite)(object)value).IdOrNull == null)))
                    helper.Write(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityBaseKeys.IsNew), ""));

                if ((helper.ViewData.ContainsKey(ViewDataKeys.LoadAll) && value != null) ||
                    (isIdentifiable && value != null && ((IIdentifiable)(object)value).IdOrNull == null))
                {
                    sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityBaseKeys.IsNew), index.ToString()));

                    //It's a new object, I preload it because it won't be possible to retrieve it from the server
                    sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity)));
                    EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(typeof(T)).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(typeof(T)));
                    TypeElementContext<T> tsc = new TypeElementContext<T>(value, typeContext, index);
                    ViewDataDictionary vdd = new ViewDataDictionary(tsc)
                    { 
                        { ViewDataKeys.MainControlUrl, es.PartialViewName},
                        { ViewDataKeys.PopupPrefix, indexedPrefix}
                    };
                    helper.PropagateSFKeys(vdd);
                    if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                        vdd[ViewDataKeys.Reactive] = true;

                    using (var sc = StyleContext.RegisterCleanStyleContext(true))
                        sb.Append(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));
                    sb.AppendLine("</div>");
                }
                else
                {
                    sb.Append(helper.Div(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));
                }

                    //Note this is added to the sbOptions, not to the result sb
                    sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr)) +
                                    ((isIdentifiable)
                                        ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToString())
                                        : ((Lite)(object)value).TryCC(i => i.ToStr)) +
                                    "</option>");
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity)));

                EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(typeof(T)).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(typeof(T)));

                TypeElementContext<T> tsc = new TypeElementContext<T>(value, typeContext, index);
                ViewDataDictionary vdd = new ViewDataDictionary(tsc)  //value instead of tsc
                { 
                    { ViewDataKeys.MainControlUrl, es.PartialViewName},
                    { ViewDataKeys.PopupPrefix, indexedPrefix}
                };
                helper.PropagateSFKeys(vdd);
                if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                    vdd[ViewDataKeys.Reactive] = true;

                using (var sc = StyleContext.RegisterCleanStyleContext(true))
                    sb.Append(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));

                sb.AppendLine("</div>");

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.AppendLine("<option id='{0}' name='{0}' value='' class='valueLine entityListOption'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.ToStr)) +
                                ((EmbeddedEntity)(object)value).TryCC(i => i.ToString()) + 
                                "</option>");
            }

            sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(indexedPrefix, EntityBaseKeys.EntityTemp) + " = '';</script>");

            return sb.ToString();
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            Type entitiesType = typeof(T);

            EntityList el = new EntityList() { EntitiesType = entitiesType };
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);

            Common.FireCommonTasks(el, Reflector.ExtractLite(entitiesType) ?? entitiesType, context);

            CallInternalEntityList<S>(helper, context, el);
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityList> settingsModifier)
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            Type entitiesType = typeof(T);

            EntityList el = new EntityList() { EntitiesType = entitiesType };
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);

            Common.FireCommonTasks(el, Reflector.ExtractLite(entitiesType) ?? entitiesType, context);

            settingsModifier(el);

            CallInternalEntityList<S>(helper, context, el);
            //if (el != null)
            //    using (el)
            //        helper.InternalEntityList<S>(context, el);
            //else
            //    helper.InternalEntityList<S>(context, el);
        }

        private static void CallInternalEntityList<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, EntityList settings)
        {
            if (settings != null)
                using (settings)
                    helper.InternalEntityList<T>(typeContext, settings);
            else
                helper.InternalEntityList<T>(typeContext, settings);
        }
    }
}
