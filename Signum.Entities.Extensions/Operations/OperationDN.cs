using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization; 

namespace Signum.Entities.Operations
{
    [Serializable]
    public class OperationDN : IdentifiableEntity
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

        public static string UniqueKey(Enum operationKey)
        {
            return "{0}.{1}".Formato(operationKey.GetType().Name, operationKey.ToString());
        }

        public static OperationDN FromEnum(Enum operationKey)
        {
            return new OperationDN
            {
                Key = UniqueKey(operationKey),
                Name = EnumExtensions.NiceToString(operationKey)
            };
        }
    }

    [Serializable]
    public class OperationInfo
    {
        public Enum Key { get; set; }
        public OperationType OperationType { get; set; }
        public bool CanExecute { get; set; }
        public bool Lazy { get; set; }
        public bool Returns { get; set; }
    }


    [Flags]
    public enum OperationType
    {
        Execute, 
        Constructor, 
        ConstructorFrom,
        ConstructorFromMany
    }
}
