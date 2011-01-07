#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Files;
using System.Web.Routing;
using System.IO;
#endregion

namespace Signum.Web.Files
{
    public static class FilesClient
    {
        public static string ViewPrefix = "files/Views/";

        public static void Start(bool filePath, bool embeddedFile)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(FilesClient), "~/files/", "Signum.Web.Extensions.Files."));
                
                RouteTable.Routes.InsertRouteAt0("files/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "files" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                FileRepositoryDN.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (filePath)
                    Navigator.AddSetting(new EntitySettings<FilePathDN>(EntityType.Default));

                if (embeddedFile)
                    Navigator.AddSetting(new EmbeddedEntitySettings<EmbeddedFileDN>()
                    {
                        MappingDefault = new EntityMapping<EmbeddedFileDN>(true)
                        {
                            GetValue = ctx =>
                            {
                                RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                                if (runtimeInfo.RuntimeType == null)
                                    ctx.Value = null;
                                else
                                {
                                    if (runtimeInfo.IsNew)
                                    {
                                        ctx.Value = new EmbeddedFileDN();

                                        HttpPostedFileBase hpf = ctx.ControllerContext.HttpContext.Request.Files[ctx.ControlID] as HttpPostedFileBase;

                                        if (hpf.ContentLength != 0)
                                        {
                                            ctx.Value.FileName = Path.GetFileName(hpf.FileName);
                                            ctx.Value.BinaryFile = hpf.InputStream.ReadAllBytes();
                                        }
                                    }
                                }

                                return ctx.Value;
                            }
                        }
                    });
            }
        }
    }
}
