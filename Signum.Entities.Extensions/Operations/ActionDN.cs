using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization; 

namespace Signum.Entities.Operations
{
    [Serializable]
    public class ActionDN : IdentifiableEntity
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

        public static string UniqueKey(Enum enumValue)
        {
            return "{0}.{1}".Formato(enumValue.GetType().Name, enumValue.ToString());
        }

        public static ActionDN FromEnum(Enum enumValue)
        {
            return new ActionDN
            {
                Key = UniqueKey(enumValue),
                Name = EnumExtensions.NiceToString((object)enumValue)
            };
        }
    }

    [Serializable]
    public class ActionInfo
    {
        public Enum ActionKey { get; set; }
        public ActionType ActionType { get; set; }
        public bool CanExecute { get; set; }
    }

    public enum ActionType
    {
        Entity = 1,
        Lazy = 2, 
        Both = 3,
    }
}
