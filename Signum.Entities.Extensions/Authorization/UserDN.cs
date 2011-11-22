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
using System.Text.RegularExpressions;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class UserDN : Entity, IEmailOwnerDN
    {
        public UserDN()
        {
            PasswordHash = Guid.NewGuid().ToString();
        }

        public static Func<string, string> ValidatePassword = p =>
        {
            if (Regex.Match(p, @"^[0-9a-zA-Z]{7,15}$").Success)
                return null;
            return Resources.ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter;
        };


        public static string OnValidatePassword(string password)
        {
            if (ValidatePassword != null)
                return ValidatePassword(password);

            return null;
        }

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
            set
            {
                if (Set(ref passwordHash, value, () => PasswordHash))
                    PasswordSetDate = TimeZoneManager.Now.TrimToSeconds();
            }
        }

        DateTime passwordSetDate;
        public DateTime PasswordSetDate
        {
            get { return passwordSetDate; }
            private set { Set(ref passwordSetDate, value, () => PasswordSetDate); }
        }


        bool passwordNeverExpires;
        public bool PasswordNeverExpires
        {
            get { return passwordNeverExpires; }
            set { Set(ref passwordNeverExpires, value, () => PasswordNeverExpires); }
        }
       
        IIdentifiable related;
        public IIdentifiable Related
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

        DateTime? anulationDate;
        public DateTime? AnulationDate
        {
            get { return anulationDate; }
            set { Set(ref anulationDate, value, () => AnulationDate); }
        }

        UserState state = UserState.New;
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
            if (pi.Is(() => State))
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

        static readonly Variable<UserDN> sessionUser = Statics.SessionVariable<UserDN>("user");
        static readonly Variable<UserDN> threadUser = Statics.ThreadVariable<UserDN>("threadUser");

        public static void SetSessionUser(UserDN user)
        {
            sessionUser.Value = user;
        }

        public static IDisposable Scope(UserDN user)
        {
            var old = threadUser.Value;
            threadUser.Value = user;
            return new Disposable(() =>  threadUser.Value = old); 
        }

        public static UserDN Current
        {
            get { return threadUser.Value ?? sessionUser.Value; }
        }
    }


    public enum UserState
    {
        [Ignore]
        New = -1,
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
        SetPassword
    }


    [Serializable]
    public class IncorrectUsernameException : ApplicationException
    {
        public IncorrectUsernameException() { }
        public IncorrectUsernameException(string message) : base(message) { }
        protected IncorrectUsernameException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class IncorrectPasswordException : ApplicationException
    {
        public IncorrectPasswordException() { }
        public IncorrectPasswordException(string message) : base(message) { }
        protected IncorrectPasswordException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
