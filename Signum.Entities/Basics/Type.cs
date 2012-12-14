using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable, EntityType(EntityType.SystemString)]
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

        [NotNullable, UniqueIndex]
        string cleanName;
        public string CleanName
        {
            get { return cleanName; }
            set { Set(ref cleanName, value, () => CleanName); }
        }

        static Expression<Func<TypeDN, string>> ToStringExpression = e => e.CleanName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
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
