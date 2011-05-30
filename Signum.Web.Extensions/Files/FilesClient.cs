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
                    Navigator.AddSetting(new EntitySettings<FilePathDN>(EntityType.Default)
                    {
                        MappingDefault = new EntityMapping<FilePathDN>(true)
                        {
                            GetEntity = ctx =>
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

                                return ctx.Value;
                            }
                        }
                    });
                }

                if (embeddedFile)
                {
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
}
