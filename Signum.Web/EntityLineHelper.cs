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
    }

    public class EntityCombo : EntityBase
    {
        public EntityCombo()
        {
            Create = false;
            Remove = false;
            Find = false;
        }
    }

    public static class EntityLineHelper
    {

        private static void InternalEntityLine<T>(this HtmlHelper helper, string idValueField, T value, EntityLine settings)
            where T: Modifiable
        {
            idValueField = helper.GlobalName(idValueField);
            string divASustituir = helper.GlobalName("divASustituir");

            StringBuilder sb = new StringBuilder();
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.StaticType, (Reflector.ExtractLazy(typeof(T)) ?? typeof(T)).Name));

            if (StyleContext.Current.LabelVisible)
                sb.Append(helper.Span(idValueField + "lbl", settings.LabelText ?? "", TypeContext.CssLineLabel));

            string runtimeType = "";
            if (value != null)
            {
                Type cleanRuntimeType = value.GetType();
                if (typeof(Lazy).IsAssignableFrom(value.GetType()))
                    cleanRuntimeType = (value as Lazy).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }
            sb.Append(helper.Hidden(idValueField + TypeContext.Separator + TypeContext.RuntimeType, runtimeType));
                
            string popupOpeningParameters = "'/Signum/PartialView','{0}','{1}',function(){{OnPopupOK('/Signum/TrySavePartial','{1}');}},function(){{OnPopupCancel('{1}');}}".Formato(divASustituir, idValueField);

            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLazy = typeof(Lazy).IsAssignableFrom(typeof(T));
            if (isIdentifiable || isLazy)
            {
                sb.Append(helper.Hidden(
                    idValueField + TypeContext.Separator + TypeContext.Id, 
                    (isIdentifiable) 
                       ? ((IIdentifiable)(object)value).TryCS(i => i.Id).TrySS(id => id)
                       : ((Lazy)(object)value).TryCS(i => i.Id).TrySS(id => id)) + "\n");

                sb.Append(helper.Div(idValueField + TypeContext.Separator + EntityBaseKeys.Entity, "", "", new Dictionary<string, string> { { "style", "display:none" } }));
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

                if(settings.Autocomplete)
                    sb.Append(helper.AutoCompleteExtender(idValueField + TypeContext.Separator + EntityLineKeys.DDL,
                                                      idValueField + TypeContext.Separator + EntityBaseKeys.ToStr,
                                                      (!string.IsNullOrEmpty(runtimeType)) ? runtimeType : (Reflector.ExtractLazy(typeof(T)) ?? typeof(T)).Name, 
                                                      (settings.Implementations != null) ? settings.Implementations.ToString(t => t.Name,",") : "",
                                                      idValueField + TypeContext.Separator +  TypeContext.Id,
                                                      "/Signum/Autocomplete", 1, 5, 500));
                
                if (settings.Implementations != null) //Interface with several possible implementations
                {
                    sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Implementations + "\" style=\"display:none\" >\n");

                    List<SelectListItem> types = new List<SelectListItem>{new SelectListItem{Text="Select type",Value="",Selected=true}};
                    foreach(Type t in settings.Implementations)
                    {
                        types.Add(new SelectListItem{Text=t.Name,Value=t.Name});
                    }
                    string ddlStr = helper.DropDownList(idValueField + TypeContext.Separator + EntityBaseKeys.ImplementationsDDL, types);
                    sb.Append(helper.RenderPartialToString(
                        "~/Plugin/Signum.Web.dll/Signum.Web.Views.OKCancelPopup.ascx",
                        new ViewDataDictionary(value) 
                        { 
                            { ViewDataKeys.CustomHtml, ddlStr},
                            { ViewDataKeys.PopupPrefix, idValueField},
                            { ViewDataKeys.OnOk, "OnImplementationsOk({0},'{1}');".Formato(popupOpeningParameters, typeof(EmbeddedEntity).IsAssignableFrom(typeof(T))) },
                            { ViewDataKeys.OnCancel, "OnImplementationsCancel('" + idValueField + "');"}
                        }
                    ));
                    sb.Append("</div>\n");
                }
            }
            else
            {
                //It's an embedded entity: Render popupcontrol with embedded entity to the _sfEntity hidden div
                sb.Append("<div id=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" name=\"" + idValueField + TypeContext.Separator + EntityBaseKeys.Entity + "\" style=\"display:none\" >\n");

                EntitySettings es = Navigator.NavigationManager.EntitySettings.TryGetC(typeof(T)).ThrowIfNullC("No hay una vista asociada al tipo: " + typeof(T));
            
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

                sb.Append(helper.Span(idValueField + TypeContext.Separator + EntityBaseKeys.ToStr, value.ToString(), "valueLine", new Dictionary<string, string> { { "style", "display:" + ((value == null) ? "block" : "none") } }));
            }

            string viewingUrl = "javascript:OpenPopup(" + popupOpeningParameters +");";
            sb.Append(
                    helper.Href(idValueField + TypeContext.Separator + EntityBaseKeys.ToStrLink,
                        (value!=null) ? value.ToString() : "&nbsp;",
                        viewingUrl,
                        "View",
                        "valueLine",
                        new Dictionary<string, string> { {"style","display:" + ((value==null) ? "none" : "block")}}));

            sb.Append("<script type=\"text/javascript\">var " + idValueField + "_sfEntityTemp = \"\"</script>\n");
            
            string creatingUrl = (settings.Implementations == null) ?
                "NewPopup({0},'{1}');".Formato(popupOpeningParameters, (typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))) : 
                "ChooseImplementation('{0}','{1}');".Formato(divASustituir, idValueField);
            if (settings.Create)
                sb.Append(
                    helper.Button(idValueField + "_btnCreate",
                              "+",
                              creatingUrl,
                              "lineButton",
                              (value == null) ? new Dictionary<string, string>() : new Dictionary<string, string>() { { "style", "display:none" } }));

            if (settings.Remove)
                sb.Append(
                    helper.Button(idValueField + "_btnRemove",
                              "x",
                              "RemoveContainedEntity('" + idValueField + "');",
                              "lineButton",
                              (value == null) ? new Dictionary<string, string>() { { "style", "display:none" } } : new Dictionary<string, string>()));

            if (settings.Find)
                sb.Append(
                    helper.Button(idValueField + "_btnFind",
                                "O",
                                "Find('/Signum/PartialFind','{0}','false',function(){{OnSearchOk('{1}');}},function(){{OnSearchCancel('{1}');}},'{2}','{1}');".Formato(Navigator.TypesToURLNames[typeof(T)], idValueField, divASustituir),
                                "lineButton",
                                (value == null) ? new Dictionary<string, string>() : new Dictionary<string, string>() { { "style", "display:none" } }));

            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        public static void EntityLine<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
            where S : Modifiable 
        {
            TypeContext<S> context = Common.WalkExpressionGen(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }

            EntityLine el = new EntityLine();
            Common.FireCommonTasks(el, typeof(T), context);

            if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType , false);

            helper.InternalEntityLine<S>(context.Name, context.Value, el);
        }

        public static void EntityLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLine> settingsModifier)
            where S : Modifiable
        {
            TypeContext<S> context = Common.WalkExpressionGen(tc, property);

            Type runtimeType = typeof(S);
            if (context.Value != null)
            {
                if (typeof(Lazy).IsAssignableFrom(context.Value.GetType()))
                    runtimeType = (context.Value as Lazy).RuntimeType;
                else
                    runtimeType = context.Value.GetType();
            }

            EntityLine el = new EntityLine();
            Common.FireCommonTasks(el, typeof(T), context);

            if (el.Implementations == null)
                Navigator.ConfigureEntityBase(el, runtimeType, false);

            settingsModifier(el);

            helper.InternalEntityLine<S>(context.Name, context.Value, el);
        }

    }


}
