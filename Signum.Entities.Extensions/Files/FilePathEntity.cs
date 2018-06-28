using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using System.IO;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Web;
using Signum.Entities.Patterns;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Files
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Transactional)]
    public class FilePathEntity : LockableEntity, IFile, IFilePath
    {
        public static string ForceExtensionIfEmpty = ".dat";

        public FilePathEntity() { }

        public FilePathEntity(FileTypeSymbol fileType)
        {
            this.FileType = fileType;
        }

        public FilePathEntity(FileTypeSymbol fileType, string path)
            : this(fileType)
        {
            this.FileName = Path.GetFileName(path);
            this.BinaryFile = File.ReadAllBytes(path);
        }

        public FilePathEntity(FileTypeSymbol fileType, string fileName, byte[] fileData)
            : this(fileType)
        {
            this.FileName = fileName;
            this.BinaryFile = fileData;
        }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        string fileName;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 260), FileNameValidator]
        public string FileName
        {
            get { return fileName; }
            set
            {
                var newValue = fileName;
                if (ForceExtensionIfEmpty.HasText() && !Path.GetExtension(value).HasText())
                    value += ForceExtensionIfEmpty;

                Set(ref fileName, value);
            }
        }

        [Ignore]
        byte[] binaryFile;
        public byte[] BinaryFile
        {
            get { return binaryFile; }
            set
            {
                if (Set(ref binaryFile, value) && binaryFile != null)
                    FileLength = binaryFile.Length;
            }
        }

        public int FileLength { get; internal set; }

        static Expression<Func<FilePathEntity, string>> FileLengthStringExpression =
          @this => ((long)@this.FileLength).ToComputerSize(true);
        [ExpressionField]
        public string FileLengthString
        {
            get { return FileLengthStringExpression.Evaluate(this); }
        }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 260)]
        public string Suffix { get; set; }

        [Ignore]
        public string CalculatedDirectory { get; set; }

        [NotNullValidator]
        public FileTypeSymbol FileType { get; internal set; }

        [Ignore]
        internal PrefixPair _prefixPair;
        public void SetPrefixPair(PrefixPair prefixPair)
        {
            this._prefixPair = prefixPair;
        }

        public PrefixPair GetPrefixPair()
        {
            if (this._prefixPair != null)
                return this._prefixPair;

            if (CalculatePrefixPair == null)
                throw new InvalidOperationException("OnCalculatePrefixPair not set");

            this._prefixPair = CalculatePrefixPair(this);

            return this._prefixPair;
        }

        public static Func<FilePathEntity, PrefixPair> CalculatePrefixPair;

        public string FullPhysicalPath()
        {
            var pp = this.GetPrefixPair();

            return FilePathUtils.SafeCombine(pp.PhysicalPrefix, Suffix);
        }

        public string FullWebPath()
        {
            var pp = this.GetPrefixPair();

            if (string.IsNullOrEmpty(pp.WebPrefix))
                return null;

            var result = pp.WebPrefix + "/" + FilePathUtils.UrlPathEncode(Suffix.Replace("\\", "/"));

            if (result.StartsWith("http"))
                return result;

            return VirtualPathUtility.ToAbsolute(result);
        }

        public override string ToString()
        {
            return "{0} - {1}".FormatWith(FileName, ((long)FileLength).ToComputerSize(true));
        }

        protected override void PostRetrieving()
        {
            if (CalculatePrefixPair == null)
                throw new InvalidOperationException("OnCalculatePrefixPair not set");

            this.GetPrefixPair();
        }
    }

    [Serializable]
    public class PrefixPair
    {
        public PrefixPair(string physicalPrefix)
        {
            this.PhysicalPrefix = physicalPrefix;
        }

        public string PhysicalPrefix;
        public string WebPrefix;
    }



    [AutoInit]
    public static class FilePathOperation
    {
        public static ExecuteSymbol<FilePathEntity> Save;
    }
}
