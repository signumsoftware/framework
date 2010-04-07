using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using System.IO;
using Signum.Utilities;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileDN : Entity, IFile
    {
        public FileDN() { }

        public FileDN(string path)
        {
            this.fileName = Path.GetFileName(path);
            this.binaryFile = File.ReadAllBytes(path); 
        }

        [NotNullable, SqlDbType(Size = 254)]
        string fileName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 254)]
        public string FileName
        {
            get { return fileName; }
            set { SetToStr(ref fileName, value, () => FileName); }
        }

        byte[] binaryFile;
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set { Set(ref binaryFile, value, () => BinaryFile); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(fileName, BinaryFile.TryCC(bf => StringExtensions.ToComputerSize(bf.Length)) ?? "??");
        }

        public Uri WebPath
        {
            get { return null; }
        }

        public string FullWebPath
        {
            get { return null; }
        }
    }
}
