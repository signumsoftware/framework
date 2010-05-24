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
        internal static string InternalFileLine(this HtmlHelper helper, FileLine fileLine)
        {
            if (!fileLine.Visible)
                return "";

            FilePathDN value = (FilePathDN)fileLine.UntypedValue;

            if (fileLine.FileType == null)
                fileLine.FileType = FileLineHelper.GetFileTypeFromValue(value);
            if (fileLine.FileType == null)
                throw new ArgumentException(Resources.FileTypePropertyOfFileLineSettingsMustBeSpecified.Formato(fileLine.ControlID));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.HiddenEntityInfo(fileLine));

            sb.AppendLine(helper.Hidden(fileLine.Compose(FileLineKeys.FileType),
                EnumDN.UniqueKey((value != null) ?
                    value.FileTypeEnum ?? EnumLogic<FileTypeDN>.ToEnum(value.FileType) :
                    fileLine.FileType)).ToHtmlString());

            if (value != null)
                sb.AppendLine(helper.Div(fileLine.Compose(EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

            fileLine.ValueHtmlProps.AddCssClass("valueLine");
            if (fileLine.ShowValidationMessage)
                fileLine.ValueHtmlProps.AddCssClass("inlineVal"); //inlineVal class tells Javascript code to show Inline Error

            bool hasEntity = value != null && value.FileName.HasText();
            sb.AppendLine("<div id='{0}DivOld' style='display:{1}'>".Formato(fileLine.ControlID, hasEntity ? "block" : "none"));

            if (fileLine.Download)
            {
                sb.AppendLine(
                        helper.Href(fileLine.Compose(EntityBaseKeys.ToStrLink),
                            value.TryCC(f => f.FileName) ?? "",
                            fileLine.GetDownloading(),
                            "Download",
                            "valueLine",
                            null));
            }
            else
                sb.AppendLine(helper.Span(fileLine.Compose(EntityBaseKeys.ToStr), value.TryCC(f => f.FileName) ?? "", "valueLine", null));

            sb.AppendLine(EntityBaseHelper.WriteRemoveButton(helper, fileLine));
            sb.AppendLine("</div>");

            sb.AppendLine("<div id='{0}DivNew' style='display:{1}'>".Formato(fileLine.ControlID, hasEntity ? "none" : "block"));
            sb.AppendLine("<input type='file' onchange=\"FLineOnChanged({0});\" id='{1}' name='{1}' class='valueLine'/>".Formato(fileLine.ToJS(), fileLine.ControlID));
            sb.AppendLine("<img src='Images/loading.gif' id='{0}loading' alt='loading' style='display:none'/>".Formato(fileLine.ControlID));
            sb.AppendLine("<iframe id='frame{0}' name='frame{0}' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>".Formato(fileLine.ControlID));
            sb.AppendLine("</div>");

            if (fileLine.ShowValidationMessage)
            {
                sb.Append("&nbsp;");
                sb.AppendLine(helper.ValidationMessage(fileLine.ControlID).TryCC(hs => hs.ToHtmlString()));
            }

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, fileLine));

            return sb.ToString();
        }

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

            FileLine fl = new FileLine(context.Type, context.UntypedValue, context, "", context.PropertyRoute);

            Common.FireCommonTasks(fl);

            if (settingsModifier != null)
                settingsModifier(fl);

            helper.Write(helper.InternalFileLine(fl));
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
