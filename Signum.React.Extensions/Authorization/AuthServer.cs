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
using Signum.React.Maps;
using Signum.Engine.Basics;
using Signum.React.Map;

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
                    if (typeof(Entity).IsAssignableFrom(t))
                        ti.Extension.Add("allowed", UserEntity.Current == null ? TypeAllowedBasic.None : TypeAuthLogic.GetAllowed(t).MaxUI());
                };

            if (PropertyAuthLogic.IsStarted)
                ReflectionServer.AddPropertyRouteExtension = (mi, pr) =>
                {
                    mi.Extension.Add("allowed", UserEntity.Current == null ?  PropertyAllowed.None: pr.GetPropertyAllowed());
                };

            if (OperationAuthLogic.IsStarted)
                ReflectionServer.AddOperationExtension = (oi, o) =>
                {
                    oi.Extension.Add("allowed", UserEntity.Current == null ? false : OperationAuthLogic.GetOperationAllowed(o.OperationSymbol, inUserInterface: true));
                };

            if (PermissionAuthLogic.IsStarted)
                ReflectionServer.AddFieldInfoExtension = (mi, fi) =>
                {
                    if (fi.FieldType == typeof(PermissionSymbol))
                        mi.Extension.Add("allowed", UserEntity.Current == null ? false : PermissionAuthLogic.IsAuthorized((PermissionSymbol)fi.GetValue(null)));
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


            SchemaMap.GetColorProviders += GetMapColors;
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


        static MapColorProvider[] GetMapColors()
        {
            if (!BasicPermission.AdminRules.IsAuthorized())
                return new MapColorProvider[0];

            var roleRules = AuthLogic.RolesInOrder().ToDictionary(r => r,
                r => TypeAuthLogic.GetTypeRules(r).Rules.ToDictionary(a => a.Resource.CleanName, a => a.Allowed));

            return roleRules.Keys.Select((r, i) => new MapColorProvider
            {
                Name = "role-" + r.Key(),
                NiceName = "Role - " + r.ToString(),
                AddExtra = t =>
                {
                    TypeAllowedAndConditions tac = roleRules[r].TryGetC(t.typeName);

                    if (tac == null)
                        return;

                    t.extra["role-" + r.Key() + "-ui"] = GetName(ToStringList(tac, userInterface: true));
                    t.extra["role-" + r.Key() + "-db"] = GetName(ToStringList(tac, userInterface: false));
                    t.extra["role-" + r.Key() + "-tooltip"] = ToString(tac.Fallback) + "\n" + (tac.Conditions.IsNullOrEmpty() ? null :
                        tac.Conditions.ToString(a => a.TypeCondition.NiceToString() + ": " + ToString(a.Allowed), "\n") + "\n");
                },
                Order = 10,
            }).ToArray();
        }

        static string GetName(List<TypeAllowedBasic?> list)
        {
            return "auth-" + list.ToString(a => a == null ? "Error" : a.ToString(), "-");
        }

        static List<TypeAllowedBasic?> ToStringList(TypeAllowedAndConditions tac, bool userInterface)
        {
            List<TypeAllowedBasic?> result = new List<TypeAllowedBasic?>();
            result.Add(tac.Fallback == null ? (TypeAllowedBasic?)null : tac.Fallback.Value.Get(userInterface));

            foreach (var c in tac.Conditions)
                result.Add(c.Allowed.Get(userInterface));

            return result;
        }


        private static string ToString(TypeAllowed? typeAllowed)
        {
            if (typeAllowed == null)
                return "MERGE ERROR!";

            if (typeAllowed.Value.GetDB() == typeAllowed.Value.GetUI())
                return typeAllowed.Value.GetDB().NiceToString();

            return "DB {0} / UI {1}".FormatWith(typeAllowed.Value.GetDB().NiceToString(), typeAllowed.Value.GetUI().NiceToString());
        }

    }
}