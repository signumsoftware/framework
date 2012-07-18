#region usings
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
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using Signum.Web.Extensions.Properties;
using System.IO;
using Signum.Entities.Files;
using Signum.Entities.Basics;
using System.Text;
using System.Net;
using Signum.Engine.Files;
using System.Reflection;
using Signum.Web.Controllers;
#endregion

namespace Signum.Web.Files
{
    public class FileController : Controller
    {
        [HttpPost]
        public PartialViewResult PartialView(
            string prefix,
            string fileType,
            int? sfId)
        {
            Type type = typeof(FilePathDN);
            FilePathDN entity = null;
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve<FilePathDN>(sfId.Value);
                else
                {
                    entity = new FilePathDN(EnumLogic<FileTypeDN>.ToEnum(fileType));
                }
            }
            ViewData["IdValueField"] = prefix;
            ViewData["FileType"] = fileType;

            string url = Navigator.Manager.EntitySettings[type].OnPartialViewName(entity);

            return Navigator.PartialView(this, entity, prefix, url);
        }

        public ActionResult Upload()
        {
            bool shouldSaveFilePath = !RuntimeInfo.FromFormValue((string)Request["fileParentRuntimeInfo"]).IsNew;

            FilePathDN fp = null;
            string prefix = "";
            foreach (string file in Request.Files)
            {
                prefix = file.Substring(0, file.IndexOf(FileLineKeys.File) - 1);

                RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);
                if (info.RuntimeType != typeof(FilePathDN))
                    continue;

                if (info.IdOrNull.HasValue)
                    continue; //Only new files will come with content

                HttpPostedFileBase hpf = Request.Files[file] as HttpPostedFileBase;
                if (string.IsNullOrEmpty(hpf.FileName))
                    continue; //It will have been uploaded by drag-drop

                string fileType = (string)Request.Form[TypeContextUtilities.Compose(prefix, FileLineKeys.FileType)];
                if (!fileType.HasText())
                    throw new InvalidOperationException("Couldn't create FilePath with unknown FileType for file '{0}'".Formato(file));

                fp = new FilePathDN(EnumLogic<FileTypeDN>.ToEnum(fileType))
                {
                    FileName = Path.GetFileName(hpf.FileName),
                    BinaryFile = hpf.InputStream.ReadAllBytes()
                };

                if (shouldSaveFilePath)
                    fp = fp.Save();
                else 
                    Session[Request.Form[ViewDataKeys.TabId] + prefix] = fp;
            }

            return UploadResult(prefix, fp, shouldSaveFilePath);
        }

        public ContentResult UploadDropped()
        {
            bool shouldSaveFilePath = !RuntimeInfo.FromFormValue((string)Request.Headers["X-" + EntityBaseKeys.RuntimeInfo]).IsNew;

            string fileName = Request.Headers["X-FileName"];

            string prefix = Request.Headers["X-Prefix"];

            RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Headers["X-" + TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);
            if (info.RuntimeType != typeof(FilePathDN))
                throw new InvalidOperationException("Only FilePaths can be uploaded with drag and drop");

            string fileType = (string)Request.Headers["X-" + FileLineKeys.FileType];
            if (!fileType.HasText())
                throw new InvalidOperationException("Couldn't create FilePath with unknown FileType for file '{0}'".Formato(prefix));

            FilePathDN fp = new FilePathDN(EnumLogic<FileTypeDN>.ToEnum(fileType))
            {
                FileName = fileName,
                BinaryFile = Request.InputStream.ReadAllBytes()
            };

            if (shouldSaveFilePath)
                fp = fp.Save();
            else
                Session[Request.Headers["X-" + ViewDataKeys.TabId] + prefix] = fp;

            return UploadResult(prefix, fp, shouldSaveFilePath);
        }

        private ContentResult UploadResult(string prefix, FilePathDN filePath, bool shouldHaveSaved)
        {
            StringBuilder sb = new StringBuilder();
            //Use plain javascript not to have to add also the reference to jquery in the result iframe
            sb.AppendLine("<html><head><title>-</title></head><body>");
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var parDoc = window.parent.document;");

            if (filePath.TryCS(f => f.IdOrNull) != null || !shouldHaveSaved)
            {
                sb.AppendLine("parDoc.getElementById('{0}loading').style.display='none';".Formato(prefix));
                sb.AppendLine("parDoc.getElementById('{0}').innerHTML='{1}';".Formato(TypeContextUtilities.Compose(prefix, EntityBaseKeys.ToStrLink), filePath.FileName));
                sb.AppendLine("parDoc.getElementById('{0}').value='{1}';".Formato(TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo), new RuntimeInfo(filePath).ToString()));
                sb.AppendLine("parDoc.getElementById('{0}').style.display='none';".Formato(TypeContextUtilities.Compose(prefix, "DivNew")));
                sb.AppendLine("parDoc.getElementById('{0}').style.display='block';".Formato(TypeContextUtilities.Compose(prefix, "DivOld")));
                sb.AppendLine("parDoc.getElementById('{0}').style.display='block';".Formato(TypeContextUtilities.Compose(prefix, "btnRemove")));
                sb.AppendLine("var frame = parDoc.getElementById('{0}'); frame.parentNode.removeChild(frame);".Formato(TypeContextUtilities.Compose(prefix, "frame")));
            }
            else
            {
                sb.AppendLine("parDoc.getElementById('{0}loading').style.display='none';".Formato(prefix));
                sb.AppendLine("window.parent.alert('{0}');".Formato(Resources.ErrorSavingFile));
            }

            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return Content(sb.ToString());
        }

        public FileResult Download(int? filePathID)
        {
            if (filePathID == null)
                throw new ArgumentException("Argument 'filePathID' was not passed to the controller");

            FilePathDN fp = Database.Retrieve<FilePathDN>(filePathID.Value);

            if (fp == null)
                throw new ArgumentException("Argument 'filePathID' was not passed to the controller");
            /*
            byte[] binaryFile;

            binaryFile = fp.FullWebPath != null ? new WebClient().DownloadData(fp.FullWebPath)
                : FilePathLogic.GetByteArray(fp);

            return File(binaryFile, SignumController.GetMimeType(Path.GetExtension(fp.FileName)), fp.FileName);*/

            
            //
            //HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + Path.GetFileName(path));
            //HttpContext.Response.ContentType = 
            //HttpContext.Response.TransmitFile(path);

            string path = fp.FullPhysicalPath;
            return File(FilePathLogic.GetByteArray(fp), MimeType.FromExtension(path), fp.FileName);
            //return File(path,  MimeType.FromFileName(path)); <--this cannot handle dots inside path
        }
    }
}
