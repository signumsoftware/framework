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
using Signum.Entities.Mailing;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using System.Globalization;
using Signum.Entities.Translation;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class UserDN : Entity, IEmailOwnerDN, IUserDN
    {
        public static Func<string, string> ValidatePassword = p =>
        {
            if (Regex.Match(p, @"^[0-9a-zA-Z]{7,15}$").Success)
                return null;
            return AuthMessage.ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter.NiceToString();
        };

        public static string OnValidatePassword(string password)
        {
            if (ValidatePassword != null)
                return ValidatePassword(password);

            return null;
        }

        [NotNullable, UniqueIndex(AvoidAttachToUniqueIndexes=true), SqlDbType(Size = 100)]
        string userName;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string UserName
        {
            get { return userName; }
            set { SetToStr(ref userName, value); }
        }

        [NotNullable]
        string passwordHash;
        [NotNullValidator]
        public string PasswordHash
        {
            get { return passwordHash; }
            set
            {
                if (Set(ref passwordHash, value))
                    PasswordSetDate = TimeZoneManager.Now.TrimToSeconds();
            }
        }

        DateTime passwordSetDate;
        public DateTime PasswordSetDate
        {
            get { return passwordSetDate; }
            private set { Set(ref passwordSetDate, value); }
        }

        bool passwordNeverExpires;
        public bool PasswordNeverExpires
        {
            get { return passwordNeverExpires; }
            set { Set(ref passwordNeverExpires, value); }
        }
       
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value); }
        }

        string email;
        [EMailValidator]
        public string Email
        {
            get { return email; }
            set { Set(ref email, value); }
        }

        CultureInfoDN cultureInfo;
        public CultureInfoDN CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value); }
        }

        DateTime? anulationDate;
        public DateTime? AnulationDate
        {
            get { return anulationDate; }
            set { Set(ref anulationDate, value); }
        }

        UserState state = UserState.New;
        public UserState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => State))
            {
                if (anulationDate != null && state != UserState.Disabled)
                    return AuthMessage.TheUserStateMustBeDisabled.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<UserDN, string>> ToStringExpression = e => e.userName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static UserDN Current
        {
            get { return (UserDN)UserHolder.Current; }
            set { UserHolder.Current = value; }
        }

        public static Expression<Func<UserDN, EmailOwnerData>> EmailOwnerDataExpression = entity => new EmailOwnerData
        {
            Owner = entity.ToLite(),
            CultureInfo = entity.CultureInfo,
            DisplayName = entity.UserName,
            Email = entity.Email,
        };
        public EmailOwnerData EmailOwnerData
        {
            get{ return EmailOwnerDataExpression.Evaluate(this); }
        }
    }

    public enum UserState
    {
        [Ignore]
        New = -1,
        Saved,
        Disabled,
    }

    public static class UserOperation
    {
        public static readonly ConstructSymbol<UserDN>.Simple Create = OperationSymbol.Construct<UserDN>.Simple();
        public static readonly ExecuteSymbol<UserDN> SaveNew = OperationSymbol.Execute<UserDN>();
        public static readonly ExecuteSymbol<UserDN> Save = OperationSymbol.Execute<UserDN>();
        public static readonly ExecuteSymbol<UserDN> Enable = OperationSymbol.Execute<UserDN>();
        public static readonly ExecuteSymbol<UserDN> Disable = OperationSymbol.Execute<UserDN>();
        public static readonly ExecuteSymbol<UserDN> SetPassword = OperationSymbol.Execute<UserDN>();
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
