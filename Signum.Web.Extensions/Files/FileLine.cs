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
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using System.Web.Routing;
using Signum.Engine;
#endregion

namespace Signum.Web.Files
{
    public static class FileLineKeys
    {
        public const string File = "sfFile";
        public const string FileType = "sfFileType";
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

        public string UploadUrl { get; set; }
        public string UploadDroppedUrl { get; set; }

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
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("asyncUpload", AsyncUpload ? "true" : "false");
            if (UploadUrl.HasText())
                result.Add("uploadUrl", UploadUrl.SingleQuote());
            if (UploadDroppedUrl.HasText())
                result.Add("uploadDroppedUrl", UploadDroppedUrl.SingleQuote());
            return result;
        }

        protected override string DefaultRemove()
        {
            return JsRemoving().ToJS();
        }

        public JsInstruction JsRemoving()
        {
            return new JsInstruction(() => "{0}.remove()".Formato(this.ToJS()));
        }


        protected string DefaultOnChanged()
        {
            return JsOnChanged().ToJS();
        }

        public JsInstruction JsOnChanged()
        {
            return new JsInstruction(() => "{0}.onChanged()".Formato(this.ToJS()));
        }

        protected override string DefaultFind()
        {
            return null;
        }

        protected override string DefaultView()
        {
            return null;
        }

        protected override string DefaultCreate()
        {
            return null;
        }

        public IFile GetFileValue()
        {
            Lite<IdentifiableEntity> lite = UntypedValue as Lite<IdentifiableEntity>;

            if (lite != null)
                return (IFile)lite.Retrieve();


            return (IFile)UntypedValue;
        }
    }
}
