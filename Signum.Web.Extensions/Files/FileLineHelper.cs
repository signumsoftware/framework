#region usings
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
#endregion

namespace Signum.Web.Files
{
    public static class FileLineHelper
    {
        internal static string InternalFileLine<T>(this HtmlHelper helper, TypeContext<T> typeContext, FileLine settings)
            where T : FilePathDN
        {
            if (!settings.Visible)
                return null;
            
            string prefix = helper.GlobalName(typeContext.Name);
            T value = typeContext.Value;
            Type cleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T); //typeContext.ContextType;
            bool isIdentifiable = typeof(IIdentifiable).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));

            Type cleanRuntimeType = null;
            if (value != null)
                cleanRuntimeType = typeof(Lite).IsAssignableFrom(value.GetType()) ? (value as Lite).RuntimeType : value.GetType();

            long? ticks = EntityBaseHelper.GetTicks(helper, prefix, settings);

            if (settings.FileType == null)
                settings.FileType = FileLineHelper.GetFileTypeFromValue(value);
            if (settings.FileType == null)
                throw new ArgumentException(Resources.FileTypePropertyOfFileLineSettingsMustBeSpecified.Formato(prefix));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            sb.AppendLine(helper.HiddenSFInfo(prefix, new EntityInfo<T>(value) { Ticks = ticks }));

            sb.AppendLine(helper.Hidden(TypeContext.Compose(prefix, FileLineKeys.FileType),
                EnumDN.UniqueKey((value != null) ?
                    value.FileTypeEnum ?? EnumLogic<FileTypeDN>.ToEnum(value.FileType) :
                    settings.FileType)));

            if (value != null)
                sb.AppendLine(helper.Div(TypeContext.Compose(prefix, EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } })); 

            if (StyleContext.Current.ShowValidationMessage)
            {
                if (settings.ValueHtmlProps.ContainsKey("class"))
                    settings.ValueHtmlProps["class"] = "valueLine inlineVal " + settings.ValueHtmlProps["class"];
                else
                    settings.ValueHtmlProps.Add("class", "valueLine inlineVal"); //inlineVal class tells Javascript code to show Inline Error
            }
            else
            {
                if (settings.ValueHtmlProps.ContainsKey("class"))
                    settings.ValueHtmlProps["class"] = "valueLine " + settings.ValueHtmlProps["class"];
                else
                    settings.ValueHtmlProps.Add("class", "valueLine");
            }

            bool hasEntity = value != null && value.FileName.HasText();
            sb.AppendLine("<div id='{0}DivOld' style='display:{1}'>".Formato(prefix, hasEntity ? "block" : "none"));
            if (settings.Download)
            {
                sb.AppendLine(
                        helper.Href(TypeContext.Compose(prefix, EntityBaseKeys.ToStrLink),
                            value.TryCC(f => f.FileName) ?? "",
                            settings.GetDownloading(),
                            "Download",
                            "valueLine",
                            null));
            }
            else
                sb.AppendLine(helper.Span(TypeContext.Compose(prefix, EntityBaseKeys.ToStr), value.TryCC(f => f.FileName) ?? "", "valueLine", null));

            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, settings, value));
            sb.AppendLine("</div>");

            sb.AppendLine("<div id='{0}DivNew' style='display:{1}'>".Formato(prefix, hasEntity ? "none" : "block"));
            sb.AppendLine("<input type='file' onchange=\"FLineOnChanged({0});\" id='{1}' name='{1}' class='valueLine'/>".Formato(settings.ToJS(), prefix));
            sb.AppendLine("<img src='Images/loading.gif' id='{0}loading' alt='loading' style='display:none'/>".Formato(prefix));
            sb.AppendLine("<iframe id='frame{0}' name='frame{0}' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>".Formato(prefix));
            sb.AppendLine("</div>");

            if (StyleContext.Current.ShowValidationMessage)
            {
                sb.Append("&nbsp;");
                sb.AppendLine(helper.ValidationMessage(prefix));
            }

            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            return sb.ToString();
        }

        //public static string FileLine<T>(this HtmlHelper helper, T value, string idValueField, FileLine settings) where T : FilePathDN
        //{
        //    if (settings != null)
        //        using (settings)
        //            return helper.InternalFileLine(idValueField, value, settings);
        //    else
        //        return helper.InternalFileLine(idValueField, value, settings);
        //}

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
            where S : FilePathDN
        {
            helper.FileLine<T, S>(tc, property, null);
        }

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<FileLine> settingsModifier)
            where S : FilePathDN
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            Type runtimeType = typeof(FilePathDN);

            FileLine fl = new FileLine(helper.GlobalName(context.Name));
            //Navigator.ConfigureEntityBase(el, runtimeType, false);
            Common.FireCommonTasks(fl, typeof(T), context);

            if (settingsModifier != null)
                settingsModifier(fl);

            using (fl)
                helper.ViewContext.HttpContext.Response.Write(
                    helper.InternalFileLine(context, fl));
        }

        private static Enum GetFileTypeFromValue(FilePathDN fp)
        {
            if (fp == null)
                return null;
            
            if (fp.FileTypeEnum != null)
                return fp.FileTypeEnum;
            else if (fp.FileType != null)
                return EnumLogic<FileTypeDN>.ToEnum(fp.FileType);

            return null;
        }
    }
}
