using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
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
            }
        }

        public static IFileTypeAlgorithm GetAlgorithm(this FileTypeSymbol fileType)
        {
            return FileTypes.GetOrThrow(fileType);
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
        PrefixPair GetPrefixPair(IFilePath efp);
    }

    public static class SuffixGenerators
    {
        //No GUID, use only for icons or public domain files
        public static class UNSAFE
        {
            public static readonly Func<IFilePath, string> FileName = (IFilePath fp) => Path.GetFileName(fp.FileName)!;
            public static readonly Func<IFilePath, string> CalculatedDirectory_FileName = (IFilePath fp) => Path.Combine(fp.CalculatedDirectory!, Path.GetFileName(fp.FileName)!);

            public static readonly Func<IFilePath, string> Year_FileName = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Path.GetFileName(fp.FileName)!);
            public static readonly Func<IFilePath, string> Year_Month_FileName = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), TimeZoneManager.Now.Month.ToString(), Path.GetFileName(fp.FileName)!);

        }

        //Thanks to the GUID, the file name can not be guessed
        public static class Safe
        {
            public static readonly Func<IFilePath, string> Year_GuidExtension = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
            public static readonly Func<IFilePath, string> Year_Month_GuidExtension = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.Year.ToString(), TimeZoneManager.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName));

            public static readonly Func<IFilePath, string> YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(TimeZoneManager.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName)!);
            public static readonly Func<IFilePath, string> Isolated_YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(IsolationEntity.Current?.IdOrNull.ToString() ?? "None", TimeZoneManager.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName)!);
        }
    }

    public abstract class FileTypeAlgorithmBase
    {
        public Action<IFilePath>? OnValidateFile { get; set; }
        public int? MaxSizeInBytes { get; set; }
        public bool OnlyImages { get; set; }

        public void ValidateFile(IFilePath fp)
        {
            if (OnlyImages)
            {
                var mime = MimeMapping.GetMimeType(fp.FileName);
                if (mime == null || !mime.StartsWith("image/"))
                    throw new ApplicationException(FileMessage.TheFile0IsNotA1.NiceToString(fp.FileName, "image/*"));
            }

            if (MaxSizeInBytes != null)
            {
                if (fp.BinaryFile!.Length > MaxSizeInBytes)
                    throw new ApplicationException(FileMessage.File0IsTooBigTheMaximumSizeIs1.NiceToString(fp.FileName, ((long)fp.BinaryFile.Length).ToComputerSize()));
            }

            OnValidateFile?.Invoke(fp);
        }
    }

}
