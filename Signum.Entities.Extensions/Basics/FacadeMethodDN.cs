using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class FacadeMethodDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
