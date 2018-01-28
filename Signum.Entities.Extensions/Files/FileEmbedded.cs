using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileEmbedded : EmbeddedEntity, IFile
    {
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
