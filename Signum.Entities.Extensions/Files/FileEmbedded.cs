using Signum.Utilities;
using System;
using System.IO;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileEmbedded : EmbeddedEntity, IFile
    {
        public FileEmbedded()
        {
        }

        public FileEmbedded(string readFileFrom)
        {
            this.FileName = Path.GetFileName(readFileFrom);
            this.BinaryFile = File.ReadAllBytes(readFileFrom);
        }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string FileName { get; set; }

        [NotNullValidator]
        public byte[] BinaryFile { get; set; }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(FileName, BinaryFile?.Let(bf => StringExtensions.ToComputerSize(bf.Length)) ?? "??");
        }


        public string FullWebPath()
        {
            return null;
        }
    }
}
