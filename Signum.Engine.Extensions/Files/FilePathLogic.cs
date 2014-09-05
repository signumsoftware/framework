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

namespace Signum.Engine.Files
{
    public static class FilePathLogic
    {
        static Expression<Func<FilePathDN, WebImage>> WebImageExpression =
            fp => new WebImage { FullWebPath = fp.FullWebPath };
        public static WebImage WebImage(this FilePathDN fp)
        {
            return WebImageExpression.Evaluate(fp);
        }

        static Expression<Func<FilePathDN, WebDownload>> WebDownloadExpression =
           fp => new WebDownload { FullWebPath = fp.FullWebPath };
        public static WebDownload WebDownload(this FilePathDN fp)
        {
            return WebDownloadExpression.Evaluate(fp);
        }

        static Dictionary<FileTypeSymbol, FileTypeAlgorithm> fileTypes = new Dictionary<FileTypeSymbol, FileTypeAlgorithm>();

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<FilePathDN>();

                SymbolLogic<FileTypeSymbol>.Start(sb, () => fileTypes.Keys.ToHashSet());

                sb.Schema.EntityEvents<FilePathDN>().PreSaving += FilePath_PreSaving;
                sb.Schema.EntityEvents<FilePathDN>().PreUnsafeDelete += new PreUnsafeDeleteHandler<FilePathDN>(FilePathLogic_PreUnsafeDelete);

                dqm.RegisterQuery(typeof(FileRepositoryDN), () =>
                    from r in Database.Query<FileRepositoryDN>()
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.Name,
                        r.Active,
                        r.PhysicalPrefix,
                        r.WebPrefix
                    });

                dqm.RegisterQuery(typeof(FilePathDN), () =>
                    from p in Database.Query<FilePathDN>()
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

                dqm.RegisterQuery(typeof(FileTypeSymbol), () =>
                    from f in Database.Query<FileTypeSymbol>()
                    select new
                    {
                        Entity = f,
                        f.Key
                    });

                sb.AddUniqueIndex<FilePathDN>(f => new { f.Sufix, f.Repository });

                dqm.RegisterExpression((FilePathDN fp) => fp.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                dqm.RegisterExpression((FilePathDN fp) => fp.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");

                new Graph<FileRepositoryDN>.Execute(FileRepositoryOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (fr, _) => { }
                }.Register();
            }
        }


        static void FilePathLogic_PreUnsafeDelete(IQueryable<FilePathDN> query)
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

        public static FilePathDN UnsafeLoad(FileRepositoryDN repository, FileTypeSymbol fileType, string fullPath)
        {
            if (!fullPath.StartsWith(repository.FullPhysicalPrefix, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("The File {0} doesn't belong to the repository {1}".Formato(fullPath, repository.PhysicalPrefix));

            return new FilePathDN
            {
                FileLength = (int)new FileInfo(fullPath).Length,
                FileType = fileType,
                Sufix = fullPath.Substring(repository.FullPhysicalPrefix.Length).TrimStart('\\'),
                FileName = Path.GetFileName(fullPath),
                Repository = repository,
            };
        }

        static void FilePath_PreSaving(FilePathDN fp, ref bool graphModified)
        {
            if (fp.IsNew && !unsafeMode.Value)
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                {
                    FileTypeAlgorithm alg = fileTypes[fp.FileType];
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

        private static bool SaveFile(FilePathDN fp)
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
                    using (OperationLogic.AllowSave<FileRepositoryDN>())
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
            fileTypes.Add(fileTypeSymbol, algorithm);
        }

        public static byte[] GetByteArray(this FilePathDN fp)
        {
            return fp.BinaryFile ?? File.ReadAllBytes(fp.FullPhysicalPath);
        }

        public static byte[] GetByteArray(this Lite<FilePathDN> fp)
        {
            return File.ReadAllBytes(fp.InDB(f => f.FullPhysicalPath));
        }


    }

    public sealed class FileTypeAlgorithm
    {
        public Func<FilePathDN, FileRepositoryDN> GetRepository { get; set; }
        public Func<FilePathDN, string> CalculateSufix { get; set; }

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
              "{0}({1}){2}".Formato(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        public static readonly Func<FilePathDN, FileRepositoryDN> DefaultGetRepository = (FilePathDN fp) =>
            Database.Query<FileRepositoryDN>().FirstOrDefault(r => r.Active && r.FileTypes.Contains(fp.FileType));

        public static readonly Func<FilePathDN, string> FileName_Sufix = (FilePathDN fp) => fp.FileName;

        public static readonly Func<FilePathDN, string> Year_FileName_Sufix = (FilePathDN fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), fp.FileName);
        public static readonly Func<FilePathDN, string> Year_Month_FileName_Sufix = (FilePathDN fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), fp.FileName));

        public static readonly Func<FilePathDN, string> Year_GuidExtension_Sufix = (FilePathDN fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
        public static readonly Func<FilePathDN, string> Year_Month_GuidExtension_Sufix = (FilePathDN fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName)));

        public static readonly Func<FilePathDN, string> YearMonth_Guid_Filename_Sufix = (FilePathDN fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));
    }
}
