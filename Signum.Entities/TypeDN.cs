using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [Serializable]
    public class TypeDN : IdentifiableEntity
    {
        [UniqueIndex]
        string className;
        public string ClassName
        {
            get { return className; }
            set { Set(ref className, value, () => ClassName); }
        }

        [UniqueIndex]
        string tableName;
        public string TableName
        {
            get { return tableName; }
            set { Set(ref tableName, value, () => TableName); }
        }

        [NotNullable]
        string friendlyName;
        [StringLengthValidator(Min=1)]
        public string FriendlyName
        {
            get { return friendlyName; }
            set { Set(ref friendlyName, value, () => FriendlyName); }
        }

        public override string ToString()
        {
            return friendlyName; 
        }

        public bool IsType(Type type)
        {
            if (type == null)
                throw new ArgumentException("type");

            return ClassName == type.Name;
        }
    }
}
