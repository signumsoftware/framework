using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Signum.Utilities
{
    public static class FileTools
    {
        public static string AvailableFileName(string fileName)
        {
            if (!File.Exists(fileName))
                return fileName;
            string path = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
            string extension = Path.GetExtension(fileName);
            for (int i = 1; ; i++)
            {
                string fullPath = "{0}({1}){2}".Formato(path, i, extension);
                if (!File.Exists(fullPath))
                    return fullPath;
            }
        }

        public static void CreateDirectory(string FilePath)
        {
            if (FilePath != string.Empty)
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (directory != string.Empty)
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
            }
        }

        public class TemporalFile : IDisposable
        {
            string tempFile;
            public string Path
            {
                get { return tempFile; }
            }

            bool ignoreError = true;

            public TemporalFile(byte[] data, string extension)
            {
                Create(data, extension);
            }

            public TemporalFile(byte[] data, string extension, bool ignoreDisposingError)
            {
                ignoreError = ignoreDisposingError;
                Create(data, extension);
            }

            private void Create(byte[] data, string extension)
            {
                tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "{0}{1}".Formato(System.IO.Path.GetTempFileName(), extension.HasText() ? "." + extension.Replace(".", null) : null));
                File.WriteAllBytes(tempFile, data);
            }

            public void Dispose()
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    if (!ignoreError)
                        throw;
                }
            }
        }

        public static TemporalFile GetTemporalFile(this byte[] data, string extension)
        {
            return new TemporalFile(data, extension);
        }

        public static TemporalFile GetTemporalFile(this byte[] data, string extension, bool ignoreDisposingErrors)
        {
            return new TemporalFile(data, extension, ignoreDisposingErrors);
        }
    }

}
