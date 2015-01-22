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
                        p.FullPhysicalPath,
                        p.FullWebPath,
                        p.Repository
                    });

                new Graph<FilePathEntity>.Execute(FilePathOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (fp, _) =>
                    {
                        if (!fp.IsNew)
                        {
                            var originalData = fp.ToLite().InDB(f => new { FileName = f.FileName, Sufix = f.Sufix, FullPhysicalPath = f.FullPhysicalPath });

                            if (fp.FileName != originalData.FileName || fp.Sufix != originalData.Sufix || fp.FullPhysicalPath != originalData.FullPhysicalPath)
                            {
                                using (Transaction tr = new Transaction())
                                {
                                    var preSufix = originalData.Sufix.Substring(0, originalData.Sufix.Length - originalData.FileName.Length);
                                    fp.Sufix = Path.Combine(preSufix, fp.FileName);
                                    fp.Save();
                                    System.IO.File.Move(originalData.FullPhysicalPath, fp.FullPhysicalPath);
                                    tr.Commit();
                                }
                            }
                        }
                    }
                }.Register();

                OperationLogic.SetProtectedSave<FilePathEntity>(false);

                dqm.RegisterQuery(typeof(FileRepositoryEntity), () =>
                    from r in Database.Query<FileRepositoryEntity>()
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.Name,
                        r.Active,
                        r.PhysicalPrefix,
                        r.WebPrefix
                    });
                
                dqm.RegisterQuery(typeof(FileTypeSymbol), () =>
                    from f in Database.Query<FileTypeSymbol>()
                    select new
                    {
                        Entity = f,
                        f.Key
                    });

                sb.AddUniqueIndex<FilePathEntity>(f => new { f.Sufix, f.Repository });

                dqm.RegisterExpression((FilePathEntity fp) => fp.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                dqm.RegisterExpression((FilePathEntity fp) => fp.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");

                new Graph<FileRepositoryEntity>.Execute(FileRepositoryOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (fr, _) => { }
                }.Register();
            }
        }

        public static void FilePathLogic_PreUnsafeDelete(IQueryable<FilePathEntity> query)
        {
            var list = query.Select(a => a.FullPhysicalPath).ToList();

            Transaction.PostRealCommit += ud =>
            {
                foreach (var fullPath in list)
                {
                    if (unsafeMode.Value)
                        Debug.WriteLine(fullPath);
                    else
                        File.Delete(fullPath);
                }
            };
        }

        static readonly Variable<bool> unsafeMode = Statics.ThreadVariable<bool>("filePathUnsafeMode");

        public static IDisposable UnsafeMode()
        {
            if (unsafeMode.Value) return null;
            unsafeMode.Value = true;
            return new Disposable(() => unsafeMode.Value = false);
        }

        public static FilePathEntity UnsafeLoad(FileRepositoryEntity repository, FileTypeSymbol fileType, string fullPath)
        {
            if (!fullPath.StartsWith(repository.FullPhysicalPrefix, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("The File {0} doesn't belong to the repository {1}".FormatWith(fullPath, repository.PhysicalPrefix));

            return new FilePathEntity
            {
                FileLength = (int)new FileInfo(fullPath).Length,
                FileType = fileType,
                Sufix = fullPath.Substring(repository.FullPhysicalPrefix.Length).TrimStart('\\'),
                FileName = Path.GetFileName(fullPath),
                Repository = repository,
            };
        }

        public static void FilePath_PreSaving(FilePathEntity fp, ref bool graphModified)
        {
            if (fp.IsNew && !unsafeMode.Value)
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    FileTypeAlgorithm alg = FileTypes.GetOrThrow(fp.FileType);
                    string sufix = alg.CalculateSufix(fp);
                    if (!sufix.HasText())
                        throw new InvalidOperationException("Sufix not set");

                    do
                    {
                        fp.Repository = alg.GetRepository(fp);
                        if (fp.Repository == null)
                            throw new InvalidOperationException("Repository not set");
                        int i = 2;
                        fp.Sufix = sufix;
                        while (File.Exists(fp.FullPhysicalPath) && alg.RenameOnCollision)
                        {
                            fp.Sufix = alg.RenameAlgorithm(sufix, i);
                            i++;
                        }
                    }
                    while (!SaveFile(fp));
                }
            }


        }

        const long ERROR_DISK_FULL = 112L;

        private static bool SaveFile(FilePathEntity fp)
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

                int hresult = (int)ex.GetType().GetField("_HResult",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ex); // The error code is stored in just the lower 16 bits
                if ((hresult & 0xFFFF) == ERROR_DISK_FULL)
                {
                    fp.Repository.Active = false;
                    using (OperationLogic.AllowSave<FileRepositoryEntity>())
                        Database.Save(fp.Repository);
                    return false;
                }
                else
                    throw;
            }
            return true;
        }

        public static void Register(FileTypeSymbol fileTypeSymbol, FileTypeAlgorithm algorithm)
        {
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
        public Func<FilePathEntity, FileRepositoryEntity> GetRepository { get; set; }
        public Func<FilePathEntity, string> CalculateSufix { get; set; }

        bool renameOnCollision = true;
        public bool RenameOnCollision
        {
            get { return renameOnCollision; }
            set { renameOnCollision = value; }
        }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public FileTypeAlgorithm()
        {
            RenameAlgorithm = DefaultRenameAlgorithm;
            GetRepository = DefaultGetRepository;
            CalculateSufix = FileName_Sufix;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix),
              "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        public static readonly Func<FilePathEntity, FileRepositoryEntity> DefaultGetRepository = (FilePathEntity fp) =>
            Database.Query<FileRepositoryEntity>().FirstOrDefault(r => r.Active && r.FileTypes.Contains(fp.FileType));

        public static readonly Func<FilePathEntity, string> FileName_Sufix = (FilePathEntity fp) => fp.FileName;

        public static readonly Func<FilePathEntity, string> Year_FileName_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), fp.FileName);
        public static readonly Func<FilePathEntity, string> Year_Month_FileName_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), fp.FileName));

        public static readonly Func<FilePathEntity, string> Year_GuidExtension_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
        public static readonly Func<FilePathEntity, string> Year_Month_GuidExtension_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName)));

        public static readonly Func<FilePathEntity, string> YearMonth_Guid_Filename_Sufix = (FilePathEntity fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));
        public static readonly Func<FilePathEntity, string> Isolated_YearMonth_Guid_Filename_Sufix = (FilePathEntity fp) => Path.Combine(IsolationEntity.Current.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));

    }
}
