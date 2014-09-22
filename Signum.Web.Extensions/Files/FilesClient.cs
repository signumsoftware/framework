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
using Signum.Web.PortableAreas;
#endregion

namespace Signum.Web.Files
{
    public static class FilesClient
    {
        public static string ViewPrefix = "~/Files/Views/{0}.cshtml";

        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Files/Scripts/Files");

        public static void Start(bool filePath, bool file, bool embeddedFile)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(FilesClient));

                FileRepositoryDN.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                UrlsRepository.DefaultSFUrls.AddRange(new Dictionary<string, Func<UrlHelper, string>>
                {
                    { "uploadFile", url=>url.Action<FileController>(fc => fc.Upload()) },
                    { "uploadDroppedFile", url=>url.Action<FileController>(fc => fc.UploadDropped()) },
                    { "downloadFile", url=>url.Action("Download", "File") },
                });  

                if (filePath)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<FileRepositoryDN>{ PartialViewName = e => ViewPrefix.Formato("FileRepository")},
                        new EntitySettings<FilePathDN>(),
                        new EntitySettings<FileTypeSymbol>(),
                    });

                    var es = Navigator.EntitySettings<FilePathDN>();
                     
                    var baseMapping = (Mapping<FilePathDN>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingLine = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo == null)
                            return null;
                        else
                        {
                            if (runtimeInfo.IsNew)
                            {
                                HttpPostedFileBase hpf = GetHttpRequestFile(ctx);
                                if (hpf != null)
                                {
                                    string fileType = ctx.Inputs[FileLineKeys.FileType];
                                    return new FilePathDN(SymbolLogic<FileTypeSymbol>.ToSymbol(fileType))
                                    {
                                        FileName = Path.GetFileName(hpf.FileName),
                                        BinaryFile = hpf.InputStream.ReadAllBytes(),
                                    };
                                }
                                else
                                {
                                    throw new InvalidOperationException("Impossible to create new FilePath {0}".Formato(ctx.Prefix));
                                }
                            }
                            else
                                return baseMapping(ctx);
                             
                        }

                      
                    };

                    es.MappingMain = es.MappingLine;

                    var lm = new LiteMapping<FilePathDN>();
                    lm.EntityHasChanges = ctx => ctx.GetRuntimeInfo().IsNew;
                    Mapping.RegisterValue<Lite<FilePathDN>>(lm.GetValue);
                }

                if (file)
                {
                    var es = new EntitySettings<FileDN>();
                    Navigator.AddSetting(es);

                    var baseMapping = (Mapping<FileDN>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

                    es.MappingLine = ctx =>
                    {
                        RuntimeInfo runtimeInfo = ctx.GetRuntimeInfo();
                        if (runtimeInfo == null)
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
                                    throw new InvalidOperationException("Impossible to create new FileDN {0}".Formato(ctx.Prefix));
                                }
                            }
                            else
                                return baseMapping(ctx);
                        }

                        
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
                        if (runtimeInfo == null)
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
                            else if (ctx.Inputs.ContainsKey(EntityBaseKeys.EntityState))
                            {
                                return (EmbeddedFileDN)Navigator.Manager.DeserializeEntity(ctx.Inputs[EntityBaseKeys.EntityState]);
                            }
                            else
                                return baseMapping(ctx);
                        }
                    };
                }

                var dqm = DynamicQueryManager.Current;
               
                QuerySettings.FormatRules.Add(new FormatterRule("WebImage",
                       col => col.Type == typeof(WebImage),
                       col => new CellFormatter((help, obj) => ((WebImage)obj).FullWebPath == null ? null :
                           new HtmlTag("img")
                           .Attr("src", RouteHelper.New().Content(((WebImage)obj).FullWebPath))
                           .Attr("alt", typeof(WebImage).NiceName())
                           .Attr("style", "width:80px").ToHtmlSelf()) { TextAlign = "center" }
                 ));

                QuerySettings.FormatRules.Add(new FormatterRule("WebDownload",
                       col => col.Type == typeof(WebDownload),
                       col => new CellFormatter((help, obj) => ((WebDownload)obj).FullWebPath == null ? null :
                          new MvcHtmlString("<a href='{0}'>{1}</a>".Formato(RouteHelper.New().Content(((WebDownload)obj).FullWebPath), typeof(WebDownload).NiceName()))) { TextAlign = "center" }
                ));

            }

        }

        private static HttpPostedFileBase GetHttpRequestFile(MappingContext ctx)
        {
            string fileKey = TypeContextUtilities.Compose(ctx.Prefix, FileLineKeys.File);
            HttpPostedFileBase hpf = ctx.Controller.ControllerContext.HttpContext.Request.Files[fileKey] as HttpPostedFileBase;
            return hpf;
        }


        static string DownloadFileUrl(Lite<FileDN> file)
        {
            if (file == null)
                return null;

            return RouteHelper.New().Action((FileController fc) => fc.Download( new RuntimeInfo(file).ToString())); 
        }

        public static string GetDownloadPath(IFile file)
        {
            var webPath = file.FullWebPath;
            if (webPath.HasText())
                return RouteHelper.New().Content(webPath);

            if (file is FilePathDN || file is FileDN)
                return RouteHelper.New().Action((FileController fc) => fc.Download(new RuntimeInfo((IdentifiableEntity)file).ToString()));

            return null;
        }

    }

}
