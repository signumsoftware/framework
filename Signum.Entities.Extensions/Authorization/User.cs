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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class UserEntity : Entity, IEmailOwnerEntity, IUserEntity
    {
        public static Func<string, string> ValidatePassword = p =>
        {
            if (p.Length >= 5)
                return null;

            return AuthMessage.ThePasswordMustHaveAtLeast5Characters.NiceToString();
        };

        public static string OnValidatePassword(string password)
        {
            if (ValidatePassword != null)
                return ValidatePassword(password);

            return null;
        }

        [UniqueIndex(AvoidAttachToUniqueIndexes = true)]
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string UserName { get; set; }

        [NotNullable, SqlDbType(Size = 128)]
        byte[] passwordHash;
        [NotNullValidator]
        public byte[] PasswordHash
        {
            get { return passwordHash; }
            set
            {
                if (Set(ref passwordHash, value))
                    PasswordSetDate = TimeZoneManager.Now.TrimToSeconds();
            }
        }

        public DateTime PasswordSetDate { get; private set; }

        public bool PasswordNeverExpires { get; set; }

        [NotNullValidator]
        public Lite<RoleEntity> Role { get; set; }

        [EMailValidator]
        public string Email { get; set; }

        public CultureInfoEntity CultureInfo { get; set; }

        public DateTime? AnulationDate { get; set; }

        public UserState State { get; set; } = UserState.New;

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(State))
            {
                if (AnulationDate != null && State != UserState.Disabled)
                    return AuthMessage.TheUserStateMustBeDisabled.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        public static Expression<Func<UserEntity, string>> ToStringExpression = e => e.UserName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static UserEntity Current
        {
            get { return (UserEntity)UserHolder.Current; }
            set { UserHolder.Current = value; }
        }

        public static Expression<Func<UserEntity, EmailOwnerData>> EmailOwnerDataExpression = u => new EmailOwnerData
        {
            Owner = u.ToLite(),
            CultureInfo = u.CultureInfo,
            DisplayName = u.UserName,
            Email = u.Email,
        };
        [ExpressionField]
        public EmailOwnerData EmailOwnerData
        {
            get { return EmailOwnerDataExpression.Evaluate(this); }
        }
    }

    public enum UserState
    {
        [Ignore]
        New = -1,
        Saved,
        Disabled,
    }

    [AutoInit]
    public static class UserOperation
    {
        public static ConstructSymbol<UserEntity>.Simple Create;
        public static ExecuteSymbol<UserEntity> SaveNew;
        public static ExecuteSymbol<UserEntity> Save;
        public static ExecuteSymbol<UserEntity> Enable;
        public static ExecuteSymbol<UserEntity> Disable;
        public static ExecuteSymbol<UserEntity> SetPassword;
    }

    [Serializable]
    public class IncorrectUsernameException : ApplicationException
    {
        public IncorrectUsernameException() { }
        public IncorrectUsernameException(string message) : base(message) { }
        protected IncorrectUsernameException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class IncorrectPasswordException : ApplicationException
    {
        public IncorrectPasswordException() { }
        public IncorrectPasswordException(string message) : base(message) { }
        protected IncorrectPasswordException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)    
            : base(info, context)
        { }
    }
}
