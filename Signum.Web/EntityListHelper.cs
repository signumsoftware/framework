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

namespace Signum.Web
{
    public class EntityList : EntityBase
    {
        public Type EntitiesType { get; set; }
        
        public EntityList()
        {
        }
    }

    public static class EntityListHelper
    {
        private static void InternalEntityList<T>(this HtmlHelper helper, string idValueField, MList<T> value, EntityList settings)
            where T : Modifiable
        {
            idValueField = helper.GlobalName(idValueField);
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();

            Type elementsCleanType = Reflector.ExtractLazy(typeof(T)) ?? typeof(T);
            
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, elementsCleanType.Name) + "\n");

            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

            string popupOpeningParameters = "'/Signum/PartialView','{0}','{1}',function(){{OnListPopupOK('/Signum/TrySavePartial','{1}',this.id);}},function(){{OnListPopupCancel(this.id);}}".Formato(divASustituir, idValueField);

            if (settings.Implementations != null) //Interface with several possible implementations
            {
                sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" style=\"display:none\" >\n");

                List<SelectListItem> types = new List<SelectListItem> { new SelectListItem { Text = "Select type", Value = "", Selected = true } };
                foreach (Type t in settings.Implementations)
                {
                    types.Add(new SelectListItem { Text = t.Name, Value = t.Name });
                }
                string ddlStr = helper.DropDownList(idValueField + TypeContext.Separator + EntityBaseKeys.ImplementationsDDL, types);
                sb.Append(helper.RenderPartialToString(
                    "~/Plugin/Signum.Web.dll/Signum.Web.Views.OKCancelPopup.ascx",
                    new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.CustomHtml, ddlStr},
                            { ViewDataKeys.PopupPrefix, idValueField},
                            { ViewDataKeys.OnOk, "OnListImplementationsOk({0},'{1}');".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType)) },
                            { ViewDataKeys.OnCancel, "OnImplementationsCancel('" + idValueField + "');"}
                        }
                ));
                sb.Append("</div>\n");
            }

            string viewingUrl = "OpenPopupList(" + popupOpeningParameters + ");";
            StringBuilder sbSelect = new StringBuilder();
            sbSelect.Append("<select id=\"{0}\" name=\"{0}\" multiple=\"multiple\" ondblclick=\"{1}\" >\n".Formato(idValueField, viewingUrl));

            for (int i = 0; i < value.Count; i++)
            {
                sb.Append(InternalListElement(helper, sbSelect, idValueField, value[i], i, settings, divASustituir));
            }

            sbSelect.Append("</select>\n");

            sb.Append(sbSelect);

            string creatingUrl = (settings.Implementations == null) ?
                "NewPopupList({0},'{1}');".Formato(popupOpeningParameters, (typeof(EmbeddedEntity).IsAssignableFrom(elementsCleanType))) : 
                "ChooseImplementation('{0}','{1}');".Formato(divASustituir, idValueField);
            if (settings.Create)
                sb.Append(
                    helper.Button(idValueField + "_btnCreate",
                              "+",
                              creatingUrl,
                              "lineButton",
                              new Dictionary<string, string>()));

            if (settings.Remove)
                sb.Append(
                    helper.Button(idValueField + "_btnRemove",
                              "x",
                              "RemoveListContainedEntity('{0}');".Formato(idValueField),
                              "lineButton",
                              new Dictionary<string, string>()));

            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalListElement<T>(this HtmlHelper helper, StringBuilder sbOptions, string idValueField, T value, int index, EntityList settings, string divASustituir)
        {
            StringBuilder sb = new StringBuilder();

            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLazy = typeof(Lazy).IsAssignableFrom(typeof(T));

            string indexedSeparator = TypeContext.Separator + index.ToString() + TypeContext.Separator;
            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    idValueField + indexedSeparator + TypeContext.Id,
                    (isIdentifiable)
                       ? ((IIdentifiable)(object)value).TryCS(i => i.Id)
                       : ((Lazy)(object)value).TryCS(i => i.Id)) + "\n");

                sb.Append(helper.Div(idValueField + indexedSeparator + EntityBaseKeys.Entity, "", "", new Dictionary<string, string> { { "style", "display:none" } }));
                
                //Note this is added to the sbOptions, not to the result sb
                sbOptions.Append("<option id=\"" + idValueField + indexedSeparator + EntityBaseKeys.ToStr + "\" " +
                                "name=\"" + idValueField + indexedSeparator + EntityBaseKeys.ToStr + "\" " + 
                                "value=\"\" " +
                                "class = valueLine\" " +
                                ">" + 
                                ((isIdentifiable)
                                    ? ((IdentifiableEntity)(object)value).TryCC(i => i.ToStr)
                                    : ((Lazy)(object)value).TryCC(i => i.ToStr)) + 
                                "</option>\n");

                string runtimeType = "";
                if (value != null)
                {
                    Type cleanRuntimeType = value.GetType();
                    if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                        cleanRuntimeType = (value as Lazy).RuntimeType;
                    runtimeType = cleanRuntimeType.Name;
                }
                sb.Append(helper.Hidden(idValueField + indexedSeparator + TypeContext.RuntimeType, runtimeType) + "\n");
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.Append("<div id=\"" + idValueField + indexedSeparator + EntityBaseKeys.Entity + "\" name=\"" + idValueField + indexedSeparator + EntityBaseKeys.Entity + "\" style=\"display:none\" >\n");

                EntitySettings es = Navigator.NavigationManager.Settings.TryGetC(typeof(T)).ThrowIfNullC("No hay una vista asociada al tipo: " + typeof(T));

                sb.Append(
                    helper.RenderPartialToString(
                        "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx",
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.MainControlUrl, es.PartialViewName},
                            { ViewDataKeys.PopupPrefix, idValueField + TypeContext.Separator + index.ToString()}
                        }
                    )
                );
                sb.Append("</div>\n");

                //Note this is added to the sbOptions, not to the result sb
                sbOptions.Append("<option id=\"" + idValueField + indexedSeparator + EntityBaseKeys.ToStr + "\" " +
                                "name=\"" + idValueField + indexedSeparator + EntityBaseKeys.ToStr + "\" " +
                                "value=\"\" " +
                                "class = valueLine\" " +
                                ">" +
                                ((EmbeddedEntity)(object)value).TryCC(i => i.ToString()) + 
                                "</option>\n");
            }

            sb.Append("<script type=\"text/javascript\">var " + idValueField + indexedSeparator + "_sfEntityTemp = \"\"</script>\n");

            return sb.ToString();
        }

        public static void EntityList<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : Modifiable 
        {
            TypeContext<MList<S>> context = Common.WalkExpressionGen(tc, property);

            Type entitiesType = typeof(T);

            EntityList el = new EntityList() { EntitiesType = entitiesType };
            Common.FireCommonTasks(el, Reflector.ExtractLazy(entitiesType) ?? entitiesType, context);

            //if (el.Implementations == null)
            //    Navigator.ConfigureEntityBase(el, runtimeType, false);

            helper.InternalEntityList<S>(context.Name, context.Value, el);
        }
    }
}
