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
using Signum.Engine.Authorization;

namespace Signum.React.Authorization
{
    public static class AuthServer
    {
        public static bool MergeInvalidUsernameAndPasswordMessages = false;

        public static Action<ApiController, UserEntity> UserPreLogin;
        public static Action<UserEntity> UserLogged;
        public static Action UserLoggingOut;

        public static void Start(HttpConfiguration config, bool queries, bool types)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            ReflectionServer.GetContext = () => new
            {
                Culture = ReflectionServer.GetCurrentValidCulture(),
                Role = UserEntity.Current == null ? null : RoleEntity.Current,
            };

            if (TypeAuthLogic.IsStarted)
                ReflectionServer.AddTypeExtension = (ti, t) =>
                {
                    var allowed = TypeAuthLogic.GetAllowed(t).MaxUI();
                    ti.Extension.Add("allowed", allowed);
                };

            if (PropertyAuthLogic.IsStarted)
                ReflectionServer.AddPropertyExtension = (mi, pr) =>
                {
                    var allowed = pr.GetPropertyAllowed();
                    mi.Extension.Add("allowed", allowed);
                };

            if (OperationAuthLogic.IsStarted)
                ReflectionServer.AddOperationExtension = (oi, o) =>
                {
                    var allowed = OperationAuthLogic.GetOperationAllowed(o.OperationSymbol, inUserInterface: true);
                    oi.Extension.Add("allowed", allowed);
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

            if (queries)
            {
                Omnibox.OmniboxServer.IsFindable += queryName => QueryAuthLogic.GetQueryAllowed(queryName);
            }

            if (types)
            {
                Omnibox.OmniboxServer.IsNavigable += type => TypeAuthLogic.GetAllowed(type).MaxUI() >= TypeAllowedBasic.Read;
            }
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