using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Basics
{
    public class FileContent
    {
        public string FileName { get; private set; }
        public byte[] Bytes { get; private set; }

        public FileContent(string fileName, byte[] bytes)
        {
            this.FileName = fileName;
            this.Bytes = bytes;
        }
    }
}
