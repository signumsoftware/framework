using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Signum.Utilities;
using System.Threading;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Reflection;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.Mailing;
using Signum.Entities.Extensions.Authorization;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class UserDN : Entity, IPrincipal, IEmailOwnerDN
    {
        [NotNullable, UniqueIndex, SqlDbType(Size = 100)]
        string userName;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string UserName
        {
            get { return userName; }
            set { SetToStr(ref userName, value, () => UserName); }
        }

        [NotNullable]
        string passwordHash;
        [NotNullValidator]
        public string PasswordHash
        {
            get { return passwordHash; }
            set { Set(ref passwordHash, value, () => PasswordHash); }
        }

        IUserRelatedDN related;
        public IUserRelatedDN Related
        {
            get { return related; }
            set { Set(ref related, value, () => Related); }
        }

        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, () => Role); }
        }

        string email;
        [EMailValidator]
        public string Email
        {
            get { return email; }
            set { Set(ref email, value, () => Email); }
        }

        IIdentity IPrincipal.Identity
        {
            get { return null; }
        }

        bool IPrincipal.IsInRole(string role)
        {
            return this.role.Name == role;
        }


        DateTime? anulationDate;
        public DateTime? AnulationDate
        {
            get { return anulationDate; }
            set { Set(ref anulationDate, value, () => AnulationDate); }
        } 

        UserState state;
        public UserState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        public static Expression<Func<UserDN, string>> CultureInfoExpression = 
            u => null;
        public string CultureInfo
        {
            get { return CultureInfoExpression.Invoke(this); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            
            if (pi.Is(()=>State))
            {
                if (anulationDate != null && state != UserState.Disabled)
                    return Resources.TheUserStateMustBeDisabled;
            }

            return base.PropertyValidation(pi);
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


    public enum UserState
    {
        Created,
        Disabled,
    }

    public enum UserOperation
    {
        Create,
        SaveNew,
        Save,
        Enable,
        Disable,
        NewPassword
    }

    public interface IUserRelatedDN : IIdentifiable
    {

    }
}
