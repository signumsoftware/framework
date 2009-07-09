using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class PermissionDN : IdentifiableEntity
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

        public static string UniqueKey(Enum permissionKey)
        {
            return "{0}.{1}".Formato(permissionKey.GetType().Name, permissionKey.ToString());
        }

        public static PermissionDN FromEnum(Enum permissionKey)
        {
            return new PermissionDN
            {
                Key = UniqueKey(permissionKey),
                Name = EnumExtensions.NiceToString(permissionKey)
            };
        }
    }

    public enum BasicPermissions
    {
        AdminRules
    }
}
