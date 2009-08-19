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
        private static void InternalEntityRepeater<T>(this HtmlHelper helper, string idValueField, MList<T> value, EntityRepeater settings)
            where T : Modifiable
        {
            if (!settings.Visible)
                return;

            idValueField = helper.GlobalName(idValueField);

            StringBuilder sb = new StringBuilder();

            Type elementsCleanType = Reflector.ExtractLazy(typeof(T)) ?? typeof(T);
            
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, elementsCleanType.Name) + "\n");

            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

            if (settings.Implementations != null) //Interface with several possible implementations
            {
                throw new ApplicationException("Interfaces are not supported by EntityRepeater yet");
            }

            if (settings.Create)
            {
                string creatingUrl = (settings.Implementations == null) ?
                    "javascript:NewRepeaterElement('{0}','{1}','{2}','{3}','{4}','{5}');".Formato("Signum.aspx/PartialView", idValueField, elementsCleanType.Name, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.RemoveElementLinkText, (settings.maxElements.HasValue ? settings.maxElements.Value.ToString() : "")) :
                    ""; //"ChooseImplementation('{0}','{1}',function(){{OnListImplementationsOk({2},'{3}','{4}');}},function(){{OnImplementationsCancel('{1}');}});".Formato(divASustituir, idValueField, popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType), settings.DetailDiv);

                sb.Append(
                    helper.Href(idValueField + "_btnCreate",
                              settings.AddElementLinkText,
                              creatingUrl,
                              "Nuevo",
                              "lineButton",
                              new Dictionary<string, object>()));
            }

            sb.Append("<div id=\"{0}\" name=\"{0}\">".Formato(idValueField + TypeContext.Separator + EntityRepeaterKeys.EntitiesContainer));
            using (StyleContext sc = new StyleContext() { BreakLine=true, LabelVisible=true, ReadOnly=false, ShowValidationMessage=true})
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        sb.Append(InternalRepeaterElement(helper, idValueField, value[i], i, settings));
                    }
                }
            }
            sb.Append("</div>");

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
                sb.Append("<div class=\"clearall\"></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalRepeaterElement<T>(this HtmlHelper helper, string idValueField, T value, int index, EntityRepeater settings)
        {
            StringBuilder sb = new StringBuilder();
            
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLazy = typeof(Lazy).IsAssignableFrom(typeof(T));

            string indexedPrefix = idValueField + TypeContext.Separator + index.ToString() + TypeContext.Separator;

            sb.Append("<div id=\"{0}\" name=\"{0}\" class=\"repeaterElement\">".Formato(indexedPrefix + EntityRepeaterKeys.RepeaterElement));
            
            if (settings.Remove)
                sb.Append(
                    helper.Href(indexedPrefix + "btnRemove",
                              settings.RemoveElementLinkText,
                              "javascript:RemoveRepeaterEntity('{0}');".Formato(indexedPrefix + EntityRepeaterKeys.RepeaterElement),
                              settings.RemoveElementLinkText,
                              "lineButton",
                              new Dictionary<string, object>()));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.Append(helper.Hidden(indexedPrefix + TypeContext.RuntimeType, runtimeType) + "\n");

            sb.Append(helper.Hidden(indexedPrefix + EntityListKeys.Index, index.ToString()) + "\n");

            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    indexedPrefix + TypeContext.Id,
                    (isIdentifiable)
                       ? ((IIdentifiable)(object)value).TryCS(i => i.IdOrNull)
                       : ((Lazy)(object)value).TryCS(i => i.Id)) + "\n");
            }

            sb.Append("<div id=\"" + indexedPrefix + EntityBaseKeys.Entity + "\" name=\"" + indexedPrefix + EntityBaseKeys.Entity + "\" >\n");

            EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(typeof(T)).ThrowIfNullC("No hay una vista asociada al tipo: " + typeof(T));

            sb.Append(
                helper.RenderPartialToString(
                    es.PartialViewName,
                    new ViewDataDictionary(value) 
                    { 
                        { ViewDataKeys.PopupPrefix, idValueField + TypeContext.Separator + index.ToString()}
                    }
                )
            );
            sb.Append("</div>\n");

            sb.Append("<script type=\"text/javascript\">var " + indexedPrefix + "sfEntityTemp = \"\"</script>\n");

            sb.Append("</div>");

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

            helper.InternalEntityRepeater<S>(context.Name, context.Value, el);
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

            if (el.StyleContext != null)
            {
                using (el.StyleContext)
                    helper.InternalEntityRepeater<S>(context.Name, context.Value, el);
                return;
            }


            helper.InternalEntityRepeater<S>(context.Name, context.Value, el);
        }
    }
}
