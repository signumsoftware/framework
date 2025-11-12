using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;

using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Signum.Files;

public abstract class FileSystemScope : IDisposable
{
    private static readonly Variable<IFileSystem?> current = Statics.ThreadVariable<IFileSystem?>("currentHelpXmlFileSystem");
    protected static IFileSystem Current => current.Value ?? new FileSystem();
    private readonly IFileSystem? _previous;

    protected FileSystemScope(IFileSystem fs)
    {
        _previous = current.Value;
        current.Value = fs;
    }

    public void Dispose() => current.Value = _previous; //todo: set to null as always Real FS should be default?

    public static System.IO.Abstractions.IFile File => Current.File;
    public static IDirectory Directory => Current.Directory;
    public static IDirectoryInfoFactory DirectoryInfo => Current.DirectoryInfo;
    public static IPath Path => Current.Path;
}

/// <summary>
/// Provides a disposable in-memory file system scope that captures all file operations 
/// into a virtual structure and builds a ZIP archive on demand. 
/// <para>Intended for use around export routines such as <c>HelpXml.ExportAll()</c>, allowing them to 
/// write outputs a Zip directly from RAM without touching disk or altering existing export logic.</para>
/// </summary>

/// <param name="root">The root folder for entries within the ZIP archive.</param>
public sealed class ZipBuilderScope(string root) : FileSystemScope(new MockFileSystem())
{
    private readonly string root = root.TrimEnd('/', '\\');

    public byte[] GetAllBytes()
    {
        var mockFS = (MockFileSystem)Current;
        using var ms = new MemoryStream();
        using var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);
        var files = Directory.GetFiles(".", "*", SearchOption.AllDirectories);
        foreach (var sourceFile in files)
        {
            var destFile = Path.GetRelativePath(mockFS.AllDrives.First(), sourceFile); //relative to drive root
            destFile = destFile.Replace(Path.DirectorySeparatorChar, '/'); // normalize dir seperator for Zip
            destFile = $"{root}/{destFile}";
            var destEntry = zip.CreateEntry(destFile);

            using var destStream = destEntry.Open();
            using var source = File.OpenRead(sourceFile);
            source.CopyTo(destStream);
        }
        zip.Dispose();

        return ms.ToArray();
    }
}

