using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Security.Authentication;
using Signum.Entities.Extensions.Properties;

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
            set { SetToStr(ref name, value, () => Name); }
        }

        MList<Lite<RoleDN>> roles;
        public MList<Lite<RoleDN>> Roles
        {
            get { return roles; }
            set { Set(ref roles, value, () => Roles); }
        }

        public override string ToString()
        {
            return name;
        }

        public static RoleDN Current
        {
            get
            {
                UserDN user = UserDN.Current;
                if (user == null)
                    throw new AuthenticationException(Resources.NotUserLogged);

                return user.Role;
            }
        }
    }
    public enum RoleQueries
    {
        ReferedBy
    }
}
