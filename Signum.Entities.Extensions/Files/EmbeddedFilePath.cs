using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.ComponentModel;
using System.IO;

namespace Signum.Entities.Files
{
    [Serializable]
    public class EmbeddedFilePathEntity : EmbeddedEntity, IFile, IFilePath
    {
        public static string ForceExtensionIfEmpty = ".dat";

        public EmbeddedFilePathEntity() { }

        public EmbeddedFilePathEntity(FileTypeSymbol fileType)
        {
            this.FileType = fileType;
        }

        public EmbeddedFilePathEntity(FileTypeSymbol fileType, string path)
            : this(fileType)
        {
            this.FileName = Path.GetFileName(path);
            this.BinaryFile = File.ReadAllBytes(path);
        }

        public EmbeddedFilePathEntity(FileTypeSymbol fileType, string fileName, byte[] fileData)
            : this(fileType)
        {
            this.FileName = fileName;
            this.BinaryFile = fileData;
        }

        [NotNullable, SqlDbType(Size = 260)]
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

        public string FileLengthString
        {
            get { return ((long)FileLength).ToComputerSize(true); }
        }

        [NotNullable, SqlDbType(Size = 260)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 260)]
        public string Sufix { get; set; }

        [Ignore]
        public string CalculatedDirectory { get; set; }

        [NotNullable]
        public FileTypeSymbol FileType { get; internal set; }

        [Ignore]
        internal PrefixPair prefixPair;
        public void SetPrefixPair(PrefixPair prefixPair)
        {
            this.prefixPair = prefixPair;
        }

        public string FullPhysicalPath
        {
            get
            {
                if (prefixPair == null)
                    throw new InvalidOperationException("prefixPair not set");

                return Path.Combine(prefixPair.PhysicalPrefix, Sufix);
            }
        }

        public string FullWebPath
        {
            get
            {
                if (prefixPair == null)
                    throw new InvalidOperationException("prefixPair not set");

                return string.IsNullOrEmpty(prefixPair.WebPrefix) ? null : prefixPair.WebPrefix + "/" + HttpFilePathUtils.UrlPathEncode(Sufix.Replace("\\", "/"));
            }
        }

        public override string ToString()
        {
            return "{0} - {1}".FormatWith(FileName, ((long)FileLength).ToComputerSize(true));
        }

        public static Action<EmbeddedFilePathEntity> OnPreSaving;
        protected override void PreSaving(ref bool graphModified)
        {
            if (OnPreSaving == null)
                throw new InvalidOperationException("OnPreSaving not set");

            OnPreSaving(this);
        }

        public static Action<EmbeddedFilePathEntity> OnPostRetrieved;

        protected override void PostRetrieving()
        {
            if (OnPostRetrieved == null)
                throw new InvalidOperationException("OnPostRetrieved not set");

            OnPostRetrieved(this);
        }
    }
}
