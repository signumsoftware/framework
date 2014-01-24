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
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Engine;
#endregion

namespace Signum.Web.Files
{
    public static class FileLineHelper
    {
        

        public static MvcHtmlString FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
            where S : IFile
        {
            return FileLineInternal<T, S>(helper, tc, property, null);
        }

        public static MvcHtmlString FileLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<FileLine> settingsModifier)
            where S : IFile
        {
            return FileLineInternal<T, S>(helper, tc, property, settingsModifier);
        }

        public static MvcHtmlString FileLineLite<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, Lite<S>>> property)
           where S : class, IFile, IIdentifiable 
        {
            return FileLineInternal<T, Lite<S>>(helper, tc, property, null);
        }

        public static MvcHtmlString FileLineLite<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, Lite<S>>> property, Action<FileLine> settingsModifier)
           where S : class, IFile, IIdentifiable 
        {
            return FileLineInternal<T, Lite<S>>(helper, tc, property, settingsModifier);
        }

        static MvcHtmlString FileLineInternal<T, S>(HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<FileLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            FileLine fl = new FileLine(context.Type, context.UntypedValue, context, "", context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(fl, fl.Type.CleanType());

            fl.Download = (context.Type.IsIIdentifiable() || context.Type.IsLite());

            Common.FireCommonTasks(fl);

            if (settingsModifier != null)
                settingsModifier(fl);

            return helper.InternalFileLine(fl);
        }

        internal static MvcHtmlString InternalFileLine(this HtmlHelper helper, FileLine fileLine)
        {
            if (!fileLine.Visible)
                return MvcHtmlString.Empty;

            IFile value = fileLine.GetFileValue(); 

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("div").Id(fileLine.ControlID).Class("sf-field")))
            using (fileLine.ValueFirst ? sb.Surround(new HtmlTag("div").Class("sf-value-first")) : null)
            {
                sb.AddLine(new HtmlTag("link").Attrs(new { rel = "stylesheet", type = "text/css", href = RouteHelper.New().Content("~/Files/Content/SF_Files.css") }).ToHtmlSelf());

                if (value != null)
                    sb.AddLine(helper.Div(fileLine.Compose(EntityBaseKeys.Entity), null, "", new Dictionary<string, object> { { "style", "display:none" } }));

                fileLine.ValueHtmlProps.AddCssClass("sf-value-line");
                if (fileLine.ShowValidationMessage)
                    fileLine.ValueHtmlProps.AddCssClass("inlineVal"); //inlineVal class tells Javascript code to show Inline Error

                bool hasEntity = value != null && value.FileName.HasText();

                using (sb.Surround(new HtmlTag("div", fileLine.Compose("DivOld")).Attr("style", "display:" + (hasEntity ? "block" : "none"))))
                {
                    var label = EntityBaseHelper.BaseLineLabel(helper, fileLine,
                            fileLine.Download ? fileLine.Compose(EntityBaseKeys.Link) : fileLine.Compose(EntityBaseKeys.ToStr));

                    if (!fileLine.ValueFirst)
                        sb.AddLine(label);

                    using (sb.Surround(new HtmlTag("div").Class("sf-value-container")))
                    {
                        if (fileLine.Download)
                        {
                            sb.AddLine(
                                    helper.Href(fileLine.Compose(EntityBaseKeys.Link),
                                        value.TryCC(f => f.FileName),
                                        hasEntity ? FilesClient.GetDownloadPath(value) : null,
                                        "Download",
                                        "sf-value-line",
                                        new Dictionary<string, object> { { "download", value.TryCC(f => f.FileName)} }));
                        }
                        else
                        {
                            sb.AddLine(helper.Span(fileLine.Compose(EntityBaseKeys.ToStr), value.TryCC(f => f.FileName) ?? "", "sf-value-line", null));
                        }

                        if (fileLine.Type.IsEmbeddedEntity())
                            sb.AddLine(helper.Hidden(fileLine.Compose(EntityBaseKeys.EntityState), value.TryCC(f => Navigator.Manager.SerializeEntity((ModifiableEntity)f))));

                        sb.AddLine(EntityBaseHelper.RemoveButton(helper, fileLine, hidden: false));

                        if (fileLine.ValueFirst)
                            sb.AddLine(label);
                    }
                }

                var filesParentPrefix = ((TypeContext)fileLine).FollowC(fl => (TypeContext)fl.Parent).First(ctx => ctx.Type != typeof(FilePathDN) && ctx.Type != typeof(MList<FilePathDN>)).ControlID;

                var divNew = new HtmlTag("div", fileLine.Compose("DivNew"))
                    .Class("sf-file-line-new")
                    .Attr("style", "display:" + (hasEntity ? "none" : "block"))
                    .Attr("data-parent-prefix", filesParentPrefix.HasText() ? filesParentPrefix : "");

                using (sb.Surround(divNew))
                //using (sb.Surround(new HtmlTag("form").Attrs(new { method = "post", enctype = "multipart/form-data", encoding = "multipart/form-data", target = "frame" + fileLine.ControlID })))
                {
                    sb.AddLine(helper.HiddenEntityInfo(fileLine));

                    if (fileLine.PropertyRoute.Type.CleanType() == typeof(FilePathDN))
                    {
                        FilePathDN filePath = value as FilePathDN;
                        if (filePath != null)
                        {
                            sb.AddLine(helper.Hidden(fileLine.Compose(FileLineKeys.FileType),
                                MultiEnumDN.UniqueKey(filePath.FileTypeEnum ?? filePath.FileType.ToEnum())));
                        }
                        else
                        {
                            if (fileLine.FileType == null)
                                throw new ArgumentException("FileType property of FileLine settings must be specified for FileLine {0}".Formato(fileLine.ControlID));

                            sb.AddLine(helper.Hidden(fileLine.Compose(FileLineKeys.FileType), MultiEnumDN.UniqueKey(fileLine.FileType)));
                        }
                    }

                    var label = EntityBaseHelper.BaseLineLabel(helper, fileLine, fileLine.ControlID);

                    if (!fileLine.ValueFirst)
                        sb.AddLine(label);

                    using (sb.Surround(new HtmlTag("div").Class("sf-value-container")))
                    {
                        sb.AddLine(MvcHtmlString.Create("<input type='file' onchange=\"{0}.onChanged()\" id='{1}' name='{1}' class='sf-value-line'/>".Formato(fileLine.ToJS(), fileLine.Compose(FileLineKeys.File))));
                        sb.AddLine(MvcHtmlString.Create("<img src='{0}' id='{1}_loading' alt='loading' style='display:none'/>".Formato(RouteHelper.New().Content("~/Files/Images/loading.gif"), fileLine.ControlID)));
                        
                        if (fileLine.ValueFirst)
                            sb.AddLine(label);
                    }
                }

                if (fileLine.ShowValidationMessage)
                    sb.AddLine(helper.ValidationMessage(fileLine.Compose(FileLineKeys.File)));
            }

            sb.AddLine(helper.RegisterUrls(new Dictionary<string,string>
            {
                { "uploadFile", RouteHelper.New().Action<FileController>(fc => fc.Upload()) },
                { "uploadDroppedFile", RouteHelper.New().Action<FileController>(fc => fc.UploadDropped()) },
                { "downloadFile", RouteHelper.New().Action("Download", "File") },
            }));            

            sb.AddLine(new HtmlTag("script")
                .Attr("type", "text/javascript")
                .InnerHtml(MvcHtmlString.Create("$(function(){ " +
                    "SF.Loader.loadJs('{0}', function(){{ $('#{1}').fileLine({2}); }});".Formato(
                        RouteHelper.New().Content("~/Files/Scripts/SF_Files.js"),
                        fileLine.ControlID,
                        fileLine.OptionsJS()) +
                    "});"))
                .ToHtml());

            return sb.ToHtml();
        }
    }
}
