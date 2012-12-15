using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Security.Authentication;
using Signum.Entities.Extensions.Properties;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityType(EntityType.Shared)]
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

        [NotNullable]
        MList<Lite<RoleDN>> roles = new MList<Lite<RoleDN>>();
        public MList<Lite<RoleDN>> Roles
        {
            get { return roles; }
            set { Set(ref roles, value, () => Roles); }
        }

        static readonly Expression<Func<RoleDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
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

    public enum RoleOperation
    {
        Save,
        Delete
    }
}
