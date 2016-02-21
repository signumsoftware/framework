using Signum.Entities.Authorization;
using Signum.React.Facades;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Services;
using Signum.Entities.Reflection;

namespace Signum.React.Authorization
{
    public static class AuthServer
    {
        public static bool MergeInvalidUsernameAndPasswordMessages = false;

        public static Action<ApiController, UserEntity> UserPreLogin;
        public static Action<UserEntity> UserLogged;
        public static Action UserLoggingOut;

        public static void Start(HttpConfiguration config)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.GetContext = () => new
            {
                Culture = ReflectionServer.GetCurrentValidCulture(),
                Role = UserEntity.Current == null ? null : RoleEntity.Current,
            };

            var piPasswordHash = ReflectionTools.GetPropertyInfo((UserEntity e) => e.PasswordHash);
            var pcs = PropertyConverter.GetPropertyConverters(typeof(UserEntity));
            pcs.GetOrThrow("passwordHash").CustomWriteJsonProperty = ctx => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = ctx => { },
                CustomReadJsonProperty = ctx =>
                {
                    EntityJsonConverter.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPasswordHash));

                    var password = (string)ctx.JsonReader.Value;

                    var error = UserEntity.OnValidatePassword(password);
                    if (error != null)
                        throw new ApplicationException(error);
                    
                    ((UserEntity)ctx.Entity).PasswordHash = Security.EncodePassword(password);
                }
            });
        }

        public static void OnUserPreLogin(ApiController controller, UserEntity user)
        {
            AuthServer.UserPreLogin?.Invoke(controller, user);
        }

        public static void AddUserSession(UserEntity user)
        {
            UserEntity.Current = user;

            AuthServer.UserLogged?.Invoke(user);
        }
    }
}