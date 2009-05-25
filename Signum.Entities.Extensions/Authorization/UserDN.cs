using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Signum.Utilities;
using System.Threading;
using System.Security.Cryptography;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class UserDN : Entity, IPrincipal
    {
        [NotNullable, UniqueIndex, SqlDbType(Size = 100)]
        string userName;
        [StringLengthValidator(AllowNulls = false, Min = 4, Max = 100)]
        public string UserName
        {
            get { return userName; }
            set { SetToStr(ref userName, value, "UserName"); }
        }

        [NotNullable]
        string passwordHash;
        [NotNullValidator]
        public string PasswordHash
        {
            get { return passwordHash; }
            set { Set(ref passwordHash, value, "PasswordHash"); }
        }

        //ImplementedBy this
        Lazy<IdentifiableEntity> related;
        public Lazy<IdentifiableEntity> Related
        {
            get { return related; }
            set { Set(ref related, value, "Related;"); }
        }

        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }
    
        public IIdentity Identity
        {
            get { return null; }
        }

        public bool IsInRole(string role)
        {
            return this.role.BreathFirst(a=>a.Roles).Any(a => a.Name == role); 
        }

        public override string ToString()
        {
            return userName;
        }

        public static UserDN Current
        {
            get { return Thread.CurrentPrincipal as UserDN; }
        }
    }
}
