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
#endregion

namespace Signum.Web.Files
{
    public class FileController : Controller
    {
        [HttpPost]
        public PartialViewResult PartialView(string prefix, string fileType, int? sfId)
        {
            Type type = typeof(FilePathDN);
            FilePathDN entity = null;
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve<FilePathDN>(sfId.Value);
                else
                {
                    entity = new FilePathDN(MultiEnumLogic<FileTypeDN>.ToEnum(fileType));
                }
            }
            ViewData["IdValueField"] = prefix;
            ViewData["FileType"] = fileType;

            string partialViewName = Navigator.Manager.EntitySettings[type].OnPartialViewName(entity);

            return Navigator.PartialView(this, entity, prefix, partialViewName);
        }

        public ActionResult Upload()
        {
            string fileName = Request.Files.Cast<string>().Single();

            string prefix = fileName.Substring(0, fileName.IndexOf(FileLineKeys.File) - 1);

            RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            bool isEmbedded = info.EntityType.IsEmbeddedEntity(); 

            IFile file;
            if (info.EntityType == typeof(FilePathDN))
            {
                string fileType = (string)Request.Form[TypeContextUtilities.Compose(prefix, FileLineKeys.FileType)];
                if (!fileType.HasText())
                    throw new InvalidOperationException("Couldn't create FilePath with unknown FileType for file '{0}'".Formato(fileName));

                file = new FilePathDN(MultiEnumLogic<FileTypeDN>.ToEnum(fileType));
            }
            else
            {
                file = (IFile)Activator.CreateInstance(info.EntityType);
            }

            HttpPostedFileBase hpf = Request.Files[fileName] as HttpPostedFileBase;

            file.FileName = Path.GetFileName(hpf.FileName);
            file.BinaryFile = hpf.InputStream.ReadAllBytes();

            if (!isEmbedded)
                ((IdentifiableEntity)file).Save();

            StringBuilder sb = new StringBuilder();
            //Use plain javascript not to have to add also the reference to jquery in the result iframe
            sb.AppendLine("<html><head><title>-</title></head><body>");
            sb.AppendLine("<script type='text/javascript'>");
            RuntimeInfo ri = file is EmbeddedEntity ? new RuntimeInfo((EmbeddedEntity)file) : new RuntimeInfo((IIdentifiable)file);
            sb.AppendLine("window.parent.$.data(window.parent.document.getElementById('{0}'), 'SF-control').onUploaded('{1}', '{2}', '{3}', '{4}')".Formato(
                prefix,
                file.FileName,
                FilesClient.GetDownloadPath(file),
                ri.ToString(),
                isEmbedded ? Navigator.Manager.SerializeEntity((EmbeddedEntity)file) : null));
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return Content(sb.ToString());
        }

        public JsonNetResult UploadDropped()
        {
            string fileName = Request.Headers["X-FileName"];

            string prefix = Request.Headers["X-Prefix"];

            RuntimeInfo info = RuntimeInfo.FromFormValue((string)Request.Headers["X-" + TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            bool isEmbedded = info.EntityType.IsEmbeddedEntity(); 
            
            IFile file;
            if (info.EntityType == typeof(FilePathDN))
            {
                string fileType = (string)Request.Headers["X-" + FileLineKeys.FileType];
                if (!fileType.HasText())
                    throw new InvalidOperationException("Couldn't create FilePath with unknown FileType for file '{0}'".Formato(prefix));

                file = new FilePathDN(MultiEnumLogic<FileTypeDN>.ToEnum(fileType));
            }
            else
            {
                file = (IFile)Activator.CreateInstance(info.EntityType);
            }

            file.FileName = fileName;
            file.BinaryFile = Request.InputStream.ReadAllBytes();

            if (!isEmbedded)
                ((IdentifiableEntity)file).Save();

            RuntimeInfo ri = file is EmbeddedEntity ? new RuntimeInfo((EmbeddedEntity)file) : new RuntimeInfo((IIdentifiable)file);
            
            return this.JsonNet(new
            {
                file.FileName,
                FullWebPath = FilesClient.GetDownloadPath(file),
                RuntimeInfo = ri.ToString(),
                EntityState = isEmbedded ? Navigator.Manager.SerializeEntity((EmbeddedEntity)file) : null,
            }); 
        }

        public ActionResult Download(string file)
        {
            if (file == null)
                throw new ArgumentException("file");

            RuntimeInfo ri = RuntimeInfo.FromFormValue(file);

            if (ri.EntityType == typeof(FilePathDN))
            {
                FilePathDN fp = Database.Retrieve<FilePathDN>(ri.IdOrNull.Value);

                return File(fp.FullPhysicalPath, MimeType.FromFileName(fp.FullPhysicalPath), fp.FileName);
            }
            else
            {
                FileDN f = Database.Retrieve<FileDN>(ri.IdOrNull.Value);

                return new StaticContentResult(f.BinaryFile, f.FileName);
            }
        }
    }
}
