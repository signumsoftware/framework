using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Files;
using Signum.Entities.Isolation;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Files
{
    public static class FileTypeLogic
    {
        public static Dictionary<FileTypeSymbol, FileTypeAlgorithm> FileTypes = new Dictionary<FileTypeSymbol, FileTypeAlgorithm>();

        public static void Register(FileTypeSymbol fileTypeSymbol, FileTypeAlgorithm algorithm)
        {
            FileTypes.Add(fileTypeSymbol, algorithm);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<FileTypeSymbol>.Start(sb, () => FileTypes.Keys.ToHashSet());

                dqm.RegisterQuery(typeof(FileTypeSymbol), () =>
                    from f in Database.Query<FileTypeSymbol>()
                    select new
                    {
                        Entity = f,
                        f.Key
                    });


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

        internal static void SaveFile(IFilePath fp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                FileTypeAlgorithm alg = FileTypes.GetOrThrow(fp.FileType);

                if (alg.TakesOwnership)
                {
                    string sufix = alg.CalculateSufix(fp);
                    if (!sufix.HasText())
                        throw new InvalidOperationException("Sufix not set");

                    fp.SetPrefixPair(alg.GetPrefixPair(fp));

                    int i = 2;
                    fp.Sufix = sufix;
                    while (alg.RenameOnCollision && File.Exists(fp.FullPhysicalPath))
                    {
                        fp.Sufix = alg.RenameAlgorithm(sufix, i);
                        i++;
                    }

                    alg.SaveFileInDisk(fp);
                }
            }
        }
    }

    public sealed class FileTypeAlgorithm
    {
        public Func<IFilePath, PrefixPair> GetPrefixPair { get; set; }
        public Func<IFilePath, string> CalculateSufix { get; set; }

        public bool RenameOnCollision { get; set; }
        public bool TakesOwnership { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public Action<IFilePath> SaveFileInDisk;
        public Action<List<IFilePath>> DeleteFiles;

        public FileTypeAlgorithm()
        {
            TakesOwnership = true;
            CalculateSufix = FileName_Sufix;

            RenameOnCollision = true;
            RenameAlgorithm = DefaultRenameAlgorithm;

            SaveFileInDisk = SaveFileDefault;
            DeleteFiles = DeleteFilesDefault;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix),
              "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        public static readonly Func<IFilePath, string> FileName_Sufix = (IFilePath fp) => fp.FileName;
        public static readonly Func<IFilePath, string> CalculatedDirectory_FileName_Sufix = (IFilePath fp) => Path.Combine(fp.CalculatedDirectory, fp.FileName);

        public static readonly Func<IFilePath, string> Year_FileName_Sufix = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), fp.FileName);
        public static readonly Func<IFilePath, string> Year_Month_FileName_Sufix = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), fp.FileName));

        public static readonly Func<IFilePath, string> Year_GuidExtension_Sufix = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
        public static readonly Func<IFilePath, string> Year_Month_GuidExtension_Sufix = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.Combine(TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName)));

        public static readonly Func<IFilePath, string> YearMonth_Guid_Filename_Sufix = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));
        public static readonly Func<IFilePath, string> Isolated_YearMonth_Guid_Filename_Sufix = (IFilePath fp) => Path.Combine(IsolationEntity.Current.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));


        public string Errors()
        {
            string error = null;

            if (GetPrefixPair == null)
                error = "GetPrefixPair";

            if (TakesOwnership && CalculateSufix == null)
                error = ", ".CombineIfNotEmpty(error, "CalculateSufix");

            if (RenameOnCollision && RenameAlgorithm == null)
                error = ", ".CombineIfNotEmpty(error, "RenameAlgorithm");

            if (error.HasText())
                error += " not set";

            return error;
        }

        public static void SaveFileDefault(IFilePath fp)
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

        public static void DeleteFilesDefault(List<IFilePath> filePaths)
        {
            foreach (var fp in filePaths)
            {
                File.Delete(fp.FullPhysicalPath);
            }
        }
    }
}
