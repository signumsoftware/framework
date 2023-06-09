using Signum.Entities.Authorization;
using Signum.React.Facades;
using Microsoft.AspNetCore.Mvc;
using Signum.Utilities.Reflection;
using Signum.Services;
using Signum.Engine.Authorization;
using Signum.React.Maps;
using Microsoft.AspNetCore.Builder;
using Signum.React.ApiControllers;
using System.Text.Json;
using Signum.Engine.Json;
using Signum.Entities.Basics;

namespace Signum.React.Authorization;

public static class AuthServer
{
    public static bool MergeInvalidUsernameAndPasswordMessages = false;

    public static Action<ActionContext, UserEntity> UserPreLogin;
    public static Action<ActionContext, UserEntity> UserLogged;
    public static Action<ActionContext, UserWithClaims> UserLoggingOut;

    
    public static void Start(IApplicationBuilder app, Func<AuthTokenConfigurationEmbedded> tokenConfig, string hashableEncryptionKey)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        AuthTokenServer.Start(tokenConfig, hashableEncryptionKey);

        ReflectionServer.GetContext = () => new
        {
            Culture = ReflectionServer.GetCurrentValidCulture(),
            Role = UserEntity.Current == null ? null : RoleEntity.Current,
        };

        AuthLogic.OnRulesChanged += ReflectionServer.InvalidateCache;

        if (TypeAuthLogic.IsStarted)
        {
            ReflectionServer.TypeExtension += (ti, t) =>
            {
                if (typeof(Entity).IsAssignableFrom(t))
                {
                    if (UserEntity.Current == null)
                        return null;
                    
                    var ta = TypeAuthLogic.GetAllowed(t);

                    if (ta.MaxUI() == TypeAllowedBasic.None)
                        return null;

                    ti.Extension.Add("maxTypeAllowed", ta.MaxUI());
                    ti.Extension.Add("minTypeAllowed", ta.MinUI());
                    ti.RequiresEntityPack |= ta.ConditionRules.Any();

                    return ti;
                }
                else
                {
                    if (t.HasAttribute<AllowUnathenticatedAttribute>())
                        return ti;

                    if (UserEntity.Current == null)
                        return null;

                    if (!AuthServer.IsNamespaceAllowed(t))
                        return null;

                    return ti;
                }
            };


            EntityPackTS.AddExtension += ep =>
            {
                var typeAllowed =
                UserEntity.Current == null ? TypeAllowedBasic.None :
                ep.entity.IsNew ? TypeAuthLogic.GetAllowed(ep.entity.GetType()).MaxUI() :
                TypeAuthLogic.IsAllowedFor(ep.entity, TypeAllowedBasic.Write, true) ? TypeAllowedBasic.Write :
                TypeAuthLogic.IsAllowedFor(ep.entity, TypeAllowedBasic.Read, true) ? TypeAllowedBasic.Read :
                TypeAllowedBasic.None;

                ep.extension.Add("typeAllowed", typeAllowed);
            };

            OperationController.AnyReadonly += (Lite<Entity>[] lites) =>
            {
                return lites.GroupBy(ap => ap.EntityType).Any(gr =>
                {
                    var ta = TypeAuthLogic.GetAllowed(gr.Key);

                    if (ta.Min(inUserInterface: true) == TypeAllowedBasic.Write)
                        return false;

                    if (ta.Max(inUserInterface: true) <= TypeAllowedBasic.Read)
                        return true;

                    return giCountReadonly.GetInvoker(gr.Key)() > 0;
                });
            };
        }

        if (QueryAuthLogic.IsStarted)
        {
            ReflectionServer.TypeExtension += (ti, t) =>
            {
                if (ti.QueryDefined)
                {
                    var allowed = UserEntity.Current == null ? QueryAllowed.None : QueryAuthLogic.GetQueryAllowed(t);
                    if (allowed == QueryAllowed.None)
                        ti.QueryDefined = false;

                    ti.Extension.Add("queryAllowed", allowed);
                }

                return ti;
            };

            ReflectionServer.FieldInfoExtension += (mi, fi) =>
            {
                if (fi.DeclaringType!.Name.EndsWith("Query"))
                {
                    var allowed = UserEntity.Current == null ? QueryAllowed.None : QueryAuthLogic.GetQueryAllowed(fi.GetValue(null)!);

                    if (allowed == QueryAllowed.None)
                        return null;

                    mi.Extension.Add("queryAllowed", allowed);
                }
                return mi;
            };
        }

        if (PropertyAuthLogic.IsStarted)
        {
            ReflectionServer.PropertyRouteExtension += (mi, pr) =>
            {
                var allowed = UserEntity.Current == null ? pr.GetAllowUnathenticated() : pr.GetPropertyAllowed();
                if (allowed == PropertyAllowed.None)
                    return null;

                mi.Extension.Add("propertyAllowed", allowed);
                return mi;
            };

            SignumServer.WebEntityJsonConverterFactory.CanReadPropertyRoute += (pr, mod) =>
            {
                var allowed = UserEntity.Current == null ? pr.GetAllowUnathenticated() : pr.GetPropertyAllowed();

                return allowed == PropertyAllowed.None ? "Not allowed" : null;
            };

            SignumServer.WebEntityJsonConverterFactory.CanWritePropertyRoute += (pr, mod) =>
            {
                var allowed = UserEntity.Current == null ? pr.GetAllowUnathenticated() : pr.GetPropertyAllowed();

                return allowed == PropertyAllowed.Write ? null : "Not allowed to write property: " + pr.ToString();
            };

            PropertyAuthLogic.GetPropertyConverters = (t) => SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(t);
        }

        if (OperationAuthLogic.IsStarted)
        {
            ReflectionServer.OperationExtension += (oits, oi, type) =>
            {
                var allowed = UserEntity.Current == null ? false :
                           OperationAuthLogic.GetOperationAllowed(oi.OperationSymbol, type, inUserInterface: true);

                if (!allowed)
                    return null;

                return oits;
            };

        }

        if (PermissionAuthLogic.IsStarted)
        {
            ReflectionServer.FieldInfoExtension += (mi, fi) =>
            {
                if (fi.FieldType == typeof(PermissionSymbol))
                {
                    var allowed = UserEntity.Current == null ? false :
                        PermissionAuthLogic.IsAuthorized((PermissionSymbol)fi.GetValue(null)!);

                    if (allowed == false)
                        return null;
                }

                return mi;
            };
        }

        var piPasswordHash = ReflectionTools.GetPropertyInfo((UserEntity e) => e.PasswordHash);
        var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(UserEntity));
        pcs.GetOrThrow("passwordHash").CustomWriteJsonProperty = (writer, ctx) => { };
        pcs.Add("newPassword", new PropertyConverter
        {
            AvoidValidate = true,
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPasswordHash), ctx.Entity);

                var password = reader.GetString();

                if (password == null)
                    ((UserEntity)ctx.Entity).PasswordHash = null;
                else
                {
                    var error = UserEntity.OnValidatePassword(password);
                    if (error != null)
                        throw new ApplicationException(error);

                    ((UserEntity)ctx.Entity).PasswordHash = Security.EncodePassword(password);
                }
            }
        });

        if (TypeAuthLogic.IsStarted)
            Omnibox.OmniboxServer.IsNavigable += type => TypeAuthLogic.GetAllowed(type).MaxUI() >= TypeAllowedBasic.Read;

        if (SessionLogLogic.IsStarted)
            AuthServer.UserLogged +=  (ActionContext ac, UserEntity user) =>
            {
                Microsoft.AspNetCore.Http.HttpRequest re = ac.HttpContext.Request;
                SessionLogLogic.SessionStart(
                    re.Host.ToString(),
                    re.Headers["User-Agent"].FirstOrDefault());
            };

        SchemaMap.GetColorProviders += GetMapColors;
    }

    public static ResetLazy<Dictionary<string, List<Type>>> entitiesByNamespace =
        new ResetLazy<Dictionary<string, List<Type>>>(() => Schema.Current.Tables.Keys.Where(t => !EnumEntity.IsEnumEntity(t)).GroupToDictionary(t => t.Namespace!));

    public static bool IsNamespaceAllowed(Type type)
    {
        var func = ReflectionServer.OverrideIsNamespaceAllowed.TryGetC(type.Namespace!);

        if (func != null)
            return func();

        var typesInNamespace = entitiesByNamespace.Value.TryGetC(type.Namespace!);
        if (typesInNamespace != null)
            return typesInNamespace.Any(t => TypeAuthLogic.GetAllowed(t).MaxUI() > TypeAllowedBasic.None);


        throw new InvalidOperationException(@$"Unable to determine whether the metadata for '{type.FullName}' should be delivered to the client for role '{RoleEntity.Current}' because there are no entities in the namespace '{type.Namespace!}'.");
    }



    static GenericInvoker<Func<int>> giCountReadonly = new(() => CountReadonly<Entity>());
    public static int CountReadonly<T>() where T : Entity
    {
        return Database.Query<T>().Count(a => !a.IsAllowedFor(TypeAllowedBasic.Write, true));
    }

    public static void OnUserPreLogin(ActionContext ac, UserEntity user)
    {
        AuthServer.UserPreLogin?.Invoke(ac, user);
    }

    public static void AddUserSession(ActionContext ac, UserEntity user)
    {
        UserHolder.Current = new UserWithClaims(user);

        AuthServer.UserLogged?.Invoke(ac, user);
    }

    static MapColorProvider[] GetMapColors()
    {
        if (!BasicPermission.AdminRules.IsAuthorized())
            return new MapColorProvider[0];

        var roleRules = AuthLogic.RolesInOrder(includeTrivialMerge: false).ToDictionary(r => r,
            r => TypeAuthLogic.GetTypeRules(r).Rules.ToDictionary(a => a.Resource.CleanName, a => a.Allowed));

        return roleRules.Keys.Select((r, i) => new MapColorProvider
        {
            Name = "role-" + r.Key(),
            NiceName = "Role - " + r.ToString(),
            AddExtra = t =>
            {
                TypeAllowedAndConditions? tac = roleRules[r].TryGetC(t.typeName);

                if (tac == null)
                    return;

                t.extra["role-" + r.Key() + "-ui"] = GetName(ToStringList(tac, userInterface: true));
                t.extra["role-" + r.Key() + "-db"] = GetName(ToStringList(tac, userInterface: false));
                t.extra["role-" + r.Key() + "-tooltip"] = ToString(tac.Fallback) + "\n" + (tac.ConditionRules.IsNullOrEmpty() ? null :
                    tac.ConditionRules.ToString(a => a.ToString() + ": " + ToString(a.Allowed), "\n") + "\n");
            },
            Order = 10,
        }).ToArray();
    }

    static string GetName(List<TypeAllowedBasic> list)
    {
        return "auth-" + list.ToString("-");
    }

    static List<TypeAllowedBasic> ToStringList(TypeAllowedAndConditions tac, bool userInterface)
    {
        List<TypeAllowedBasic> result = new List<TypeAllowedBasic>();
        result.Add(tac.Fallback.Get(userInterface));

        foreach (var c in tac.ConditionRules)
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
