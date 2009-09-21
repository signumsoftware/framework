using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Signum.Utilities;
using System.Threading;
using System.Security.Cryptography;
using System.ComponentModel;

namespace Signum.Entities.Authorization
{
    [Serializable, LocDescription]
    public class UserDN : Entity, IPrincipal
    {
        [NotNullable, UniqueIndex, SqlDbType(Size = 100)]
        string userName;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100), LocDescription]
        public string UserName
        {
            get { return userName; }
            set { SetToStr(ref userName, value, "UserName"); }
        }

        [NotNullable]
        string passwordHash;
        [NotNullValidator, LocDescription]
        public string PasswordHash
        {
            get { return passwordHash; }
            set { Set(ref passwordHash, value, "PasswordHash"); }
        }

        Lazy<IEmployeeDN> employee;
        [LocDescription]
        public Lazy<IEmployeeDN> Employee
        {
            get { return employee; }
            set { Set(ref employee, value, "Related;"); }
        }

        RoleDN role;
        [NotNullValidator, LocDescription]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        string email;
        [EmailValidator, LocDescription]
        public string EMail
        {
            get { return email; }
            set { Set(ref email, value, "EMail"); }
        }

        IIdentity IPrincipal.Identity
        {
            get { return null; }
        }

        bool IPrincipal.IsInRole(string role)
        {
            return this.role.BreathFirst(a=>a.Roles).Any(a => a.Name == role); 
        }


        DateTime? anulationDate;
        public DateTime? AnulationDate
        {
            get { return anulationDate; }
            set { Set(ref anulationDate, value, "AnulationDate"); }
        } 

        UserState state;
        public UserState State
        {
            get { return state; }
            set { Set(ref state, value, "State"); }
        }

        protected override void PreSaving()
        {

            if (anulationDate != null && state != UserState.Disabled)
                throw new ApplicationException("The user state must be Anulated {0}".Formato(this.ToString()));
            
            base.PreSaving();
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
        [Description("Creado")]
        Createad,
        [Description("Anulado")]
        Disabled,
    }

    public enum UserOperation
    {
        [LocDescription]
        Create,
        [LocDescription]
        SaveNew,
        [LocDescription]
        Save,
        [LocDescription]
        Enable,
        [LocDescription]
        Disable,
    }

    public interface IEmployeeDN:IIdentifiable
    {

    }
}
