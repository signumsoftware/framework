using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using System.IO;
using Signum.Entities.Files;
using Signum.Entities.Basics;
using System.Text;
using System.Net;
using Signum.Engine.Files;
using System.Reflection;
using Signum.Web.Controllers;
using Signum.Web.PortableAreas;
using Signum.Entities.Reflection;

namespace Signum.Web.Files
{
    public class FileController : Controller
    {
        public ActionResult Upload()
        {
            string singleFile =  Request.Files.Cast<string>().Single();

            string prefix = singleFile.Substring(0, singleFile.IndexOf(FileLineKeys.File) - 1);

            RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            HttpPostedFileBase hpf = Request.Files[singleFile] as HttpPostedFileBase;

            string fileName = Path.GetFileName(hpf.FileName);
            byte[] bytes = hpf.InputStream.ReadAllBytes();
            string fileType = (string)Request.Form[TypeContextUtilities.Compose(prefix, FileLineKeys.FileType)];
            string extraData = (string)Request.Form[TypeContextUtilities.Compose(prefix, FileLineKeys.CalculatedDirectory)];

            IFile file = FilesClient.ConstructFile(info.EntityType, new UploadedFileData
            {
                FileName = fileName,
                Content = bytes,
                FileType = fileType,
                CalculatedDirectory = extraData
            });

            StringBuilder sb = new StringBuilder();
            //Use plain javascript not to have to add also the reference to jquery in the result iframe
            sb.AppendLine("<html><head><title>-</title></head><body>");
            sb.AppendLine("<script type='text/javascript'>");
            RuntimeInfo ri = file is EmbeddedEntity ? new RuntimeInfo((EmbeddedEntity)file) : new RuntimeInfo((IEntity)file);
            sb.AppendLine("window.parent.$.data(window.parent.document.getElementById('{0}'), 'SF-control').onUploaded('{1}', '{2}', '{3}', '{4}')".FormatWith(
                prefix,
                file.FileName,
                FilesClient.GetDownloadUrl(file),
                ri.ToString(),
                info.EntityType.IsEmbeddedEntity() ? Navigator.Manager.SerializeEntity((EmbeddedEntity)file) : null));
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return Content(sb.ToString());
        }

        public JsonNetResult UploadDropped()
        {
            string prefix = Request.Headers["X-Prefix"];

            RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Headers["X-" + TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            string fileName = Request.Headers["X-FileName"];
            byte[] bytes = Request.InputStream.ReadAllBytes();
            string fileType = (string)Request.Headers["X-" + FileLineKeys.FileType];
            string calculatedDirectory = (string)Request.Headers["X-" + FileLineKeys.CalculatedDirectory];

            IFile file = FilesClient.ConstructFile(info.EntityType, new UploadedFileData
            {
                FileName = fileName,
                Content = bytes,
                FileType = fileType,
                CalculatedDirectory = calculatedDirectory
            });

            RuntimeInfo ri = file is EmbeddedEntity ? new RuntimeInfo((EmbeddedEntity)file) : new RuntimeInfo((IEntity)file);
            
            return this.JsonNet(new
            {
                file.FileName,
                FullWebPath = FilesClient.GetDownloadUrl(file),
                RuntimeInfo = ri.ToString(),
                EntityState = info.EntityType.IsEmbeddedEntity() ? Navigator.Manager.SerializeEntity((EmbeddedEntity)file) : null,
            }); 
        }

        public ActionResult Download(string file)
        {
            if (file == null)
                throw new ArgumentException("file");

            RuntimeInfo ri = RuntimeInfo.FromFormValue(file);

            return FilesClient.DownloadFileResult(ri);
        }

        public FileResult DownloadEmbedded(Lite<FileTypeSymbol> lite, string suffix, string fileName)
        {
            var virtualFile = new FilePathEmbedded(lite.Retrieve())
            {
                Suffix = suffix,
                FileName = fileName
            };

            var pair = FileTypeLogic.FileTypes.GetOrThrow(lite.Retrieve()).GetPrefixPair(virtualFile);

            var fullPhysicalPath = Path.Combine(pair.PhysicalPrefix, suffix);

            return new FilePathResult(fullPhysicalPath, MimeMapping.GetMimeMapping(fullPhysicalPath)) { FileDownloadName = fileName };
        }
    }
}
