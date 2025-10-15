using Signum.API.Filters;
using Signum.Utilities;
using System;
using System.IO;

namespace Signum.Files.FileTypeAlgorithms;

public class FileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
{
    public Func<IFilePath, string> GetPhisicalPrefix { get; set; }
    public Func<IFilePath, string>? GetWebPrefix { get; set; }

    public Func<IFilePath, string> CalculateSuffix { get; set; }

    public bool WeakFileReference { get; set; }
    public bool DeleteEmptyFolderOnDelete { get; set; }

    public Func<string, int, string>? RenameAlgorithm { get; set; }

    public FileTypeAlgorithm(Func<IFilePath, string> physicalPrefix, Func<IFilePath, string>? webPrefix)
    {
        this.GetPhisicalPrefix = physicalPrefix;
        this.GetWebPrefix = webPrefix;

        WeakFileReference = false;
        CalculateSuffix = SuffixGenerators.Safe.YearMonth_Guid_Filename;
        DeleteEmptyFolderOnDelete = true;

        //Avoids potentially slow File.Exists, consider using CalculateSuffix using GUID
        RenameAlgorithm = null; // DefaultRenameAlgorithm;
    }

    public static readonly Func<string, int, string> DefaultRenameAlgorithm = (sufix, num) =>
       Path.Combine(Path.GetDirectoryName(sufix)!,
          "{0}({1}){2}".FormatWith(Path.GetFileNameWithoutExtension(sufix), num, Path.GetExtension(sufix)));


    public virtual void SaveFile(IFilePath fp)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            if (WeakFileReference)
                return;

            CalculateSufixWithRenames(fp);

            EnsureDirectory(fp);

            var binaryFile = fp.BinaryFile;
            SaveFileInDisk(fp.FullPhysicalPath()!, binaryFile);
            fp.CleanBinaryFile();
        }
    }

    static readonly string Uploading = ".uploading";
    public Task StartUpload(IFilePath fp)
    {
        CalculateSufixWithRenames(fp);

        EnsureDirectory(fp);

        using var f = File.Create(fp.FullPhysicalPath()!);
        using var f2 = File.Create(fp.FullPhysicalPath()! + Uploading);

        return Task.CompletedTask;
    }

    public async Task<ChunkInfo> UploadChunk(IFilePath fp, int chunkIndex, MemoryStream chunk, CancellationToken token = default)
    {
        if (!File.Exists(fp.FullPhysicalPath() + Uploading))
            throw new InvalidOperationException("File is not currently uploading!: " + fp.FullPhysicalPath());

        using (var file = File.Open(fp.FullPhysicalPath()!, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            await chunk.CopyToAsync(file, token);

        var hash = CryptorEngine.CalculateMD5Hash(chunk);

        return new ChunkInfo
        {
            PartialHash = hash,
            BlockId = chunkIndex.ToString(),
        };
    }

    public Task FinishUpload(IFilePath fp, List<ChunkInfo> chunks, CancellationToken token = default)
    {
        var fi = new FileInfo(fp.FullPhysicalPath()!);

        var hashList = chunks.ToString(c => c.PartialHash, "\n");

        fp.Hash = (CryptorEngine.CalculateMD5Hash(Encoding.UTF8.GetBytes(hashList)));
        fp.FileLength = fi!.Length;

        File.Delete(fp.FullPhysicalPath() + Uploading);

        return Task.CompletedTask;
    }

    public virtual Task SaveFileAsync(IFilePath fp, CancellationToken token = default)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            if (WeakFileReference)
                return Task.CompletedTask;

            CalculateSufixWithRenames(fp);

            EnsureDirectory(fp);

            var binaryFile = fp.BinaryFile;
            //fp.CleanBinaryFile(); at the end of transaction
            return SaveFileInDiskAsync(fp.FullPhysicalPath()!, binaryFile, token);
        }
    }

    private void CalculateSufixWithRenames(IFilePath fp)
    {
        string suffix = CalculateSuffix(fp);
        if (!suffix.HasText())
            throw new InvalidOperationException("Suffix not set");

        int i = 2;
        fp.Suffix = suffix;
        if (RenameAlgorithm != null)
        {
            while (File.Exists(fp.FullPhysicalPath()))
            {
                fp.Suffix = RenameAlgorithm(suffix, i);
                i++;
            }
        }
    }

    static void SaveFileInDisk(string fullPhysicalPath, byte[] binaryFile)
    {
        using (HeavyProfiler.Log("SaveFileInDisk", () => fullPhysicalPath))
        {
            try
            {
                File.WriteAllBytes(fullPhysicalPath, binaryFile);
            }
            catch (IOException ex)
            {
                ex.Data.Add("FullPhysicalPath", fullPhysicalPath);
                ex.Data.Add("CurrentPrincipal", System.Threading.Thread.CurrentPrincipal!.Identity!.Name);

                throw;
            }
        }
    }

    static async Task SaveFileInDiskAsync(string fullPhysicalPath, byte[] binaryFile, CancellationToken token = default)
    {
        using (HeavyProfiler.Log("SaveFileInDiskAsync", () => fullPhysicalPath))
        {
            try
            {
                await File.WriteAllBytesAsync(fullPhysicalPath, binaryFile);
            }
            catch (IOException ex)
            {
                ex.Data.Add("FullPhysicalPath", fullPhysicalPath);
                ex.Data.Add("CurrentPrincipal", System.Threading.Thread.CurrentPrincipal?.Identity?.Name);

                throw;
            }
        }
    }

    private static string EnsureDirectory(IFilePath fp)
    {
        string fullPhysicalPath = fp.FullPhysicalPath()!;
        string directory = Path.GetDirectoryName(fullPhysicalPath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        return fullPhysicalPath;
    }

    public virtual Stream OpenRead(IFilePath path)
    {
        string fullPhysicalPath = path.FullPhysicalPath()!;
        using (HeavyProfiler.Log("OpenRead", () => fullPhysicalPath))
            return File.OpenRead(fullPhysicalPath);
    }

    public virtual byte[] ReadAllBytes(IFilePath path)
    {
        string fullPhysicalPath = path.FullPhysicalPath()!;
        using (HeavyProfiler.Log("ReadAllBytes", () => fullPhysicalPath))
            return File.ReadAllBytes(fullPhysicalPath);
    }

    public virtual void MoveFile(IFilePath ofp, IFilePath fp,bool createTargetFolder)
    {
        if (WeakFileReference)
            return;

        string source = ofp.FullPhysicalPath()!;
        string target = fp.FullPhysicalPath()!;
        using (HeavyProfiler.Log("ReadAllBytes", () =>
        "SOURCE: " + source + "\n" +
        "TARGET:" + target))
        {
            string targetDirectory = Path.GetDirectoryName(target)!;

            if (createTargetFolder && !Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);


            System.IO.File.Move(source, target);
        }
    }

    public virtual void DeleteFiles(IEnumerable<IFilePath> files)
    {
        if (WeakFileReference)
            return;

        foreach (var f in files)
        {
            string fullPhysicalPath = f.FullPhysicalPath()!;
            using (HeavyProfiler.Log("DeleteFile", () => fullPhysicalPath))
            {
                File.Delete(fullPhysicalPath);
                if (DeleteEmptyFolderOnDelete && IsDirectoryEmpty(Path.GetDirectoryName(fullPhysicalPath)!))
                    Directory.Delete(Path.GetDirectoryName(fullPhysicalPath)!);
            }
        }
    }

    static bool IsDirectoryEmpty(string path)
    {
        return Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0;
    }

    public virtual void DeleteFilesIfExist(IEnumerable<IFilePath> files)
    {
        if (WeakFileReference)
            return;

        foreach (var f in files)
        {
            string fullPhysicalPath = f.FullPhysicalPath()!;

            using (HeavyProfiler.Log("DeleteFileIfExists", () => fullPhysicalPath))
            {
                if (File.Exists(fullPhysicalPath))
                {
                    File.Delete(fullPhysicalPath);

                    if (DeleteEmptyFolderOnDelete)
                        Directory.Delete(Path.GetDirectoryName(fullPhysicalPath)!);
                }
            }
        }
    }


    public string? GetFullPhysicalPath(IFilePath efp)
    {
        return FilePathUtils.SafeCombine(GetPhisicalPrefix(efp), efp.Suffix);
    }

    public string? GetFullWebPath(IFilePath efp)
    {
        var prefix = this.GetWebPrefix?.Invoke(efp);

        if (prefix == null)
            return null;

        var suffix = "/" + FilePathUtils.UrlPathEncode(efp.Suffix.Replace("\\", "/"));

        return Content(prefix + suffix);
    }

    public static Func<string, string> Content = (url) =>
    {
        if (!url.StartsWith("~"))
            return url;

        var urlBuilder = SignumCurrentContextFilter.Url;

        if (urlBuilder == null)
            throw new InvalidOperationException("Unable to convert to url to when not in a request. Consider overriding FileTypeAlgorithm.Content");

        return urlBuilder.Content(url);
    };

    public string? ReadAsStringUTF8(IFilePath fp)
    {
        string fullPhysicalPath = fp.FullPhysicalPath()!;
        using (HeavyProfiler.Log("ReadAllText", () => fullPhysicalPath))
            return File.ReadAllText(fullPhysicalPath);
    }


}

