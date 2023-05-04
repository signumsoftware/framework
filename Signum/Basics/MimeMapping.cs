using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace Signum.Basics;

public static class MimeMapping
{
    public static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        FileExtensionContentTypeProvider mimeConverter = new FileExtensionContentTypeProvider();

        return mimeConverter.Mappings.TryGetValue(extension ?? "", out var result) ? result : "application/octet-stream";
    }

    /// <param name="stream">No need to close</param
    public static FileStreamResult GetFileStreamResult(Stream stream, string fileName, bool forDownload = true)
    {
        var mime = MimeMapping.GetMimeType(fileName);
        return new FileStreamResult(stream, mime)
        {
            FileDownloadName = forDownload ? Path.GetFileName(fileName) : null
        };
    }

    public static FileStreamResult GetFileStreamResult(FileContent file, bool forDownload = true)
    {
        return GetFileStreamResult(new MemoryStream(file.Bytes), file.FileName, forDownload);
    }
}
