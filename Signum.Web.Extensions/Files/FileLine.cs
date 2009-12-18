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
    public static class FileLineKeys
    {
        public const string FileType = "FileType";
    }

    public class FileLine : EntityBase
    {
        public Enum FileType { get; set; }

        public readonly Dictionary<string, object> ValueHtmlProps = new Dictionary<string, object>(0);

        public bool Download { get; set; }
        
        public string Downloading { get; set; }
        internal string GetDownloading()
        {
            if (!Download)
                return "";
            return Downloading ?? DefaultDownloading();
        }

        public FileLine(string prefix)
        {
            Prefix = prefix;
            Download = true;
            Create = false;
            View = false;
        }

        public override void SetReadOnly()
        {
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }

        public override string ToJS()
        {
            return "new FLine(" + this.OptionsJS() + ")";
        }

        protected override string DefaultRemoving()
        {
            return FileLine.JsRemoving(this).ToJS();
        }

        public static JsRenderer JsRemoving(FileLine fline)
        {
            return new JsRenderer(() => "FLineOnRemoving({0})".Formato(fline.ToJS()));
        }

        protected string DefaultDownloading()
        {
            return FileLine.JsDownloading(this).ToJS();
        }

        public static JsRenderer JsDownloading(FileLine fline)
        {
            return new JsRenderer(() => "javascript:FLineOnDownloading({0})".Formato(fline.ToJS()));
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
