using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities.Files;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Web.Extensions.Properties;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Engine.Basics;

namespace Signum.Web.Files
{
    public static class FileLineKeys
    {
        public const string FileType = "FileType";
    }

    public class FileLine : BaseLine
    {
        public readonly Dictionary<string, object> ValueHtmlProps = new Dictionary<string, object>(0);

        Enum fileType;
        public Enum FileType
        {
            get { return fileType; }
            set { fileType = value; }
        }

        bool view = true;
        public bool View
        {
            get { return view; }
            set { view = value; }
        }

        bool find = true;
        public bool Find
        {
            get { return find; }
            set { find = value; }
        }

        bool remove = true;
        public bool Remove
        {
            get { return remove; }
            set { remove = value; }
        }

        string finding = "";
        public string Finding
        {
            get { return finding; }
            set { finding = value; }
        }

        string viewing = "";
        public string Viewing
        {
            get { return viewing; }
            set { viewing = value; }
        }

        string removing = "";
        public string Removing
        {
            get { return removing; }
            set { removing = value; }
        }

        public override void SetReadOnly()
        {
            Find = false;
            Remove = false;
        }
    }

    public static class FileLineHelper
    {
        internal static string InternalFileLine<S>(this HtmlHelper helper, TypeContext<S> typeContext, FileLine settings)
            where S : FilePathDN
        {
            if (!settings.Visible)
                return null;

            if (settings.FileType == null)
                throw new ArgumentException(Resources.FileTypePropertyOfFileLineSettingsMustBeSpecified.Formato(typeContext.Name));

            string idValueField = helper.GlobalName(typeContext.Name);
            FilePathDN value = typeContext.Value;
            Type type = typeof(FilePathDN);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.StaticType), type.Name));

            if (StyleContext.Current.LabelVisible)
                sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText ?? "", TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), TypeContext.CssLineLabel));

            string runtimeType = "";
            Type cleanRuntimeType = null;
            if (value != null)
            {
                cleanRuntimeType = value.GetType();
                if (typeof(Lite).IsAssignableFrom(value.GetType()))
                    throw new ApplicationException("FileLine doesn't support Lazies");
                    //cleanRuntimeType = (value as Lite).RuntimeType;
                runtimeType = cleanRuntimeType.Name;
            }

            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, FileLineKeys.FileType),
                EnumDN.UniqueKey((value != null) ? 
                    value.FileTypeEnum ?? EnumLogic<FileTypeDN>.ToEnum(value.FileType) : 
                    settings.FileType)));

            sb.AppendLine(helper.Hidden(TypeContext.Compose(idValueField, TypeContext.RuntimeType), runtimeType));

            if ((StyleContext.Current.ShowTicks == null || StyleContext.Current.ShowTicks.Value) && !StyleContext.Current.ReadOnly && (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText()))
                sb.AppendLine("<input type='hidden' id='{0}' name='{0}' value='{1}' />".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), helper.GetChangeTicks(idValueField) ?? 0));

            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                throw new ApplicationException("FileLine doesn't support ReloadOnChange functionality");

            sb.AppendLine(helper.Hidden(
                TypeContext.Compose(idValueField, TypeContext.Id),
                value.TryCS(i => i.IdOrNull).TryToString("")));

            if ((helper.ViewData.ContainsKey(ViewDataKeys.LoadAll) && value != null) ||
                (value != null && value.IdOrNull == null))
            {
                sb.AppendLine("<div id='{0}' name='{0}' style='display:none'>".Formato(TypeContext.Compose(idValueField, EntityBaseKeys.Entity)));

                //EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(cleanRuntimeType ?? type).ThrowIfNullC(Resources.TheresNotAViewForType0.Formato(cleanRuntimeType ?? type));
                //ViewDataDictionary vdd = new ViewDataDictionary(typeContext) //value
                //{ 
                //    { ViewDataKeys.MainControlUrl, es.PartialViewName},
                //    //{ ViewDataKeys.PopupPrefix, idValueField}
                //};
                //helper.PropagateSFKeys(vdd);
                //if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                //    vdd[ViewDataKeys.Reactive] = true;

                //using (var sc = StyleContext.RegisterCleanStyleContext(true))
                //    sb.AppendLine(helper.RenderPartialToString(Navigator.Manager.PopupControlUrl, vdd));

                sb.AppendLine("</div>");
            }
            else
                sb.AppendLine(helper.Div(TypeContext.Compose(idValueField, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

            //sb.AppendLine(helper.AsyncFileUpload(idValueField, new AsyncFileUploadOptions()));

            if (StyleContext.Current.ShowValidationMessage)
            {
                if (settings.ValueHtmlProps.ContainsKey("class"))
                    settings.ValueHtmlProps["class"] = "valueLine inlineVal " + settings.ValueHtmlProps["class"];
                else
                {
                    settings.ValueHtmlProps.Add("class", "valueLine inlineVal"); //inlineVal class tells Javascript code to show Inline Error
                }
            }
            else
            {
                if (settings.ValueHtmlProps.ContainsKey("class"))
                    settings.ValueHtmlProps["class"] = "valueLine " + settings.ValueHtmlProps["class"];
                else
                    settings.ValueHtmlProps.Add("class", "valueLine");
            }

            bool hasEntity = value != null;
            sb.AppendLine("<div id='div{0}Old' style='display:{1}'>".Formato(idValueField, hasEntity ? "block" : "none"));
            if (settings.View)
            {
                string viewingUrl = "javascript:DownloadFile('File.aspx/Download','{0}');".Formato(idValueField); // "javascript:OpenPopup(" + popupOpeningParameters + ");";
                sb.AppendLine(
                        helper.Href(TypeContext.Compose(idValueField, EntityBaseKeys.ToStrLink),
                            value.TryCC(f => f.FileName) ?? "",
                            viewingUrl,
                            "View",
                            "valueLine",
                            null));
            }
            else
            {
                sb.AppendLine(helper.Span(TypeContext.Compose(idValueField, EntityBaseKeys.ToStr), value.TryCC(f => f.FileName) ?? "", "valueLine", null));
            }
            if (settings.Remove)
            {
                sb.AppendLine(
                    helper.Button(TypeContext.Compose(idValueField, "btnRemove"),
                              "x",
                              "RemoveFileLineEntity('{0}');".Formato(idValueField),
                              "lineButton remove",
                              null));
            }
            sb.AppendLine("</div>");

            sb.AppendLine("<div id='div{0}New' style='display:{1}'>".Formato(idValueField, hasEntity ? "none" : "block"));
            //sb.AppendLine("<input type='file' onblur=\"window.alert('blur');\" onchange=\"window.alert('change');\" id='{0}' name='{0}' class='valueLine'/>".Formato(idValueField));
            //sb.AppendLine("<input type='file' id='{0}' name='{0}' class='valueLine'/>".Formato(idValueField));
            sb.AppendLine("<input type='file' onchange=\"UploadFile('File/Upload',this.id);\" id='{0}' name='{0}' class='valueLine'/>".Formato(idValueField));
            //sb.AppendLine("<input type='button' value='Submit' onclick=\"UploadFile('File/Upload','{0}');\" />".Formato(idValueField));
            sb.AppendLine("<img src='Images/loading.gif' id='{0}loading' alt='loading' style='display:none'/>".Formato(idValueField));
            sb.AppendLine("<iframe id='frame{0}' name='frame{0}' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>".Formato(idValueField));
            sb.AppendLine("</div>");
            
            if (StyleContext.Current.ShowValidationMessage)
            {
                sb.Append("&nbsp;");
                sb.AppendLine(helper.ValidationMessage(idValueField));
            }

            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            return sb.ToString();
        }

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
            where S : FilePathDN
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(FilePathDN);

            FileLine fl = new FileLine();

            //Navigator.ConfigureEntityBase(el, runtimeType, false);

            Common.FireCommonTasks(fl, typeof(T), context);

            helper.ViewContext.HttpContext.Response.Write(
                SetFileLineOptions(helper, context, fl));
        }

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<FileLine> settingsModifier)
            where S : FilePathDN
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(FilePathDN);

            FileLine fl = new FileLine();

            //Navigator.ConfigureEntityBase(el, runtimeType, false);

            Common.FireCommonTasks(fl, typeof(T), context);

            settingsModifier(fl);

            helper.ViewContext.HttpContext.Response.Write(
                SetFileLineOptions(helper, context, fl));
        }

        private static string SetFileLineOptions<S>(HtmlHelper helper, TypeContext<S> context, FileLine fl)
            where S : FilePathDN
        {
            if (fl != null)
                using (fl)
                    return helper.InternalFileLine(context, fl);
            else
                return helper.InternalFileLine(context, fl);
        }
        
    }
}
