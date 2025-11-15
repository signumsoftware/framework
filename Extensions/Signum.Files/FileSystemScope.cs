using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Signum.Files;

public class FileSystemScope : IDisposable
{
    private static readonly Variable<ILimitedFileSystem> current = Statics.ThreadVariable<ILimitedFileSystem>("currentLimitedFileSystem");
    
    private static RealLimitedFileSystem realFS = new();
    protected static ILimitedFileSystem Current => current.Value ?? realFS;

    public FileSystemScope(ILimitedFileSystem fs)
    {
        current.Value = fs;
    }

    void IDisposable.Dispose()
    {
        current.Value = realFS;
    }

    #region Directory

    public static class Directory
    { 
        public static bool Exists(string path) => Current.DirectoryExists(path);
        public static string[] GetFiles(string path) => Current.GetFiles(path, "*");
        public static string[] GetFiles(string path, string searchPattern) => Current.GetFiles(path, searchPattern);
        public static DirectoryInfo CreateDirectory(string path) => Current.CreateDirectory(path);
        public static DirectoryInfo[] GetDirectories(string path) => Current.GetDirectories(path);
        public static void Delete(string path, bool recursive) => Current.DeleteDirectory(path, recursive);
    
    }
    #endregion


    #region File

    public static class File
    {
        public static Stream OpenWrite(string path) => Current.FileOpenWrite(path);

        public static void WriteAllBytes(string path, byte[] bytes) => Current.FileWriteAllBytes(path, bytes);

        public static byte[] ReadAllBytes(string path) => Current.FileReadAllBytes(path);

        public static Stream OpenRead(string path) => Current.FileOpenRead(path);

        public static void Delete(string path) => Current.FileDelete(path);
    }
    #endregion

    #region Path methods (Always call corresponding methods from System.IO.Paths)

    public static class Path
    {

        // Path methods always call corresponding methods from System.IO.Path

        [return: NotNullIfNotNull(nameof(path))]
        public static string? GetFileNameWithoutExtension(string? path) => System.IO.Path.GetFileNameWithoutExtension(path);

        [return: NotNullIfNotNull(nameof(path))]
        public static string? GetFileName(string? path) => System.IO.Path.GetFileName(path);

        public static string Combine(string path1, string path2) => System.IO.Path.Combine(path1, path2);

        public static string Combine(string path1, string path2, string path3) => System.IO.Path.Combine(path1, path2, path3);

        public static string? GetDirectoryName(string? path) => System.IO.Path.GetDirectoryName(path);

        public static char[] GetInvalidPathChars() => System.IO.Path.GetInvalidPathChars();
    }
    #endregion

}



