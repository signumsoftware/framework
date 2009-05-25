using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities; 

namespace Signum.Entities.Basics
{
    public interface IFileDN
    {
        byte[] BinaryFile { get; set; }
        string FileName { get; set; }
    }

    [Serializable]
    public class FileDN : EmbeddedEntity, IFileDN
    {
        [NotNullable]
        byte[] binaryFile;
        [NotNullValidator]
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set { Set(ref binaryFile, value, "BinaryFile"); }
        }

        [NotNullable]
        string fileName;
        [StringLengthValidator(Min = 3)]
        public string FileName
        {
            get { return fileName; }
            set { Set(ref fileName, value, "FileName"); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(fileName, BinaryFile.TryCC(bf => StringExtensions.ToComputerSize(bf.Length)) ?? "??");
        }
    }
}
