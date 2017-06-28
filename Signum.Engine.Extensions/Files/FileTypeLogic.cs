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
        public static Dictionary<FileTypeSymbol, IFileTypeAlgorithm> FileTypes = new Dictionary<FileTypeSymbol, IFileTypeAlgorithm>();

        public static void Register(FileTypeSymbol fileTypeSymbol, IFileTypeAlgorithm algorithm)
        {
            if (fileTypeSymbol == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(FileTypeSymbol), nameof(fileTypeSymbol));

            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm));

            FileTypes.Add(fileTypeSymbol, algorithm);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<FileTypeSymbol>.Start(sb,dqm, () => FileTypes.Keys.ToHashSet());
                sb.Include<FileTypeSymbol>()
                    .WithQuery(dqm, () => f => new
                    {
                        Entity = f,
                        f.Key
                    });

                sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
            }
        }

        public static IFileTypeAlgorithm GetAlgorithm(this FileTypeSymbol fileType)
        {
            return FileTypes.GetOrThrow(fileType);
        }

        static void Schema_SchemaCompleted()
        {
            var errors = (from kvp in FileTypes
                          let error = kvp.Value.ConfigErrors()
                          where error.HasText()
                          select kvp.Key + ": " + error.Indent(4)).ToList();

            if (errors.Any())
                throw new InvalidOperationException("Errors in the following FileType algorithms: \r\n" +
                    errors.ToString("\r\n").Indent(4));
        }
    }

    public interface IFileTypeAlgorithm
    {
        void SaveFile(IFilePath fp);
        void DeleteFiles(IEnumerable<IFilePath> files);
        byte[] ReadAllBytes(IFilePath fp);
        Stream OpenRead(IFilePath fp);
        void MoveFile(IFilePath ofp, IFilePath nfp);
        string ConfigErrors();
        PrefixPair GetPrefixPair(IFilePath efp);
    }

    public class FileTypeAlgorithm : IFileTypeAlgorithm
    {
        public Func<IFilePath, PrefixPair> GetPrefixPair { get; set; }
        public Func<IFilePath, string> CalculateSufix { get; set; }

        public bool RenameOnCollision { get; set; }
        public bool WeakFileReference { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public FileTypeAlgorithm()
        {
            WeakFileReference = false;
            CalculateSufix = FileName_Sufix;

            RenameOnCollision = true;
            RenameAlgorithm = DefaultRenameAlgorithm;
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
        public static readonly Func<IFilePath, string> Isolated_YearMonth_Guid_Filename_Sufix = (IFilePath fp) => Path.Combine(IsolationEntity.Current?.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Path.Combine(Guid.NewGuid().ToString(), fp.FileName));


        public string ConfigErrors()
        {
            string error = null;

            if (GetPrefixPair == null)
                error = "GetPrefixPair";

            if (!WeakFileReference && CalculateSufix == null)
                error = ", ".CombineIfNotEmpty(error, "CalculateSufix");

            if (RenameOnCollision && RenameAlgorithm == null)
                error = ", ".CombineIfNotEmpty(error, "RenameAlgorithm");

            if (error.HasText())
                error += " not set";

            return error;
        }

        public virtual void SaveFile(IFilePath fp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                if (WeakFileReference)
                    return;

                string sufix = CalculateSufix(fp);
                if (!sufix.HasText())
                    throw new InvalidOperationException("Sufix not set");

                fp.SetPrefixPair(GetPrefixPair(fp));

                int i = 2;
                fp.Suffix = sufix;
                while (RenameOnCollision && File.Exists(fp.FullPhysicalPath()))
                {
                    fp.Suffix = RenameAlgorithm(sufix, i);
                    i++;
                }

                SaveFileInDisk(fp);
            }
        }

        public virtual void SaveFileInDisk(IFilePath fp)
        {
            string fullPhysicalPath = null;
            try
            {
                string path = Path.GetDirectoryName(fp.FullPhysicalPath());
                fullPhysicalPath = path;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllBytes(fp.FullPhysicalPath(), fp.BinaryFile);
                fp.BinaryFile = null;
            }
            catch (IOException ex)
            {
                ex.Data.Add("FullPhysicalPath", fullPhysicalPath);
                ex.Data.Add("CurrentPrincipal", System.Threading.Thread.CurrentPrincipal.Identity.Name);

                throw;
            }
        }

        public virtual Stream OpenRead(IFilePath path)
        {
            return File.OpenRead(path.FullPhysicalPath());
        }

        public virtual byte[] ReadAllBytes(IFilePath path)
        {
            return File.ReadAllBytes(path.FullPhysicalPath());
        }

        public virtual void MoveFile(IFilePath ofp, IFilePath fp)
        {
            if (WeakFileReference)
                return;

            System.IO.File.Move(ofp.FullPhysicalPath(), fp.FullPhysicalPath());
        }

        public virtual void DeleteFiles(IEnumerable<IFilePath> files)
        {
            if (WeakFileReference)
                return;

            foreach (var f in files)
            {
                File.Delete(f.FullPhysicalPath());
            }
        }

        PrefixPair IFileTypeAlgorithm.GetPrefixPair(IFilePath efp)
        {
            return this.GetPrefixPair(efp);
        }
    }
}
