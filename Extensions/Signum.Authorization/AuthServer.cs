using Microsoft.AspNetCore.Mvc;
using Signum.Utilities.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Signum.Authorization.SessionLog;
using Signum.Authorization.Rules;
using Signum.Authorization.AuthToken;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Json;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Frozen;

namespace Signum.Authorization;

public static class AuthServer
{
    public static bool MergeInvalidUsernameAndPasswordMessages = false;

    public static Action<ActionContext, UserEntity> UserPreLogin;
    public static Action<ActionContext, UserEntity> UserLogged;
    public static Action<ActionContext, UserWithClaims> UserLoggingOut;

    
    public static void Start(Func<AuthTokenConfigurationEmbedded> tokenConfig, string hashableEncryptionKey)
    {
        AuthTokenServer.Start(tokenConfig, hashableEncryptionKey);

        ReflectionServer.GetContext = () => new
        {
            Culture = ReflectionServer.GetCurrentValidCulture(),
            Role = UserEntity.Current == null ? null : RoleEntity.Current,
        };

        CultureController.OnChangeCulture = culture =>
        {
            if (UserEntity.Current != null && !UserEntity.Current.Is(AuthLogic.AnonymousUser)) //Won't be used till next refresh
            {
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    var user = UserEntity.Current.RetrieveAndRemember();
                    user.CultureInfo = culture.RetrieveAndRemember();
                    UserHolder.Current = new UserWithClaims(user);
                    user.Save();
                }
            }
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

                    if(ta.Fallback == TypeAllowed.None)
                    {
                        var conditions = ta.ConditionRules.SelectMany(a => a.TypeConditions)
                        .Distinct().Where(a => TypeConditionLogic.IsQueryAuditor(t, a)).Select(a => a.Key);

                        if (conditions.Any())
                            ti.Extension.Add("queryAuditors", conditions.ToList());
                    }

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
                var args = FilterQueryArgs.FromEntity(ep.entity);

                var typeAllowed =
                UserEntity.Current == null ? TypeAllowedBasic.None :
                ep.entity.IsNew ? TypeAuthLogic.GetAllowed(ep.entity.GetType()).MaxUI() :
                TypeAuthLogic.IsAllowedFor(ep.entity, TypeAllowedBasic.Write, inUserInterface: true, args) ? TypeAllowedBasic.Write :
                TypeAuthLogic.IsAllowedFor(ep.entity, TypeAllowedBasic.Read, inUserInterface: true, args) ? TypeAllowedBasic.Read :
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

                    return giCountReadonly.GetInvoker(gr.Key)(gr) > 0;
                });
            };
        }

        if (QueryAuthLogic.IsStarted)
        {
            ReflectionServer.TypeExtension += (ti, t) =>
            {
                if (ti.QueryDefined)
                {
                    var allowed = UserEntity.Current == null ? QueryAllowed.None : 
                    QueryLogic.Queries.QueryAllowed(t, fullScreen: true) ? QueryAllowed.Allow :
                    QueryLogic.Queries.QueryAllowed(t, fullScreen: false) ? QueryAllowed.EmbeddedOnly : QueryAllowed.None;
                    
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
                    var q = fi.GetValue(null)!;

                    var allowed = UserEntity.Current == null ? QueryAllowed.None :
                    QueryLogic.Queries.QueryAllowed(q, fullScreen: true) ? QueryAllowed.Allow :
                    QueryLogic.Queries.QueryAllowed(q, fullScreen: false) ? QueryAllowed.EmbeddedOnly : QueryAllowed.None;

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

                    ((UserEntity)ctx.Entity).PasswordHash = PasswordEncoding.EncodePassword(((UserEntity)ctx.Entity).UserName, password);
                }
            }
        });

        if (SessionLogLogic.IsStarted)
            AuthServer.UserLogged +=  (ActionContext ac, UserEntity user) =>
            {
                Microsoft.AspNetCore.Http.HttpRequest re = ac.HttpContext.Request;
                SessionLogLogic.SessionStart(
                    re.Host.ToString(),
                    re.Headers["User-Agent"].FirstOrDefault());
            };


    }

    public static ResetLazy<FrozenDictionary<string, List<Type>>> entitiesByNamespace =
        new(() => Schema.Current.Tables.Keys.Where(t => !EnumEntity.IsEnumEntity(t)).GroupToDictionary(t => t.Namespace!).ToFrozenDictionary());

    public static ConcurrentDictionary<string, bool> NamespaceNoLoaded = new ConcurrentDictionary<string, bool>();

    public static bool IsNamespaceAllowed(Type type)
    {
        var func = ReflectionServer.OverrideIsNamespaceAllowed.TryGetC(type.Namespace!);

        if (func != null)
            return func();

        var typesInNamespace = entitiesByNamespace.Value.TryGetC(type.Namespace!);
        if (typesInNamespace != null)
            return typesInNamespace.Any(t => TypeAuthLogic.GetAllowed(t).MaxUI() > TypeAllowedBasic.None);


        var notLoaded = NamespaceNoLoaded.GetOrAdd(type.Namespace!, ns =>
        {
            var entities = type.Assembly.ExportedTypes.Where(a => a.Namespace == type.Namespace && typeof(Entity).IsAssignableFrom(a) && !a.IsAbstract);

            return entities.Any() && !entities.Any(e => Schema.Current.Tables.ContainsKey(e));
        });

        if (notLoaded)
            return false;

        throw new InvalidOperationException(@$"Unable to determine whether the metadata for '{type.FullName}' should be delivered to the client because there are no entities in the namespace '{type.Namespace!}'.
Consider calling ReflectionServer.RegisterLike(typeof({type.Name}), ()=> yourCondition);");
    }



    static GenericInvoker<Func<IEnumerable<Lite<Entity>>, int>> giCountReadonly = new(lites => CountReadonly<UserEntity>(lites));
    public static int CountReadonly<T>(IEnumerable<Lite<Entity>> lites) where T : Entity
    {
        var array = lites.Cast<Lite<T>>().ToArray();
        var args = FilterQueryArgs.FromFilter<T>(e => array.Contains(e.ToLite()));

        using (TypeAuthLogic.DisableQueryFilter())
            return Database.Query<T>().Where(a => array.Contains(a.ToLite())).Count(a => !a.IsAllowedFor(TypeAllowedBasic.Write, true, args));
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

   

}
