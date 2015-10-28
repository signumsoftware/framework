using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Reflection;
using Signum.Utilities;
using System.IO;
using System.Web.Hosting;
using System.Web.UI;
using System.Web;
using System.IO.Compression;
using Signum.React.PortableAreas;

namespace Signum.React.Controllers
{
    public class ResourcesController : Controller
    {
        public ActionResult GetFile(string file)
        {
            return FileRepositoryManager.GetFile("~/" + file);
        }
    }
}
