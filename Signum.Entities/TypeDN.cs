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
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value, () => FullClassName); }
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

            return FullClassName == type.FullName;
        }
    }
}
