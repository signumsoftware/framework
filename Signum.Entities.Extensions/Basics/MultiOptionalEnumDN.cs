using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.String)]
    public class MultiOptionalEnumDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set
            {
                if (key != null)
                    throw new ApplicationException("This alert type is protected");

                SetToStr(ref name, value, () => Name);
            }
        }

        [SqlDbType(Size = 100), UniqueIndex(AllowMultipleNulls = true)]
        string key;
        public string Key
        {
            get { return key; }
            set { Set(ref key, value, () => Key); }
        }

        static readonly Expression<Func<MultiOptionalEnumDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
