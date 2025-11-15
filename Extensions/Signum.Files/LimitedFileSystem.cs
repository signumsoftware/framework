using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Signum.Files;

public interface ILimitedFileSystem
{
    bool DirectoryExists(string path);
    DirectoryInfo CreateDirectory(string path);
    DirectoryInfo[] GetDirectories(string path);
    void DeleteDirectory(string path, bool recursive);

    string[] GetFiles(string path, string searchPattern);
    Stream FileOpenWrite(string path);
    void FileWriteAllBytes(string path, byte[] bytes);
    byte[] FileReadAllBytes(string path);
    Stream FileOpenRead(string path);
    void FileDelete(string path);

}

public class RealLimitedFileSystem : ILimitedFileSystem
{
    bool ILimitedFileSystem.DirectoryExists(string path) => Directory.Exists(path);

    DirectoryInfo ILimitedFileSystem.CreateDirectory(string path) => Directory.CreateDirectory(path);

    DirectoryInfo[] ILimitedFileSystem.GetDirectories(string path) => new DirectoryInfo(path).GetDirectories();

    void ILimitedFileSystem.DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    string[] ILimitedFileSystem.GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);

    Stream ILimitedFileSystem.FileOpenWrite(string path) => File.OpenWrite(path);
    
    void ILimitedFileSystem.FileWriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    byte[] ILimitedFileSystem.FileReadAllBytes(string path) => File.ReadAllBytes(path);

    Stream ILimitedFileSystem.FileOpenRead(string path) => File.OpenRead(path);

    void ILimitedFileSystem.FileDelete(string path) => File.Delete(path);

}

public abstract class ZipFileSystem
{
    protected readonly string root; //No trailing slash

    protected static string SlashFix(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
    protected string Absolute(string path) => root.HasText() ? $"{root}/{path}" : path;

    public ZipFileSystem(string root)
    {
        this.root = root.HasText() ? SlashFix(root).TrimEnd('/', '\\') : "";
    }

    protected sealed class StreamWithCallback(Stream inner, Action<Stream> onClose) : Stream
    {
        private readonly Stream inner = inner;
        private readonly Action<Stream> onClose = onClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onClose(inner);
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }

}

public class ZipBuilder(string root) : ZipFileSystem(root), ILimitedFileSystem, IDisposable
{
    // key: normalized path, value: bytes
    private readonly Dictionary<string, byte[]> files = [];

    public Stream FileOpenWrite(string path)
    {
        path = Absolute(SlashFix(path));
        var ms = new MemoryStream();
        return new StreamWithCallback(ms, s =>
        {
            s.Position = 0;
            files[path] = ((MemoryStream)s).ToArray();
        });
    }

    public void FileWriteAllBytes(string path, byte[] bytes)
    {
        path = Absolute(SlashFix(path));
        files[path] = bytes;
    }

    public byte[] GetAllBytes()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var kvp in files)
            {
                var entry = zip.CreateEntry(kvp.Key, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                entryStream.Write(kvp.Value, 0, kvp.Value.Length);
            }
        }

        return output.ToArray();
    }

    void IDisposable.Dispose()
    {
        //for now nothing to dispose
    }

    //Bypassed operationss
    bool ILimitedFileSystem.DirectoryExists(string path) => false;
    DirectoryInfo ILimitedFileSystem.CreateDirectory(string path) => new(Absolute(SlashFix(path)));

    // Unsupported operations
    DirectoryInfo[] ILimitedFileSystem.GetDirectories(string path) => throw new NotSupportedException();
    void ILimitedFileSystem.DeleteDirectory(string path, bool recursive) => throw new NotSupportedException();
    string[] ILimitedFileSystem.GetFiles(string path, string searchPattern) => [];
    byte[] ILimitedFileSystem.FileReadAllBytes(string path) => throw new NotSupportedException();
    Stream ILimitedFileSystem.FileOpenRead(string path) => throw new NotSupportedException();
    void ILimitedFileSystem.FileDelete(string path) => throw new NotSupportedException();
}

public sealed class ZipLoader : ZipFileSystem, ILimitedFileSystem, IDisposable
{
    private readonly ZipArchive zip;
    private readonly string[] entriesPath; //No trailing slash

    private static string SlashEnd(string path) => path.HasText() ? SlashFix(path).TrimEnd('/', '\\') + '/' : "";

    private static Regex GetPatternRegex(string basePath, string searchPattern)
    {
        basePath = Regex.Escape(SlashFix(basePath).TrimEnd('/', '\\') + '/');
        searchPattern = Regex.Escape(searchPattern)
                                .Replace(@"\*", ".*")
                                .Replace(@"\?", ".");

        return new Regex($"^{basePath}{searchPattern}$", RegexOptions.IgnoreCase);
    }

    public ZipLoader(byte[] bytes, string root) : base(root)
    {
        zip = new(new MemoryStream(bytes), ZipArchiveMode.Read);

        entriesPath = (
            from e in zip.Entries
            let f = SlashFix(e.FullName).TrimEnd('/', '\\')
            where f.StartsWith(SlashEnd(root), StringComparison.OrdinalIgnoreCase)
            select f
            )
            .ToArray();
    }

    public ZipLoader(string zipFile, string root) : this(File.ReadAllBytes(zipFile), root) { }

    Stream ILimitedFileSystem.FileOpenRead(string path)
    {
        var entry = zip.GetEntry(SlashFix(path))
            ?? throw new FileNotFoundException(path);

        return entry.Open();
    }

    byte[] ILimitedFileSystem.FileReadAllBytes(string path) 
    {
        var entry = zip.Entries.FirstOrDefault(e => e.FullName.Equals(SlashFix(path), StringComparison.OrdinalIgnoreCase)) 
            ?? throw new FileNotFoundException($"File '{path}' not found in zip.");

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }

    bool ILimitedFileSystem.DirectoryExists(string path)
    {
        return entriesPath.Any(e => e.StartsWith(Absolute(SlashFix(path)), StringComparison.OrdinalIgnoreCase));
    }

    DirectoryInfo[] ILimitedFileSystem.GetDirectories(string path)
    {
        path = SlashEnd(path);

        var dirs = (
            from entry in entriesPath
            where entry.StartsWith(path, StringComparison.OrdinalIgnoreCase)
            let remainder = entry[path.Length..]
            where remainder.Contains('/')
            select remainder[..remainder.IndexOf('/')]
            )
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return dirs
                .Select(f => new DirectoryInfo(f))
                .ToArray();
    }

    string[] ILimitedFileSystem.GetFiles(string path, string searchPattern)
    {
        var regex = GetPatternRegex(Absolute(path), searchPattern);

        return entriesPath
                .Where(f => regex.IsMatch(f))
                .ToArray();
    }

    void IDisposable.Dispose()
    {
        zip.Dispose();
    }

    // Unsupported operations
    DirectoryInfo ILimitedFileSystem.CreateDirectory(string path) => throw new NotSupportedException();
    void ILimitedFileSystem.DeleteDirectory(string path, bool recursive) => throw new NotSupportedException();
    void ILimitedFileSystem.FileDelete(string path) => throw new NotSupportedException();
    Stream ILimitedFileSystem.FileOpenWrite(string path) => throw new NotSupportedException();
    void ILimitedFileSystem.FileWriteAllBytes(string path, byte[] bytes) => throw new NotSupportedException();
}

