using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.IO;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;
using System.ComponentModel;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FilePathDN : Entity, IFile
    {
        private FilePathDN() { }

        public FilePathDN(FileTypeDN fileType)
        {
            this.FileType = fileType;
        }

        public FilePathDN(Enum fileType)
        {
            this.fileTypeEnum = fileType;
        }

        public FilePathDN(Enum fileType, string path)
            : this(fileType)
        {
            this.fileName = Path.GetFileName(path);
            this.binaryFile = File.ReadAllBytes(path);
        }

        [NotNullable, SqlDbType(Size = 260)]
        string fileName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 260), FileNameValidator, LocDescription]
        public string FileName
        {
            get { return fileName; }
            set { SetToStr(ref fileName, value, () => FileName); }
        }

        [Ignore]
        byte[] binaryFile;
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set 
            {
                if (Set(ref binaryFile, value, () => BinaryFile))
                    fileLength = binaryFile.Length;
            }
        }

        int fileLength;
        public int FileLength
        {
            get { return fileLength; }
            private set { Set(ref fileLength, value, () => FileLength); }
        }

        [NotNullable, SqlDbType(Size = 260)]
        string sufix;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 260), LocDescription]
        public string Sufix
        {
            get { return sufix; }
            internal set { Set(ref sufix, value, () => Sufix); }
        }

        [Ignore]
        Enum fileTypeEnum;
        public Enum FileTypeEnum
        {
            get { return fileTypeEnum; }
        }

        public void SetFileTypeEnum(Enum ftEnum)
        {
            fileTypeEnum = ftEnum;
        }

        FileTypeDN fileType;
        [LocDescription]
        public FileTypeDN FileType
        {
            get { return fileType; }
            internal set { Set(ref fileType, value, () => FileType); }
        }

        FileRepositoryDN repository;
        [LocDescription]
        public FileRepositoryDN Repository
        {
            get { return repository; }
            internal set { Set(ref repository, value, () => Repository); }
        }

        static Expression<Func<FilePathDN, string>> FullPhysicalPathExpression = fp => fp.Repository.PhysicalPrefix + '\\' + fp.Sufix;
        public string FullPhysicalPath
        {
            get { return Repository == null ? null : Repository.PhysicalPrefix + '\\' + Sufix; }
        }

        static Expression<Func<FilePathDN, string>> FullWebPathExpression = fp => 
            fp.Repository != null && fp.Repository.WebPrefix.HasText() ? 
                fp.Repository.WebPrefix + "/" + fp.Sufix.Replace('\\', '/').Replace(" ", "") :
                string.Empty;
        public string FullWebPath
        {
            get { return Repository != null && Repository.WebPrefix.HasText() ? 
                    Repository.WebPrefix + "/" + Sufix.Replace('\\', '/').Replace(" ", "") : 
                    string.Empty; }
        }

        public Uri WebPath
        {
            get { return FullWebPath.HasText() ? new Uri(FullWebPath) : null; }
        }
    }


}
