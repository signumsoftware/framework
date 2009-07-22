using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    public class PropertyDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value, "Type"); }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
