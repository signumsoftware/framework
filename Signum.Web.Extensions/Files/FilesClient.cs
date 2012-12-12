#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
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
using System.Linq.Expressions;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
#endregion

namespace Signum.Web.Files
{
    public static class FilesClient
    {
        public static string ViewPrefix = "~/Files/Views/{0}.cshtml";

        public static void Start(bool filePath, bool file, bool embeddedFile)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(FilesClient));

                FileRepositoryDN.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (filePath)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<FileRepositoryDN>(EntityType.Main){ PartialViewName = e => ViewPrefix.Formato("FileRepository")},
                        new EntitySettings<FilePathDN>(EntityType.SharedPart),
                        new EntitySettings<FileTypeDN>(EntityType.SystemString),
                    });

                    var es = Navigator.EntitySettings<FilePathDN>();
                     
                    var baseMapping = (Mapping<FilePathDN>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingLine = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo.RuntimeType == null)
                            return null;
                        else
                        {
                            if (runtimeInfo.IsNew)
                            {
                                HttpPostedFileBase hpf = GetHttpRequestFile(ctx);
                                if (hpf != null)
                                {
                                    string fileType = ctx.Inputs[FileLineKeys.FileType];
                                    return new FilePathDN(MultiEnumLogic<FileTypeDN>.ToEnum(fileType))
                                    {
                                        FileName = Path.GetFileName(hpf.FileName),
                                        BinaryFile = hpf.InputStream.ReadAllBytes(),
                                    };
                                }
                                else
                                {
                                    return (FilePathDN)GetSessionFile(ctx);
                                }
                            }
                        }

                        return baseMapping(ctx);
                    };

                    es.MappingMain = es.MappingLine;

                    var lm = new LiteMapping<FilePathDN>();
                    lm.EntityHasChanges = ctx => ctx.GetRuntimeInfo().IsNew;
                    Mapping.RegisterValue<Lite<FilePathDN>>(lm.GetValue);
                }

                if (file)
                {
                    var es = new EntitySettings<FileDN>(EntityType.SharedPart);
                    Navigator.AddSetting(es);

                    var baseMapping = (Mapping<FileDN>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingLine = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo.RuntimeType == null)
                            return null;
                        else
                        {
                            if (runtimeInfo.IsNew)
                            {
                                HttpPostedFileBase hpf = GetHttpRequestFile(ctx);

                                if (hpf != null)
                                {
                                    return new FileDN
                                    {
                                        FileName = Path.GetFileName(hpf.FileName),
                                        BinaryFile = hpf.InputStream.ReadAllBytes()
                                    };
                                }
                                else
                                {
                                    return (FileDN)GetSessionFile(ctx);
                                }
                            }
                        }

                        return baseMapping(ctx);
                    };

                    FileLogic.DownloadFileUrl = DownloadFileUrl;

                    var lm = new LiteMapping<FileDN>();
                    lm.EntityHasChanges = ctx => ctx.GetRuntimeInfo().IsNew;
                    Mapping.RegisterValue<Lite<FileDN>>(lm.GetValue);
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
                            HttpPostedFileBase hpf = GetHttpRequestFile(ctx);

                            if (hpf != null && hpf.ContentLength != 0)
                            {
                                return new EmbeddedFileDN()
                                {
                                    FileName = Path.GetFileName(hpf.FileName),
                                    BinaryFile = hpf.InputStream.ReadAllBytes()
                                };
                            }
                            else
                            {
                                var sessionFile = (EmbeddedFileDN)GetSessionFile(ctx);
                                if (sessionFile != null)
                                    return sessionFile;
                                else 
                                    return baseMapping(ctx);
                            }
                        }
                    };
                }

                var dqm = DynamicQueryManager.Current;
               
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

        private static object GetSessionFile(MappingContext ctx)
        {
            return ctx.ControllerContext.HttpContext.Session[Navigator.TabID(ctx.ControllerContext.Controller) + ctx.ControlID];
        }

        private static HttpPostedFileBase GetHttpRequestFile(MappingContext ctx)
        {
            string fileKey = TypeContextUtilities.Compose(ctx.ControlID, FileLineKeys.File);
            HttpPostedFileBase hpf = ctx.ControllerContext.HttpContext.Request.Files[fileKey] as HttpPostedFileBase;
            return hpf;
        }


        static string DownloadFileUrl(Lite<FileDN> file)
        {
            if (file == null)
                return null;

            return RouteHelper.New().Action((FileController fc) => fc.Download(new RuntimeInfo(file).ToString())); 
        }
    }

}
