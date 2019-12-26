using System;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Mailing;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Entities;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class UserEntity : Entity, IEmailOwnerEntity, IUserEntity
    {
        public static Func<string, string?> ValidatePassword = p =>
        {
            if (p.Length >= 5)
                return null;

            return AuthMessage.ThePasswordMustHaveAtLeast5Characters.NiceToString();
        };

        public static string? OnValidatePassword(string password)
        {
            if (ValidatePassword != null)
                return ValidatePassword(password);

            return null;
        }

        [UniqueIndex(AvoidAttachToUniqueIndexes = true)]
        [StringLengthValidator(Min = 2, Max = 100)]
        public string UserName { get; set; }

        [SqlDbType(Size = 128)]
        public byte[] PasswordHash { get; set; }

        public Lite<RoleEntity> Role { get; set; }

        [StringLengthValidator(Max = 200), EMailValidator]
        public string? Email { get; set; }

        public CultureInfoEntity? CultureInfo { get; set; }

        public DateTime? DisabledOn { get; set; }

        public UserState State { get; set; } = UserState.New;

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(State))
            {
                if (DisabledOn != null && State != UserState.Disabled)
                    return AuthMessage.TheUserStateMustBeDisabled.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => UserName);

        public static UserEntity Current
        {
            get { return (UserEntity)UserHolder.Current; }
            set { UserHolder.Current = value; }
        }

        [AutoExpressionField]
        public EmailOwnerData EmailOwnerData => As.Expression(() => new EmailOwnerData
        {
            Owner = this.ToLite(),
            CultureInfo = CultureInfo,
            DisplayName = UserName,
            Email = Email!,
        });
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


    [Serializable]
    public class UserOIDMixin : MixinEntity
    {
        UserOIDMixin(ModifiableEntity mainEntity, MixinEntity? next)
            : base(mainEntity, next)
        {
        }

        public Guid? OID { get; set; }
    }
}
