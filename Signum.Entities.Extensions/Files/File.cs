using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using System.IO;
using Signum.Utilities;
using Signum.Services;

namespace Signum.Entities.Files
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class FileDN : ImmutableEntity, IFile
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

        string hash;
        public string Hash
        {
            get { return hash; }
            private set { Set(ref hash, value, () => Hash); }
        }

        byte[] binaryFile;
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set
            {
                if (Set(ref binaryFile, value, () => BinaryFile))
                    Hash = CryptorEngine.CalculateMD5Hash(binaryFile);
            }
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
