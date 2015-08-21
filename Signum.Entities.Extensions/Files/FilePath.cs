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

    [Serializable]
    public class FileTypeSymbol : Symbol
    {
        private FileTypeSymbol() { }

        public FileTypeSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }



    [AutoInit]
    public static class FilePathOperation
    {
        public static ExecuteSymbol<FilePathEntity> Save;
    }


    public class HttpFilePathUtils
    {
        public static string UrlPathEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            int index = value.IndexOf('?');
            if (index >= 0)
            {
                return (UrlPathEncode(value.Substring(0, index)) + value.Substring(index));
            }
            return UrlEncodeSpaces(UrlEncodeNonAscii(value, Encoding.UTF8));
        }

        static string UrlEncodeSpaces(string str)
        {
            if ((str != null) && (str.IndexOf(' ') >= 0))
            {
                str = str.Replace(" ", "%20");
            }
            return str;
        }

        static string UrlEncodeNonAscii(string str, Encoding e)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            if (e == null)
            {
                e = Encoding.UTF8;
            }
            byte[] bytes = e.GetBytes(str);
            byte[] buffer2 = UrlEncodeNonAscii(bytes, 0, bytes.Length, false);
            return Encoding.ASCII.GetString(buffer2);
        }

        static byte[] UrlEncodeNonAscii(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }
            int num = 0;
            for (int i = 0; i < count; i++)
            {
                if (IsNonAsciiByte(bytes[offset + i]))
                {
                    num++;
                }
            }
            if (!alwaysCreateNewReturnValue && (num == 0))
            {
                return bytes;
            }
            byte[] buffer = new byte[count + (num * 2)];
            int num3 = 0;
            for (int j = 0; j < count; j++)
            {
                byte b = bytes[offset + j];
                if (IsNonAsciiByte(b))
                {
                    buffer[num3++] = 0x25;
                    buffer[num3++] = (byte)IntToHex((b >> 4) & 15);
                    buffer[num3++] = (byte)IntToHex(b & 15);
                }
                else
                {
                    buffer[num3++] = b;
                }
            }
            return buffer;
        }


        static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return false;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return true;
        }

        static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }

        static bool IsNonAsciiByte(byte b)
        {
            if (b < 0x7f)
            {
                return (b < 0x20);
            }
            return true;
        }
    }
}
