using System.IO;
using System.IO.Compression;

namespace Signum.Files;

public interface ILimitedFileSystem : IDisposable
{
    DirectoryInfo CreateDirectory(string path);
    bool DirectoryExists(string path);
    string[] GetFiles(string path, string searchPattern);
    DirectoryInfo[] GetDirectories(string path);
    void DeleteDirectory(string path, bool recursive);
    Stream FileOpenWrite(string path);
    void FileWriteAllBytes(string path, byte[] bytes);
    byte[] FileReadAllBytes(string path);
    void FileDelete(string path);

}

public class RealLimitedFileSystem : ILimitedFileSystem
{
    public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public Stream FileOpenWrite(string path) => File.OpenWrite(path);

    public void FileWriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public byte[] FileReadAllBytes(string path) => File.ReadAllBytes(path);

    public void FileDelete(string path) => File.Delete(path);

    public DirectoryInfo[] GetDirectories(string path) => new DirectoryInfo(path).GetDirectories();

    public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);

    void IDisposable.Dispose()
    {
        //nothing to dispose
    }
}

public class ZipBuilder(string root) : ILimitedFileSystem
{
    private readonly string root = root.TrimEnd('/', '\\');

    // key: normalized path, value: bytes
    private readonly Dictionary<string, byte[]> files = [];

    private string Normalize(string path) =>
        $"{root}/{path.Replace(Path.DirectorySeparatorChar, '/')}";

    public Stream FileOpenWrite(string path)
    {
        path = Normalize(path);

        var ms = new MemoryStream();

        return new StreamWithCallback(ms, s =>
        {
            s.Position = 0;
            files[path] = ((MemoryStream)s).ToArray();
        });
    }

    public void FileWriteAllBytes(string path, byte[] bytes)
    {
        path = Normalize(path);
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

    private sealed class StreamWithCallback(Stream inner, Action<Stream> onClose) : Stream
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

    public void Dispose()
    {
        // nothing to dispose
    }

    // Unsupported operations
    public DirectoryInfo CreateDirectory(string path) => new(Normalize(path));
    public void DeleteDirectory(string path, bool recursive) => throw new NotSupportedException();
    public bool DirectoryExists(string path) => false;
    public DirectoryInfo[] GetDirectories(string path) => [];
    public string[] GetFiles(string path, string searchPattern) => [];
    public byte[] FileReadAllBytes(string path) => throw new NotSupportedException();
    public void FileDelete(string path) => throw new NotSupportedException();

}

