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
#endregion

namespace Signum.Web.Files
{
    public static class FilesClient
    {
        public static void Start(bool filePath, bool embeddedFile)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(FilesClient));

                FileRepositoryDN.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (filePath)
                {
                    var es = new EntitySettings<FilePathDN>(EntityType.Default);
                    Navigator.AddSetting(es);
                     
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

                                HttpPostedFileBase hpf = ctx.ControllerContext.HttpContext.Request.Files[ctx.ControlID] as HttpPostedFileBase;

                                fp.FileName = Path.GetFileName(hpf.FileName);
                                fp.BinaryFile = hpf.InputStream.ReadAllBytes();

                                return fp;
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
            }

            QuerySettings.FormatRules.Add(new FormatterRule(
                   col => col.Type == typeof(WebImage),
                   col => (help, obj) => ((WebImage)obj).FullWebPath == null ? null :
                       new MvcHtmlString("<img src='" + 
                           RouteHelper.New().Content(((WebImage)obj).FullWebPath) +
                           "' alt='" + typeof(WebImage).NiceName() + "' class='sf-search-control-image' />")
             ));
        }
    }
}
