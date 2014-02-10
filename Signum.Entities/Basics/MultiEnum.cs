using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;
namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public abstract class MultiEnumDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string key;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Key
        {
            get { return key; }
            internal set { Set(ref key, value, () => Key); }
        }

        static readonly Expression<Func<MultiEnumDN, string>> ToStringExpression = e => e.key;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static string UniqueKey(Enum a)
        {
            return "{0}.{1}".Formato(a.GetType().Name, a.ToString());
        }
    }
}
