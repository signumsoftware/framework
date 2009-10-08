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

namespace Signum.Web
{
    public static class EntityRepeaterKeys
    {
        public const string EntitiesContainer = "sfEntitiesContainer";
        public const string RepeaterElement = "sfRepeaterElement";
    }

    public class EntityRepeater : EntityBase
    {
        public Type EntitiesType { get; set; }
        public string RemoveElementLinkText = "Remove";
        public string AddElementLinkText = "New";
        public int? maxElements = null;

        public EntityRepeater() 
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

    public static class EntityRepeaterHelper
    {
        private static void InternalEntityRepeater<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, EntityRepeater settings)
            where T : Modifiable
        {
            if (!settings.Visible)
                return;

            string idValueField = helper.GlobalName(typeContext.Name);
            MList<T> value = typeContext.Value;  

            StringBuilder sb = new StringBuilder();

            Type elementsCleanType = Reflector.ExtractLazy(typeof(T)) ?? typeof(T);

            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), elementsCleanType.Name));

            if (StyleContext.Current.LabelVisible)
                sb.AppendLine(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && !StyleContext.Current.ReadOnly && (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText()))
                sb.AppendLine("<input type='hidden' id='{0}' name='{0}' value='{1}' />".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), helper.GetChangeTicks(idValueField) ?? 0));

            if (settings.Implementations != null) //Interface with several possible implementations
            {
                throw new ApplicationException("Interfaces are not supported by EntityRepeater yet");
            }

            if (settings.Create)
            {
                string creatingUrl = (settings.Implementations == null) ?
                    "javascript:NewRepeaterElement('{0}','{1}','{2}','{3}','{4}','{5}');".Formato("Signum.aspx/PartialView", idValueField, elementsCleanType.Name, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.RemoveElementLinkText, (settings.maxElements.HasValue ? settings.maxElements.Value.ToString() : "")) :
                    ""; //"ChooseImplementation('{0}','{1}',function(){{OnListImplementationsOk({2},'{3}','{4}');}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField, popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.DetailDiv);

                sb.AppendLine(
                    helper.Href(TypeContext.Compose(idValueField, "btnCreate"),
                              settings.AddElementLinkText,
                              creatingUrl,
                              "Nuevo",
                              "lineButton create",
                              new Dictionary<string, object>()));
            }

            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(TypeContext.Compose(idValueField, EntityRepeaterKeys.EntitiesContainer)));
            using (StyleContext sc = new StyleContext() { BreakLine=true, LabelVisible=true, ReadOnly=false, ShowValidationMessage=true})
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        sb.Append(InternalRepeaterElement(helper, idValueField, value[i], i, settings, typeContext));
                    }
                }
            }
            sb.AppendLine("</div>");

            //SEARCH is not supported for EntityRepeater yet
            //if (!typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType))
            //{
            //    string popupFindingParameters = "'{0}','{1}','true',function(){{OnListSearchOk('{2}');}},function(){{OnListSearchCancel('{2}');}},'{3}','{2}'".Formato(routePrefix + "/Signum/PartialFind", Navigator.TypesToURLNames[Reflector.ExtractLazy(typeof(T)) ?? typeof(T)], idValueField, divASustituir);
            //    string findingUrl = (settings.Implementations == null) ?
            //        "Find({0});".Formato(popupFindingParameters) :
            //        "ChooseImplementation('{0}','{1}',function(){{OnSearchImplementationsOk({2});}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField, popupFindingParameters);
            //    if (settings.Find)
            //        sb.Append(
            //            helper.Button(idValueField + "_btnFind",
            //                        "O",
            //                        findingUrl,
            //                        "lineButton",
            //                        new Dictionary<string, object>()));
            //}

            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalRepeaterElement<T>(this HtmlHelper helper, string idValueField, T value, int index, EntityRepeater settings, TypeContext<MList<T>> typeContext)
        {
            StringBuilder sb = new StringBuilder();
            
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLazy = typeof(Lazy).IsAssignableFrom(typeof(T));

            string indexedPrefix = TypeContext.Compose(idValueField, index.ToString());

            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "function(){{ReloadEntity('{0}','{1}');}}".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());

            sb.AppendLine("<div id='{0}' name='{0}' class='repeaterElement'>".Formato(TypeContext.Compose(indexedPrefix, EntityRepeaterKeys.RepeaterElement)));
            
            if (settings.Remove)
                sb.AppendLine(
                    helper.Href(TypeContext.Compose(indexedPrefix, "btnRemove"),
                              settings.RemoveElementLinkText,
                              "javascript:RemoveRepeaterEntity('{0}','{1}',{2});".Formato(TypeContext.Compose(indexedPrefix, EntityRepeaterKeys.RepeaterElement), idValueField, reloadOnChangeFunction),
                              settings.RemoveElementLinkText,
                              "lineButton remove",
                              new Dictionary<string, object>()));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, TypeContext.RuntimeType), runtimeType));

            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityListKeys.Index), index.ToString()));

            if (isIdentifiable || isLazy)
            {
                sb.AppendLine(helper.Hidden(
                    TypeContext.Compose(indexedPrefix, TypeContext.Id),
                    (isIdentifiable)
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull)
                       : ((Lazy)(object)value).TryCS(i => i.Id)));
            }

            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity)));

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(typeof(T)).ThrowIfNullC("No hay una vista asociada al tipo: " + typeof(T));
            TypeElementContext<T> tsc = new TypeElementContext<T>(value, typeContext, index);
            ViewDataDictionary vdd = new ViewDataDictionary(tsc) 
            { 
                { ViewDataKeys.PopupPrefix, indexedPrefix}
            };
            helper.PropagateSFKeys(vdd);

            sb.AppendLine(
                helper.RenderPartialToString(es.PartialViewName, vdd));
            sb.AppendLine("</div>");

            sb.AppendLine("<script type=\"text/javascript\">var " + TypeContext.Compose(indexedPrefix, EntityBaseKeys.EntityTemp) + " = '';</script>");

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static void EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable 
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            Type entitiesType = typeof(T);

            EntityRepeater el = new EntityRepeater() { EntitiesType = entitiesType };
            
            //if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, Reflector.ExtractLazy(typeof(S)) ?? typeof(S), false);

            Common.FireCommonTasks(el, Reflector.ExtractLazy(entitiesType) ?? entitiesType, context);

            helper.InternalEntityRepeater<S>(context, el);
        }

        public static void EntityRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<EntityRepeater> settingsModifier)
            where S : Modifiable
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            Type entitiesType = typeof(T);

            EntityRepeater el = new EntityRepeater() { EntitiesType = entitiesType };

            //if (el.Implementations == null)
            Navigator.ConfigureEntityBase(el, Reflector.ExtractLazy(typeof(S)) ?? typeof(S), false);
            
            Common.FireCommonTasks(el, Reflector.ExtractLazy(entitiesType) ?? entitiesType, context);

            settingsModifier(el);

            if (el != null)
                using(el)
                    helper.InternalEntityRepeater<S>(context, el);
            else
                helper.InternalEntityRepeater<S>(context, el);
        }
    }
}
