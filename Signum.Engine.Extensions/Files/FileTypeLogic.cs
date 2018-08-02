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
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<FileTypeSymbol>.Start(sb,() => FileTypes.Keys.ToHashSet());
                sb.Include<FileTypeSymbol>()
                    .WithQuery(() => f => new
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
        bool OnlyImages { get; set; }
        int? MaxSizeInBytes { get; set; }
        void SaveFile(IFilePath fp);
        void ValidateFile(IFilePath fp);
        void DeleteFiles(IEnumerable<IFilePath> files);
        byte[] ReadAllBytes(IFilePath fp);
        Stream OpenRead(IFilePath fp);
        void MoveFile(IFilePath ofp, IFilePath nfp);
        string ConfigErrors();
        PrefixPair GetPrefixPair(IFilePath efp);
    }

    public static class SuffixGenerators
    {
        //No GUID, use only for icons or public domain files
        public static class UNSAFE
        {
            public static readonly Func<IFilePath, string> FileName = (IFilePath fp) => Path.GetFileName(fp.FileName);
            public static readonly Func<IFilePath, string> CalculatedDirectory_FileName = (IFilePath fp) => Path.Combine(fp.CalculatedDirectory, Path.GetFileName(fp.FileName));

            public static readonly Func<IFilePath, string> Year_FileName = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.GetFileName(fp.FileName));
            public static readonly Func<IFilePath, string> Year_Month_FileName = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), TimeZoneManager.Now.Month.ToString(), Path.GetFileName(fp.FileName));

        }

        //Thanks to the GUID, the file name can not be guessed
        public static class Safe
        {
            public static readonly Func<IFilePath, string> Year_GuidExtension = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
            public static readonly Func<IFilePath, string> Year_Month_GuidExtension = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName));

            public static readonly Func<IFilePath, string> YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName));
            public static readonly Func<IFilePath, string> Isolated_YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(IsolationEntity.Current?.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName));
        }
    }

    public class FileTypeAlgorithm : IFileTypeAlgorithm
    {
        public Func<IFilePath, PrefixPair> GetPrefixPair { get; set; }
        public Func<IFilePath, string> CalculateSuffix { get; set; }

        public bool RenameOnCollision { get; set; }
        public bool WeakFileReference { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public FileTypeAlgorithm()
        {
            WeakFileReference = false;
            CalculateSuffix = SuffixGenerators.Safe.YearMonth_Guid_Filename;

            RenameOnCollision = true;
            RenameAlgorithm = DefaultRenameAlgorithm;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix),
              "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        
        public string ConfigErrors()
        {
            string error = null;

            if (GetPrefixPair == null)
                error = "GetPrefixPair";

            if (!WeakFileReference && CalculateSuffix == null)
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

                string sufix = CalculateSuffix(fp);
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

        public Action<IFilePath> OnValidateFile { get; set; }
        public int? MaxSizeInBytes { get; set; }
        public bool OnlyImages { get; set; }

        public void ValidateFile(IFilePath fp)
        {
            if (OnlyImages)
            {
                var mime = MimeMapping.GetMimeMapping(fp.FileName);
                if (mime == null || !mime.StartsWith("image/"))
                    throw new ApplicationException(FileMessage.TheFile0IsNotA1.NiceToString(fp.FileName, "image/*"));
            }

            if (MaxSizeInBytes != null)
            {
                if (fp.BinaryFile.Length > MaxSizeInBytes)
                    throw new ApplicationException(FileMessage.File0IsTooBigTheMaximumSizeIs1.NiceToString(fp.FileName, ((long)fp.BinaryFile.Length).ToComputerSize()));
            }

            OnValidateFile?.Invoke(fp);
        }
    }
}
