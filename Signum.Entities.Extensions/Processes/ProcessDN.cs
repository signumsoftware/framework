using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Utilities;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class ProcessDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string key;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Key
        {
            get { return key; }
            set { Set(ref key, value, "Key"); }
        }

        public override string ToString()
        {
            return name;
        }

        public static ProcessDN FromEnum(Enum operationKey)
        {
            return new ProcessDN
            {
                Key = EnumExtensions.UniqueKey(operationKey),
                Name = EnumExtensions.NiceToString(operationKey)
            };
        }
    }
}
