using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    [Serializable]
    public abstract class EnumDN : IdentifiableEntity
    {
        public static T New<T>(Enum key)
            where T:EnumDN, new()
        {
            return new T
            {
                Key = UniqueKey(key),
                Name = key.NiceToString(),
            }; 
        }

        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), LocDescription]
        public string Name
        {
            get { return name; }
            internal set { SetToStr(ref name, value, "Name"); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string key;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), LocDescription]
        public string Key
        {
            get { return key; }
            internal set { Set(ref key, value, "Key"); }
        }

        public override string ToString()
        {
            return name;
        }

        public static string UniqueKey(Enum a)
        {
            return "{0}.{1}".Formato(a.GetType().Name, a.ToString());
        }
    }
}
