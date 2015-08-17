using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Files
{
    [Serializable]
    public class EmbeddedFileEntity : EmbeddedEntity, IFile
    {
        [NotNullable]
        string fileName;
        [StringLengthValidator(Min = 3)]
        public string FileName
        {
            get { return fileName; }
            set { SetToStr(ref fileName, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string relativeFilePath;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string RelativeFilePath
        {
            get { return relativeFilePath; }
            set { Set(ref relativeFilePath, value); }
        }

        FileTypeSymbol fileType;
        public FileTypeSymbol FileType
        {
            get { return fileType; }
            set { Set(ref fileType, value); }
        }

        [Ignore]
        byte[] binaryFile;
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set { SetToStr(ref binaryFile, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(fileName, BinaryFile.Try(bf => StringExtensions.ToComputerSize(bf.Length)) ?? "??");
        }


        public string FullWebPath
        {
            get { return null; }
        }
    }
}
