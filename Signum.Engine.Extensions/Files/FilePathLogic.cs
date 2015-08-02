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
        
        public static Dictionary<FileTypeSymbol, FileTypeAlgorithm> FileTypes = new Dictionary<FileTypeSymbol, FileTypeAlgorithm>();

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<FilePathEntity>();

                SymbolLogic<FileTypeSymbol>.Start(sb, () => FileTypes.Keys.ToHashSet());

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
                        p.FullPhysicalPath, //The whole entity is retrieved but is lightweight anyway
                        p.FullWebPath
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

                dqm.RegisterQuery(typeof(FileTypeSymbol), () =>
                    from f in Database.Query<FileTypeSymbol>()
                    select new
                    {
                        Entity = f,
                        f.Key
                    });

                sb.AddUniqueIndex<FilePathEntity>(f => new { f.Sufix, f.FileType }); //With mixins, add AttachToUniqueIndexes to field

                dqm.RegisterExpression((FilePathEntity fp) => fp.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                dqm.RegisterExpression((FilePathEntity fp) => fp.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");

                sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
            }
        }

        static void Schema_SchemaCompleted()
        {
            var errors = (from kvp in FileTypes
                          let error = kvp.Value.Errors()
                          where error.HasText()
                          select kvp.Key + ": " + error.Indent(4)).ToList();

            if (errors.Any())
                throw new InvalidOperationException("Errors in the following FileType algorithms: \r\n" +
                    errors.ToString("\r\n").Indent(4));
        }

        static void FilePathLogic_Retrieved(FilePathEntity fp)
        {
            fp.SetPrefixPair();
        }

        public static FilePathEntity SetPrefixPair(this FilePathEntity fp)
        {
            fp.prefixPair = FilePathLogic.FileTypes.GetOrThrow(fp.FileType).GetPrefixPair(fp);

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
                        var alg = FileTypes.GetOrThrow(gr.Key);
                        if (alg.TakesOwnership)
                            alg.DeleteFiles(gr.ToList());
                    }
                };
            }
        }
        

        public static void DeleteFilesDefault(List<FilePathEntity> filePaths)
        {
            foreach (var fp in filePaths)
            {
                File.Delete(fp.FullPhysicalPath);
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
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    FileTypeAlgorithm alg = FileTypes.GetOrThrow(fp.FileType);

                    if(alg.TakesOwnership)
                    {
                        string sufix = alg.CalculateSufix(fp);
                        if (!sufix.HasText())
                            throw new InvalidOperationException("Sufix not set");

                        fp.prefixPair = alg.GetPrefixPair(fp);

                        int i = 2;
                        fp.Sufix = sufix;
                        while (alg.RenameOnCollision && File.Exists(fp.FullPhysicalPath))
                        {
                            fp.Sufix = alg.RenameAlgorithm(sufix, i);
                            i++;
                        }

                        alg.SaveFile(fp);
                    }
                }
            }
        }

        
        public static void SaveFileDefault(FilePathEntity fp)
        {
            string fullPhysicalPath = null;
            try
            {
                string path = Path.GetDirectoryName(fp.FullPhysicalPath);
                fullPhysicalPath = path;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllBytes(fp.FullPhysicalPath, fp.BinaryFile);
                fp.BinaryFile = null;
            }
            catch (IOException ex)
            {
                ex.Data.Add("FullPhysicalPath", fullPhysicalPath);
                ex.Data.Add("CurrentPrincipal", System.Threading.Thread.CurrentPrincipal.Identity.Name);

                throw;
            }
        }

        public static void Register(FileTypeSymbol fileTypeSymbol, FileTypeAlgorithm algorithm)
        {
            if (fileTypeSymbol == null)
                throw new ArgumentNullException(nameof(fileTypeSymbol));

            FileTypes.Add(fileTypeSymbol, algorithm);
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

    public sealed class FileTypeAlgorithm
    {
        public Func<FilePathEntity, PrefixPair> GetPrefixPair { get; set; }
        public Func<FilePathEntity, string> CalculateSufix { get; set; }

        public bool RenameOnCollision {get; set;}
        public bool TakesOwnership { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public Action<FilePathEntity> SaveFile;
        public Action<List<FilePathEntity>> DeleteFiles;

        public FileTypeAlgorithm()
        {
            TakesOwnership = true;
            CalculateSufix = FileName_Sufix;

            RenameOnCollision = true;        
            RenameAlgorithm = DefaultRenameAlgorithm;

            SaveFile = FilePathLogic.SaveFileDefault;
            DeleteFiles = FilePathLogic.DeleteFilesDefault;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix),
              "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        public static readonly Func<FilePathEntity, string> FileName_Sufix = (FilePathEntity fp) => fp.FileName;

        public static readonly Func<FilePathEntity, string> Year_FileName_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), fp.FileName);
        public static readonly Func<FilePathEntity, string> Year_Month_FileName_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), fp.FileName));

        public static readonly Func<FilePathEntity, string> Year_GuidExtension_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
        public static readonly Func<FilePathEntity, string> Year_Month_GuidExtension_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName)));

        public static readonly Func<FilePathEntity, string> YearMonth_Guid_Filename_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));
        public static readonly Func<FilePathEntity, string> Isolated_YearMonth_Guid_Filename_Sufix = (FilePathEntity fp) => Path.Combine(IsolationEntity.Current.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));


        public string Errors()
        {
            string error = null;

            if (GetPrefixPair == null)
                error = "GetPrefixPair";

            if (TakesOwnership && CalculateSufix == null)
                error = ", ".CombineIfNotEmpty(error,  "CalculateSufix");

            if (RenameOnCollision && RenameAlgorithm == null)
                error = ", ".CombineIfNotEmpty(error, "RenameAlgorithm");

            if (error.HasText())
                error += " not set";

            return error;
        }

    }
}
