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
using System.Web.Routing;
#endregion

namespace Signum.Web.Files
{
    public static class FileLineKeys
    {
        public const string FileType = "FileType";
    }

    public class FileLine : EntityBase
    {
        public Enum FileType { get; set; }

        public readonly RouteValueDictionary ValueHtmlProps = new RouteValueDictionary();

        bool asyncUpload = true;
        public bool AsyncUpload
        {
            get { return asyncUpload; }
            set { asyncUpload = value; }
        }

        public bool Download { get; set; }

        public string DownloadUrl { get; set; }
        public string UploadUrl { get; set; }

        public string Downloading { get; set; }
        internal string GetDownloading()
        {
            if (!Download)
                return "";
            return Downloading ?? DefaultDownloading();
        }

        public string OnChanged { get; set; }
        internal string GetOnChanged()
        {
            return OnChanged ?? DefaultOnChanged();
        }

        public FileLine(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Download = true;
            Create = false;
            View = false;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            //Implementations = null;
        }

        public override string ToJS()
        {
            return "new FLine(" + this.OptionsJS() + ")";
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("asyncUpload", AsyncUpload ? "true" : "false");
            if (DownloadUrl.HasText())
                result.Add("controllerUrl", DownloadUrl.SingleQuote());
            if (UploadUrl.HasText())
                result.Add("controllerUrl", UploadUrl.SingleQuote());
            return result;
        }

        protected override string DefaultRemoving()
        {
            return FileLine.JsRemoving(this).ToJS();
        }

        public static JsInstruction JsRemoving(FileLine fline)
        {
            return new JsInstruction(() => "FLineOnRemoving({0})".Formato(fline.ToJS()));
        }

        protected string DefaultDownloading()
        {
            return FileLine.JsDownloading(this).ToJS();
        }

        public static JsInstruction JsDownloading(FileLine fline)
        {
            if (fline.DownloadUrl == null)
                fline.DownloadUrl = RouteHelper.New().Action("Download", "File");

            return new JsInstruction(() => "javascript:FLineOnDownloading({0})".Formato(fline.ToJS()));
        }

        protected string DefaultOnChanged()
        {
            return FileLine.JsOnChanged(this).ToJS();
        }

        public static JsInstruction JsOnChanged(FileLine fline)
        {
            if (fline.UploadUrl == null)
                fline.UploadUrl = RouteHelper.New().Action("Upload", "File");

            return new JsInstruction(() => "javascript:FLineOnChanged({0})".Formato(fline.ToJS()));
        }

        protected override string DefaultFinding()
        {
            return null;
        }

        protected override string DefaultViewing()
        {
            return null;
        }

        protected override string DefaultCreating()
        {
            return null;
        }
    }
}
