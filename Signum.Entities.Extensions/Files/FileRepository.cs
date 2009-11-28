using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileTypeDN : EnumDN
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

        MList<FileTypeDN> fileTypes;
        public MList<FileTypeDN> FileTypes
        {
            get { return fileTypes; }
            set { Set(ref fileTypes, value, () => FileTypes); }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
