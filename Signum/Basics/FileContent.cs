using System.IO;

namespace Signum.Basics;

public class FileContent
{
    public string FileName { get;  set; }
    public byte[] Bytes { get;  set; }

    public FileContent(string fileName, byte[] bytes)
    {
        this.FileName = fileName;
        this.Bytes = bytes;
    }

    public static FileContent ReadFrom(string filePath) => new FileContent(Path.GetFileName(filePath), File.ReadAllBytes(filePath));
}
