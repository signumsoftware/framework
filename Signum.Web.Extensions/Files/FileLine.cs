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
using Signum.Web.Extensions.Files;
using Newtonsoft.Json.Linq;
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

        public bool AsyncUpload { get; set; }
        public bool DragAndDrop { get; set; }

        public bool Download { get; set; }

        public string UploadUrl { get; set; }
        public string UploadDroppedUrl { get; set; }

        public FileLine(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            AsyncUpload = true;
            DragAndDrop = true;
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

        protected override JObject OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("asyncUpload", AsyncUpload);
            if (UploadUrl.HasText())
                result.Add("uploadUrl", UploadUrl);
            if (UploadDroppedUrl.HasText())
                result.Add("uploadDroppedUrl", UploadDroppedUrl);
            if (!DragAndDrop)
                result.Add("dragAndDrop", false);
            return result;
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
