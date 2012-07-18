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
using Signum.Engine.Basics;
using Signum.Engine.Files;
using Signum.Engine;
#endregion

namespace Signum.Web.Files
{
    public static class FilesClient
    {
        public static string ViewPrefix = "~/Files/Views/{0}.cshtml";

        public static void Start(bool filePath, bool embeddedFile)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(FilesClient));

                FileRepositoryDN.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (filePath)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<FileRepositoryDN>(EntityType.Admin){PartialViewName = e => ViewPrefix.Formato("FileRepository")},
                        new EntitySettings<FilePathDN>(EntityType.Default),
                        new EntitySettings<FileTypeDN>(EntityType.ServerOnly),
                    });

                    var es = Navigator.EntitySettings<FilePathDN>();
                     
                    var baseMapping = (Mapping<FilePathDN>)es.MappingDefault.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingDefault = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo.RuntimeType == null)
                            return null;
                        else
                        {
                            if (runtimeInfo.IsNew)
                            {
                                string fileType = ctx.Inputs[FileLineKeys.FileType];
                                var fp = new FilePathDN(EnumLogic<FileTypeDN>.ToEnum(fileType));

                                //string fileKey = TypeContextUtilities.Compose(ctx.ControlID, FileLineKeys.File);
                                HttpPostedFileBase hpf = ctx.ControllerContext.HttpContext.Request.Files[ctx.ControlID] as HttpPostedFileBase;
                                if (hpf != null)
                                {
                                    fp.FileName = Path.GetFileName(hpf.FileName);
                                    fp.BinaryFile = hpf.InputStream.ReadAllBytes();

                                    return fp;
                                }
                                else
                                {
                                    FilePathDN filePathInSession = (FilePathDN)ctx.ControllerContext.HttpContext.Session[Navigator.TabID(ctx.ControllerContext.Controller) + ctx.ControlID];
                                    return filePathInSession;
                                }
                            }
                        }

                        return baseMapping(ctx);
                    };

                    es.MappingAdmin = es.MappingDefault;
                }

                if (embeddedFile)
                {
                    var es = new EmbeddedEntitySettings<EmbeddedFileDN>();
                    Navigator.AddSetting(es);

                    var baseMapping = (Mapping<EmbeddedFileDN>) es.MappingDefault.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingDefault = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo.RuntimeType == null)
                            return null;
                        else
                        {
                            if (runtimeInfo.IsNew)
                            {
                                var result = new EmbeddedFileDN();

                                //string fileKey = TypeContextUtilities.Compose(ctx.ControlID, FileLineKeys.File);
                                HttpPostedFileBase hpf = ctx.ControllerContext.HttpContext.Request.Files[ctx.ControlID] as HttpPostedFileBase;

                                if (hpf.ContentLength != 0)
                                {
                                    result.FileName = Path.GetFileName(hpf.FileName);
                                    result.BinaryFile = hpf.InputStream.ReadAllBytes();
                                }

                                return result;
                            }
                        }

                        return baseMapping(ctx);
                    };
                }


                QuerySettings.FormatRules.Add(new FormatterRule(
                       col => col.Type == typeof(WebImage),
                       col => (help, obj) => ((WebImage)obj).FullWebPath == null ? null :
                           new MvcHtmlString("<img src='" +
                               RouteHelper.New().Content(((WebImage)obj).FullWebPath) +
                               "' alt='" + typeof(WebImage).NiceName() + "' class='sf-search-control-image' />")
                 ));


                QuerySettings.FormatRules.Add(new FormatterRule(
                       col => col.Type == typeof(WebDownload),
                       col => (help, obj) => ((WebDownload)obj).FullWebPath == null ? null :
                          new MvcHtmlString("<a href='{0}'>{1}</a>".Formato(RouteHelper.New().Content(((WebDownload)obj).FullWebPath), typeof(WebDownload).NiceName()))
                ));

            }

        }
    }
}
