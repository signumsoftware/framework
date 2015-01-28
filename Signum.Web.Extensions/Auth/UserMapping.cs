using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Web;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Web.Mvc;
using Signum.Entities.Authorization;
using Signum.Services;

namespace Signum.Web.Auth
{
    public static class UserMapping
    {
        public static readonly string OldPasswordKey = "OldPassword";
        public static readonly string NewPasswordKey = "NewPassword";
        public static readonly string NewPasswordBisKey = "NewPasswordBis";
        public static readonly string UserNameKey = "UserName"; 

        public static Mapping<UserEntity> ChangePasswordOld = new EntityMapping<UserEntity>(false)
            .SetProperty(u => u.PasswordHash, ctx =>
            {
                string oldPassword = ctx.Parent.Inputs[OldPasswordKey];
                if (ctx.Value != Security.EncodePassword(oldPassword))
                    return ctx.ParentNone(OldPasswordKey, AuthMessage.PasswordDoesNotMatchCurrent.NiceToString());

                return GetNewPassword(ctx, NewPasswordKey, NewPasswordBisKey);
            });

        public static EntityMapping<UserEntity> ChangePassword = new EntityMapping<UserEntity>(false)
            .SetProperty(u => u.PasswordHash, ctx =>
        {      
            return GetNewPassword(ctx, NewPasswordKey, NewPasswordBisKey);
        });

        public static EntityMapping<UserEntity> NewUser = new EntityMapping<UserEntity>(true)
            .SetProperty(u => u.PasswordHash, ctx =>
        {
            return GetNewPassword(ctx, NewPasswordKey, NewPasswordBisKey);
        });

        public static byte[] GetNewPassword(MappingContext<byte[]> ctx, string newPasswordKey, string newPasswordBisKey)
        {
            string newPassword = ctx.Parent.Inputs[newPasswordKey];
            if (string.IsNullOrEmpty(newPassword))
                return ctx.ParentNone(newPasswordKey, AuthMessage.PasswordMustHaveAValue.NiceToString());

            string newPasswordBis = ctx.Parent.Inputs[newPasswordBisKey];
            if (string.IsNullOrEmpty(newPasswordBis))
                return ctx.ParentNone(newPasswordBisKey, AuthMessage.YouMustRepeatTheNewPassword.NiceToString());

            if (newPassword != newPasswordBis)
                return ctx.ParentNone(newPasswordBisKey, AuthMessage.TheSpecifiedPasswordsDontMatch.NiceToString());

            return Security.EncodePassword(newPassword);
        }
    }    
}
