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

namespace Signum.Web.Files
{
    [HandleError]
    public class FileController : Controller
    {
        public ActionResult Upload()
        {
            FilePathDN fp = null;
            string formFieldId = "";
            foreach (string file in Request.Files)
            {
                if (((string)Request.Form[TypeContext.Compose(file, TypeContext.StaticType)]) != "FilePathDN")
                    continue;

                string idStr = (string)Request.Form[TypeContext.Compose(file, TypeContext.Id)];
                int id;
                if (int.TryParse(idStr, out id))
                    fp = Database.Retrieve<FilePathDN>(id);
                else
                {
                    string fileType = (string)Request.Form[TypeContext.Compose(file, FileLineKeys.FileType)];
                    if (!fileType.HasText())
                        throw new ApplicationException(Resources.CouldntCreateFilePathWithUnknownFileTypeForField0.Formato(file));
                    fp = new FilePathDN(EnumLogic<FileTypeDN>.ToEnum(fileType));
                }

                HttpPostedFileBase hpf = Request.Files[file] as HttpPostedFileBase;
                if (hpf.ContentLength == 0)
                    continue;
                formFieldId = file;

                fp.FileName = hpf.FileName;
                fp.BinaryFile = hpf.InputStream.ReadAllBytes();

                fp = fp.Save();
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><title>-</title></head><body>");
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var parDoc = window.parent.document;");
            
            if (fp.TryCS(f => f.IdOrNull) != null)
            {
                sb.AppendLine("parDoc.getElementById('{0}loading').style.display='none';".Formato(formFieldId));
                //sb.AppendLine("parDoc.getElementById('{0}').value='{1}';".Formato(TypeContext.Compose(formFieldId, EntityBaseKeys.ToStr), fp.FileName));
                //sb.AppendLine("$('#{0}').val('{1}');".Formato(TypeContext.Compose(formFieldId, EntityBaseKeys.ToStr), fp.FileName));
                sb.AppendLine("parDoc.getElementById('{0}').value='{1}';".Formato(TypeContext.Compose(formFieldId, EntityBaseKeys.ToStrLink), fp.FileName));                
                //sb.AppendLine("$('#{0}').val('{1}');".Formato(TypeContext.Compose(formFieldId, EntityBaseKeys.ToStrLink), fp.FileName));
                sb.AppendLine("parDoc.getElementById('{0}').value='FilePathDN';".Formato(TypeContext.Compose(formFieldId, TypeContext.RuntimeType)));
                //sb.AppendLine("$('#{0}').val('FilePathDN');".Formato(TypeContext.Compose(formFieldId, TypeContext.RuntimeType)));
                sb.AppendLine("parDoc.getElementById('{0}').value='{1}';".Formato(TypeContext.Compose(formFieldId, TypeContext.Id), fp.Id.ToString()));                
                //sb.AppendLine("$('#{0}').val({1});".Formato(TypeContext.Compose(formFieldId, TypeContext.Id), fp.Id));
                sb.AppendLine("parDoc.getElementById('div{0}New').style.display='none';".Formato(formFieldId));
                sb.AppendLine("parDoc.getElementById('div{0}Old').style.display='block';".Formato(formFieldId));
                //sb.AppendLine("$('document #div{0}New').hide(); $('#div{0}Old').show();".Formato(formFieldId));
            }
            else
                sb.AppendLine("window.alert('ERROR');");

            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return Content(sb.ToString());
        }
    }
}
