using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Files;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.Utilities;
using System.IO;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using System.Diagnostics;
using System.Web;
using System.Linq.Expressions;
using Signum.Engine.Operations;
using Signum.Utilities.Reflection;
using Signum.Entities.Isolation;

namespace Signum.Engine.Files
{
    public static class FilePathLogic
    {
        static Expression<Func<FilePathEntity, WebImage>> WebImageExpression =
            fp => new WebImage { FullWebPath = fp.FullWebPath };
        public static WebImage WebImage(this FilePathEntity fp)
        {
            return WebImageExpression.Evaluate(fp);
        }

        static Expression<Func<FilePathEntity, WebDownload>> WebDownloadExpression =
           fp => new WebDownload { FullWebPath = fp.FullWebPath };
        public static WebDownload WebDownload(this FilePathEntity fp)
        {
            return WebDownloadExpression.Evaluate(fp);
        }
        
        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FileTypeLogic.Start(sb, dqm);

                sb.Include<FilePathEntity>();

                sb.Schema.EntityEvents<FilePathEntity>().Retrieved += FilePathLogic_Retrieved;
                sb.Schema.EntityEvents<FilePathEntity>().PreSaving += FilePath_PreSaving;
                sb.Schema.EntityEvents<FilePathEntity>().PreUnsafeDelete += new PreUnsafeDeleteHandler<FilePathEntity>(FilePathLogic_PreUnsafeDelete);

                dqm.RegisterQuery(typeof(FilePathEntity), () =>
                    from p in Database.Query<FilePathEntity>()
                    select new
                    {
                        Entity = p,
                        p.Id,
                        p.FileName,
                        p.FileType,
                        p.Sufix
                    });

                new Graph<FilePathEntity>.Execute(FilePathOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (fp, _) =>
                    {
                        if (!fp.IsNew)
                        {

                            var ofp = fp.ToLite().Retrieve();


                            if (fp.FileName != ofp.FileName || fp.Sufix != ofp.Sufix || fp.FullPhysicalPath != ofp.FullPhysicalPath)
                            {
                                using (Transaction tr = new Transaction())
                                {
                                    var preSufix = ofp.Sufix.Substring(0, ofp.Sufix.Length - ofp.FileName.Length);
                                    fp.Sufix = Path.Combine(preSufix, fp.FileName);
                                    fp.Save();
                                    System.IO.File.Move(ofp.FullPhysicalPath, fp.FullPhysicalPath);
                                    tr.Commit();
                                }
                            }
                        }
                    }
                }.Register();

                OperationLogic.SetProtectedSave<FilePathEntity>(false);

                sb.AddUniqueIndex<FilePathEntity>(f => new { f.Sufix, f.FileType }); //With mixins, add AttachToUniqueIndexes to field

                dqm.RegisterExpression((FilePathEntity fp) => fp.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                dqm.RegisterExpression((FilePathEntity fp) => fp.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");

            }
        }

      

        static void FilePathLogic_Retrieved(FilePathEntity fp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                fp.SetPrefixPair();
        }

        public static FilePathEntity SetPrefixPair(this FilePathEntity fp)
        {
            fp.prefixPair = FileTypeLogic.FileTypes.GetOrThrow(fp.FileType).GetPrefixPair(fp);

            return fp;
        }

        public static void FilePathLogic_PreUnsafeDelete(IQueryable<FilePathEntity> query)
        {
            if (!unsafeMode.Value)
            {
                var list = query.ToList();

                Transaction.PostRealCommit += ud =>
                {
                    foreach (var gr in list.GroupBy(f=>f.FileType))
                    {
                        var alg = FileTypeLogic.FileTypes.GetOrThrow(gr.Key);
                        if (alg.TakesOwnership)
                            alg.DeleteFiles(gr.Cast<IFilePath>().ToList());
                    }
                };
            }
        }

        static readonly Variable<bool> unsafeMode = Statics.ThreadVariable<bool>("filePathUnsafeMode");

        public static IDisposable UnsafeMode()
        {
            if (unsafeMode.Value) return null;
            unsafeMode.Value = true;
            return new Disposable(() => unsafeMode.Value = false);
        }

        public static void FilePath_PreSaving(FilePathEntity fp, ref bool graphModified)
        {
            if (fp.IsNew && !unsafeMode.Value)
            {
                FileTypeLogic.SaveFile(fp);
            }
        }

        public static byte[] GetByteArray(this FilePathEntity fp)
        {
            return fp.BinaryFile ?? File.ReadAllBytes(fp.FullPhysicalPath);
        }

        public static byte[] GetByteArray(this Lite<FilePathEntity> fp)
        {
            return File.ReadAllBytes(fp.InDB(f => f.FullPhysicalPath));
        }
    }
}
