using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class RoleDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        MList<RoleDN> roles;
        public MList<RoleDN> Roles
        {
            get { return roles; }
            set { Set(ref roles, value, "Roles"); }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
