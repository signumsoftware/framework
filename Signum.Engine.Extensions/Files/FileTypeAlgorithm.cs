using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Signum.Engine.Files
{
    public class FileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
    {
        public Func<IFilePath, PrefixPair> GetPrefixPair { get; set; }
        public Func<IFilePath, string> CalculateSuffix { get; set; }

        public bool RenameOnCollision { get; set; }
        public bool WeakFileReference { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }

        public FileTypeAlgorithm(Func<IFilePath, PrefixPair> getPrefixPair)
        {
            this.GetPrefixPair = getPrefixPair;
            
            WeakFileReference = false;
            CalculateSuffix = SuffixGenerators.Safe.YearMonth_Guid_Filename;

            RenameOnCollision = true;
            RenameAlgorithm = DefaultRenameAlgorithm;
        }

        public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
           Path.Combine(Path.GetDirectoryName(sufix)!,
              "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));

        
        public string? ConfigErrors()
        {
            string? error = null;
            if (GetPrefixPair == null)
                error = "GetPrefixPair";

            if (!WeakFileReference && CalculateSuffix == null)
                error = ", ".Combine(error, "CalculateSufix");

            if (RenameOnCollision && RenameAlgorithm == null)
                error = ", ".Combine(error, "RenameAlgorithm");

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
            string? fullPhysicalPath = null;
            try
            {
                string path = Path.GetDirectoryName(fp.FullPhysicalPath())!;
                fullPhysicalPath = path;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllBytes(fp.FullPhysicalPath(), fp.BinaryFile);
                fp.BinaryFile = null!;
            }
            catch (IOException ex)
            {
                ex.Data.Add("FullPhysicalPath", fullPhysicalPath);
                ex.Data.Add("CurrentPrincipal", System.Threading.Thread.CurrentPrincipal!.Identity!.Name);

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
