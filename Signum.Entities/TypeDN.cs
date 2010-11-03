using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public class TypeDN : IdentifiableEntity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value, () => FullClassName); }
        }

        [NotNullable, UniqueIndex]
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


        public string Namespace
        {
            get
            {
                return FullClassName.Substring(0, FullClassName.LastIndexOf('.').NotFound(0));
            }
        }

        public string ClassName
        {
            get
            {
                return FullClassName.Substring(FullClassName.LastIndexOf('.') + 1);
            }
        }
    }
}
