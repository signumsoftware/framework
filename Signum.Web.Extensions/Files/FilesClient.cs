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

                FileRepositoryEntity.OverridenPhisicalCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                UrlsRepository.DefaultSFUrls.AddRange(new Dictionary<string, Func<UrlHelper, string>>
                {
                    { "uploadFile", url=>url.Action<FileController>(fc => fc.Upload()) },
                    { "uploadDroppedFile", url=>url.Action<FileController>(fc => fc.UploadDropped()) },
                });  

                if (filePath)
                {
                    RegisterFileConstructor<FilePathEntity>(data =>
                    {
                        if (!data.FileName.HasText())
                            throw new InvalidOperationException("Couldn't create FilePath with unknown FileType for file '{0}'".FormatWith(data.FileName));

                        return new FilePathEntity(SymbolLogic<FileTypeSymbol>.ToSymbol(data.FileType)) { FileName = data.FileName, BinaryFile = data.Content }.Save();
                    });

                    RegisterDownloadUrlConstructor<FilePathEntity>(fp =>
                    {
                        return RouteHelper.New().Action((FileController fc) => fc.Download(new RuntimeInfo(fp).ToString()));
                    });

                    RegisterFileDownloadResult<FilePathEntity>(ri =>
                    {
                        FilePathEntity fp = (FilePathEntity)ri.ToLite().Retrieve();

                        return new FilePathResult(fp.FullPhysicalPath, MimeType.FromFileName(fp.FullPhysicalPath)) { FileDownloadName = fp.FileName };
                    });

                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<FileRepositoryEntity>{ PartialViewName = e => ViewPrefix.FormatWith("FileRepository")},
                        new EntitySettings<FilePathEntity>{ PartialViewName = e => ViewPrefix.FormatWith("FilePath")},
                        new EntitySettings<FileTypeSymbol>(),
                    });

                    var es = Navigator.EntitySettings<FilePathEntity>();
                     
                    var baseMapping = (Mapping<FilePathEntity>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

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
                                    return new FilePathEntity(SymbolLogic<FileTypeSymbol>.ToSymbol(fileType))
                                    {
                                        FileName = Path.GetFileName(hpf.FileName),
                                        BinaryFile = hpf.InputStream.ReadAllBytes(),
                                    };
                                }
                                else
                                {
                                    throw new InvalidOperationException("Impossible to create new FilePath {0}".FormatWith(ctx.Prefix));
                                }
                            }
                            else
                                return baseMapping(ctx);
                        }
                    };

                    es.MappingMain = es.MappingLine;

                    var lm = new LiteMapping<FilePathEntity>();
                    lm.EntityHasChanges = ctx => ctx.GetRuntimeInfo().IsNew;
                    Mapping.RegisterValue<Lite<FilePathEntity>>(lm.GetValue);
                }

                if (file)
                {
                    RegisterFileConstructor<FileEntity>(data =>
                    {
                        return new FileEntity { FileName = data.FileName, BinaryFile = data.Content }.Save();
                    });

                    RegisterDownloadUrlConstructor<FileEntity>(fp =>
                    {
                        return RouteHelper.New().Action((FileController fc) => fc.Download(new RuntimeInfo(fp).ToString()));
                    });

                    RegisterFileDownloadResult<FileEntity>(ri =>
                    {
                        FileEntity f = (FileEntity)ri.ToLite().Retrieve();

                        return new StaticContentResult(f.BinaryFile, f.FileName);
                    });


                    var es = new EntitySettings<FileEntity>();
                    Navigator.AddSetting(es);

                    var baseMapping = (Mapping<FileEntity>)es.MappingLine.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

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
                                    return new FileEntity
                                    {
                                        FileName = Path.GetFileName(hpf.FileName),
                                        BinaryFile = hpf.InputStream.ReadAllBytes()
                                    };
                                }
                                else
                                {
                                    throw new InvalidOperationException("Impossible to create new FileEntity {0}".FormatWith(ctx.Prefix));
                                }
                            }
                            else
                                return baseMapping(ctx);
                        }
                    };

                    FileLogic.DownloadFileUrl = DownloadFileUrl;

                    var lm = new LiteMapping<FileEntity>();
                    lm.EntityHasChanges = ctx => ctx.GetRuntimeInfo().IsNew;
                    Mapping.RegisterValue<Lite<FileEntity>>(lm.GetValue);
                }

                if (embeddedFile)
                {
                    RegisterFileConstructor<EmbeddedFileEntity>(data =>
                    {
                        return new EmbeddedFileEntity { FileName = data.FileName, BinaryFile = data.Content };
                    });

                    var es = new EmbeddedEntitySettings<EmbeddedFileEntity>();
                    Navigator.AddSetting(es);

                    var baseMapping = (Mapping<EmbeddedFileEntity>) es.MappingDefault.AsEntityMapping().RemoveProperty(fp => fp.BinaryFile);

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
                                return new EmbeddedFileEntity()
                                {
                                    FileName = Path.GetFileName(hpf.FileName),
                                    BinaryFile = hpf.InputStream.ReadAllBytes()
                                };
                            }
                            else if (ctx.Inputs.ContainsKey(EntityBaseKeys.EntityState))
                            {
                                return (EmbeddedFileEntity)Navigator.Manager.DeserializeEntity(ctx.Inputs[EntityBaseKeys.EntityState]);
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
                          new MvcHtmlString("<a href='{0}'>{1}</a>".FormatWith(RouteHelper.New().Content(((WebDownload)obj).FullWebPath), typeof(WebDownload).NiceName()))) { TextAlign = "center" }
                ));

            }

        }

        public static HttpPostedFileBase GetHttpRequestFile(MappingContext ctx)
        {
            string fileKey = TypeContextUtilities.Compose(ctx.Prefix, FileLineKeys.File);
            HttpPostedFileBase hpf = ctx.Controller.ControllerContext.HttpContext.Request.Files[fileKey] as HttpPostedFileBase;
            return hpf;
        }

        static string DownloadFileUrl(Lite<FileEntity> file)
        {
            if (file == null)
                return null;

            return RouteHelper.New().Action((FileController fc) => fc.Download( new RuntimeInfo(file).ToString())); 
        }

      


        public static Dictionary<Type, Func<UploadedFileData, IFile>> FileConstructors = new Dictionary<Type, Func<UploadedFileData, IFile>>();

        public static void RegisterFileConstructor<T>(Func<UploadedFileData, T> fileConstructor) where T : class, IFile
        {
            FileConstructors.Add(typeof(T), fileConstructor);
        }

        public static IFile ConstructFile(Type type, UploadedFileData data)
        {
            return FileConstructors.GetOrThrow(type)(data);
        }



        public static Dictionary<Type, Func<IFile, string>> DownloadUrlConstructors = new Dictionary<Type, Func<IFile, string>>();

        public static void RegisterDownloadUrlConstructor<T>(Func<T, string> fileUrlConstructor) where T : class, IFile
        {
            DownloadUrlConstructors.Add(typeof(T), file => fileUrlConstructor((T)file));
        }

        public static string GetDownloadUrl(IFile file)
        {
            var webPath = file.FullWebPath;
            if (webPath.HasText())
                return RouteHelper.New().Content(webPath);

            var ctor = DownloadUrlConstructors.TryGetC(file.GetType());

            if (ctor == null)
                return null;

            return ctor(file);
        }


        public static Dictionary<Type, Func<RuntimeInfo, ActionResult>> FileDownloadResult = new Dictionary<Type, Func<RuntimeInfo, ActionResult>>();

        public static void RegisterFileDownloadResult<T>(Func<RuntimeInfo, ActionResult> fileDownloader) where T : class, IFile
        {
            FileDownloadResult.Add(typeof(T), fileDownloader);
        }

        public static ActionResult DownloadFileResult(RuntimeInfo runtimeInfo)
        {
            return FileDownloadResult.GetOrThrow(runtimeInfo.EntityType)(runtimeInfo);
        }
    }

    public class UploadedFileData
    {
        public string FileName;
        public byte[] Content;
        public string FileType;
        public string ExtraData;
    }
}
