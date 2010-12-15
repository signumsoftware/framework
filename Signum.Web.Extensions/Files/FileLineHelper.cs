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
using Signum.Web;
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
        internal static MvcHtmlString InternalFileLine(this HtmlHelper helper, FileLine fileLine)
        {
            if (!fileLine.Visible)
                return MvcHtmlString.Empty;

            IFile value = (IFile)fileLine.UntypedValue;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(helper.HiddenEntityInfo(fileLine));

            FilePathDN filePath = value as FilePathDN;
            if (filePath != null)
            {
                if (fileLine.FileType == null)
                    fileLine.FileType = FileLineHelper.GetFileTypeFromValue(filePath);
                if (fileLine.FileType == null)
                    throw new ArgumentException("FileType property of FileLine settings must be specified for FileLine {0}".Formato(fileLine.ControlID));

                sb.AddLine(helper.Hidden(fileLine.Compose(FileLineKeys.FileType),
                    EnumDN.UniqueKey((value != null) ?
                        filePath.FileTypeEnum ?? EnumLogic<FileTypeDN>.ToEnum(filePath.FileType) :
                        fileLine.FileType)));
            }

            if (value != null)
                sb.AddLine(helper.Div(fileLine.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

            fileLine.ValueHtmlProps.AddCssClass("valueLine");
            if (fileLine.ShowValidationMessage)
                fileLine.ValueHtmlProps.AddCssClass("inlineVal"); //inlineVal class tells Javascript code to show Inline Error

            bool hasEntity = value != null && value.FileName.HasText();

            using (sb.Surround(new HtmlTag("div", fileLine.Compose("DivOld")).Attr("style", "display:" + (hasEntity ? "block" : "none"))))
            {

                sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, fileLine,
                    fileLine.Download ? fileLine.Compose(EntityBaseKeys.ToStrLink) : fileLine.Compose(EntityBaseKeys.ToStr)));

                if (fileLine.Download && !(value is EmbeddedEntity))
                {
                    sb.AddLine(
                            helper.Href(fileLine.Compose(EntityBaseKeys.ToStrLink),
                                value.TryCC(f => f.FileName) ?? "",
                                fileLine.GetDownloading(),
                                "Download",
                                "valueLine",
                                null));
                }
                else
                    sb.AddLine(helper.Span(fileLine.Compose(EntityBaseKeys.ToStr), value.TryCC(f => f.FileName) ?? "", "valueLine", null));

                sb.AddLine(EntityBaseHelper.RemoveButton(helper, fileLine));
            }

            using (sb.Surround(new HtmlTag("div", fileLine.Compose("DivNew")).Attr("style", "display:" + (hasEntity ? "block" : "none"))))
            {
                sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, fileLine, fileLine.ControlID));

                sb.AddLine(MvcHtmlString.Create("<input type='file' onchange=\"FLineOnChanged({0});\" id='{1}' name='{1}' class='valueLine'/>".Formato(fileLine.ToJS(), fileLine.ControlID)));
                sb.AddLine(MvcHtmlString.Create("<img src='Images/loading.gif' id='{0}loading' alt='loading' style='display:none'/>".Formato(fileLine.ControlID)));
                sb.AddLine(MvcHtmlString.Create("<iframe id='frame{0}' name='frame{0}' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>".Formato(fileLine.ControlID)));
            }

            if (fileLine.ShowValidationMessage)
            {
                sb.Add(MvcHtmlString.Create("&nbsp;"));
                sb.AddLine(helper.ValidationMessage(fileLine.ControlID));
            }

            sb.AddLine(EntityBaseHelper.BreakLineDiv(helper, fileLine));

            return sb.ToHtml();
        }

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
            where S : IFile
        {
            helper.FileLine<T, S>(tc, property, null);
        }

        public static void FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<FileLine> settingsModifier)
            where S : IFile
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

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
