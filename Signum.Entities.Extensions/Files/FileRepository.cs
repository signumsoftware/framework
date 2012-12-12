using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Linq.Expressions;
using System.IO;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileTypeDN : MultiEnumDN
    {
    }

    [Serializable]
    public class FileRepositoryDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        [NotNullable, SqlDbType(Size = 500)]
        string physicalPrefix;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 500)]
        public string PhysicalPrefix
        {
            get { return physicalPrefix; }
            set { Set(ref physicalPrefix, value, () => PhysicalPrefix); }
        }


        static Expression<Func<FileRepositoryDN, string>> FullPhysicalPrefixExpression = fr => ConvertToAbsolute(fr.PhysicalPrefix);
        public string FullPhysicalPrefix
        {
            get { return ConvertToAbsolute(PhysicalPrefix); }
        }

        static string ConvertToAbsolute(string phsicalPrefix)
        {
            if (!Path.IsPathRooted(phsicalPrefix) && OverridenPhisicalCurrentDirectory != null)
                return Path.Combine(OverridenPhisicalCurrentDirectory, phsicalPrefix);

            return phsicalPrefix;
        }

        public static string OverridenPhisicalCurrentDirectory; 

        [SqlDbType(Size = 500)]
        string webPrefix;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 500)]
        public string WebPrefix
        {
            get { return webPrefix; }
            set { Set(ref webPrefix, value, () => WebPrefix); }
        }

        bool active = true;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value, () => Active); }
        }

        [NotNullable]
        MList<FileTypeDN> fileTypes = new MList<FileTypeDN>();
        public MList<FileTypeDN> FileTypes
        {
            get { return fileTypes; }
            set { Set(ref fileTypes, value, () => FileTypes); }
        }

        static readonly Expression<Func<FileRepositoryDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum FileRepositoryOperation
    { 
        Save
    }
}
