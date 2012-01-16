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
    }
}
