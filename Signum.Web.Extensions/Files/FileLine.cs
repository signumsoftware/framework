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
using Signum.Web.Files;
using Newtonsoft.Json.Linq;

namespace Signum.Web.Files
{
    public static class FileLineKeys
    {
        public const string File = "sfFile";
        public const string FileType = "sfFileType";
        public const string ExtraData = "sfExtraData";
    }

    public enum DownloadBehaviour
    {
        SaveAs,
        View,
        None
    }

    public class FileLine : EntityBase
    {
        public FileTypeSymbol FileType { get; set; }

        public string ExtraData { get; set; }

        public readonly RouteValueDictionary ValueHtmlProps = new RouteValueDictionary();

        public bool AsyncUpload { get; set; }
        public bool DragAndDrop { get; set; }

        public DownloadBehaviour Download { get; set; }

        public string UploadUrl { get; set; }
        public string UploadDroppedUrl { get; set; }

        public FileLine(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            AsyncUpload = true;
            DragAndDrop = true;
            Download = DownloadBehaviour.View;
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

        protected override Dictionary<string, object> OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("asyncUpload", AsyncUpload);
            if (UploadUrl.HasText())
                result.Add("uploadUrl", UploadUrl);
            if (UploadDroppedUrl.HasText())
                result.Add("uploadDroppedUrl", UploadDroppedUrl);
            if (!DragAndDrop)
                result.Add("dragAndDrop", false);
            result.Add("download", (int)Download);

            if (this.Type.CleanType() == typeof(FilePathEntity) && !this.ReadOnly)
            {
                if (FileType == null)
                    throw new ArgumentException("FileType is mandatory for FilePathEntity (FileLine {0})".FormatWith(Prefix));

                result.Add("fileType", FileType.Key);
            }

            if (this.ExtraData.HasText())
            {
                result.Add("extraData", this.ExtraData);
            }

            return result;
        }

        public IFile GetFileValue()
        {
            Lite<Entity> lite = UntypedValue as Lite<Entity>;

            if (lite != null)
                return (IFile)lite.Retrieve();


            return (IFile)UntypedValue;
        }
    }
}
