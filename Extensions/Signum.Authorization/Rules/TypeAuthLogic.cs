using Microsoft.AspNetCore.Routing;
using Signum.Authorization.Rules;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Signum.Authorization;

public static partial class TypeAuthLogic
{
    static TypeCache cache = null!;

    public static IManualAuth<Type, WithConditions<TypeAllowed>> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        TypeLogic.AssertStarted(sb);
        AuthLogic.AssertStarted(sb);
        TypeConditionLogic.Start(sb);

        sb.Schema.EntityEventsGlobal.Saving += Schema_Saving; //because we need Modifications propagated
        sb.Schema.EntityEventsGlobal.Retrieved += EntityEventsGlobal_Retrieved;
        sb.Schema.IsAllowedCallback += Schema_IsAllowedCallback;

        sb.Schema.SchemaCompleted += () =>
        {
            foreach (var type in TypeConditionLogic.Types)
            {
                miRegister.GetInvoker(type)(Schema.Current);
            }
        };


        sb.Schema.EntityEventsGlobal.PreUnsafeDelete += query =>
        {
            return OnIsWriting(query.ElementType);
        };

        sb.Include<RuleTypeEntity>()
            .WithUniqueIndex(rt => new { rt.Resource, rt.Role })
            .WithVirtualMList(rt => rt.ConditionRules, c => c.RuleType);

        cache = new TypeCache(sb);

        sb.Schema.Synchronizing += rep => TypeConditionRuleSync.NotDefinedTypeCondition<RuleTypeConditionEntity>(rep, rt => rt.Conditions, rtc => rtc.RuleType.Entity.Resource, rtc => rtc.RuleType.Entity.Role);
        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RuleTypeEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
        sb.Schema.EntityEvents<RoleEntity>().PreDeleteSqlSync += role => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RuleTypeEntity>().Where(a => a.Role.Is(role)));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += t => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RuleTypeEntity>().Where(a => a.Resource.Is(t)));
        sb.Schema.EntityEvents<TypeConditionSymbol>().PreDeleteSqlSync += condition => TypeConditionRuleSync.DeletedTypeCondition<RuleTypeConditionEntity>(rt => rt.Conditions, mle => mle.Element.Is(condition));

        Validator.PropertyValidator((RuleTypeEntity r) => r.ConditionRules).StaticPropertyValidation += TypeAuthCache_StaticPropertyValidation;




        AuthLogic.ExportToXml += cache.ExportXml;
        AuthLogic.ImportFromXml += cache.ImportXml;

        AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
        TypeConditionLogic.RegisterCompile(UserTypeCondition.DeactivatedUsers, (UserEntity u) => u.State == UserState.Deactivated);
    }

    static string? TypeAuthCache_StaticPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        RuleTypeEntity rt = (RuleTypeEntity)sender;
        if (rt.Resource == null)
        {
            if (rt.ConditionRules.Any())
                return "Default {0} should not have conditions".FormatWith(typeof(RuleTypeEntity).NiceName());

            return null;
        }

        try
        {
            Type type = TypeLogic.EntityToType.GetOrThrow(rt.Resource);
            var conditions = rt.ConditionRules.Where(a =>
                a.Conditions.Any(c => c.FieldInfo != null && /*Not 100% Sync*/
                !TypeConditionLogic.IsDefined(type, c)));

            if (conditions.IsEmpty())
                return null;

            return "Type {0} has no definitions for the conditions: {1}".FormatWith(type.Name, conditions.CommaAnd(a => a.Conditions.CommaAnd(c => c.Key)));
        }
        catch (Exception ex) when (StartParameters.IgnoredDatabaseMismatches != null)
        {
            //This try { throw } catch is here to alert developers.
            //In production, in some cases its OK to attempt starting an application with a slightly different schema (dynamic entities, green-blue deployments).
            //In development, consider synchronize.
            StartParameters.IgnoredDatabaseMismatches.Add(ex);
            return null;
        }
    }

    static GenericInvoker<Action<Schema>> miRegister =
        new(s => RegisterSchemaEvent<TypeEntity>(s));
    static void RegisterSchemaEvent<T>(Schema schema)
         where T : Entity
    {
        schema.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(TypeAuthLogic_FilterQuery<T>);

        schema.EntityEvents<T>().RegisterBinding(a => a._TypeConditions,
            () => TypeConditionsForBinding(typeof(T)) != null,
            () =>
            {
                var conditions = TypeConditionsForBinding(typeof(T))!;
                if (conditions == null || conditions.IsEmpty())
                    return (e, rowId) => (IDictionary?)null;

                return GetTypeConditionsDictionary<T>(conditions);
            },
            (e, rowId, ret) => null);
    }

    private static Expression<Func<T, PrimaryKey?, IDictionary?>> GetTypeConditionsDictionary<T>(List<TypeConditionSymbol> conditions) where T : Entity
    {
        var entity = Expression.Parameter(typeof(T));
        var rowId = Expression.Parameter(typeof(PrimaryKey?));
        var type = typeof(Dictionary<TypeConditionSymbol, bool>);
        var miAdd = type.GetMethod("Add")!;
        var newDic = Expression.ListInit(Expression.New(type),
            conditions.Select(c => Expression.ElementInit(miAdd, Expression.Constant(c),
                Expression.Invoke(TypeConditionLogic.GetCondition(typeof(T), c, null), entity))));

        return Expression.Lambda<Func<T, PrimaryKey?, IDictionary?>>(newDic, [entity, rowId]);
    }

    static List<TypeConditionSymbol>? TypeConditionsForBinding(Type type)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return null;

        var tac = GetAllowed(type);

        var role = RoleEntity.Current;

        if (TrivialTypeGetUI(tac).HasValue && !HasTypeConditionInProperties(role, type) && !HasTypeConditionInOperations(role, type))
            return null;

        return tac.ConditionRules.SelectMany(a => a.TypeConditions).Distinct()
            .Where(cond => !TypeConditionLogic.HasInMemoryCondition(type, cond))
            .ToList();
    }


    public static TypeAllowedBasic? TrivialTypeGetUI(WithConditions<TypeAllowed> tac)
    {
        var conditions = tac.ConditionRules.Where(a => a.Allowed != TypeAllowed.None).Select(a => a.Allowed.GetUI()).ToHashSet();

        if (tac.Fallback != TypeAllowed.None)
            conditions.Add(tac.Fallback.GetUI());

        if (conditions.Count > 1)
            return null;

        if (conditions.Count == 0)
            return TypeAllowedBasic.None;
        else if (conditions.Count == 1)
            return conditions.SingleEx();
        else
            return null;
    }

    public static Func<Lite<RoleEntity>, Type, bool> HasTypeConditionInProperties = (role, t) => false;

    public static Func<Lite<RoleEntity>, Type, bool> HasTypeConditionInOperations = (role, t) => false;


    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => TypeAuthLogic.Start(null!)));
    }

    static string? Schema_IsAllowedCallback(Type type, bool inUserInterface)
    {
        var allowed = GetAllowed(type);

        if (allowed.Max(inUserInterface) == TypeAllowedBasic.None)
            return "Type '{0}' is set to None".FormatWith(type.NiceName());

        return null;
    }

    static void Schema_Saving(Entity ident)
    {
        if (ident.IsGraphModified && !inSave.Value)
        {
            WithConditions<TypeAllowed> access = GetAllowed(ident.GetType());

            var requested = TypeAllowedBasic.Write;

            var min = access.MinDB();
            var max = access.MaxDB();
            if (requested <= min && TypeConditionsForBinding(ident.GetType()).IsNullOrEmpty())
                return;

            if (max < requested)
            {
                if (ident.IsNew)
                    throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo01.NiceToString().FormatWith(requested.NiceToString(), ident.GetType().NiceName()));
                else
                    throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(requested.NiceToString(), ident.GetType().NiceName(), ident.IdOrNull));
            }

            Schema_Saving_Instance(ident);
        }
    }


    static void EntityEventsGlobal_Retrieved(Entity ident, PostRetrievingContext ctx)
    {
        Type type = ident.GetType();
        TypeAllowedBasic access = GetAllowed(type).MaxDB();
        if (access < TypeAllowedBasic.Read)
            throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedToRetrieve0.NiceToString().FormatWith(type.NicePluralName()));
    }

    public static TypeRulePack GetTypeRules(Lite<RoleEntity> role)
    {
        using (HeavyProfiler.LogNoStackTrace("GetTypeRules"))
        {
            var result = new TypeRulePack { Role = role };
            Schema s = Schema.Current;
            var types = TypeLogic.TypeToEntity.Where(t => !t.Key.IsEnumEntity() && s.IsAllowed(t.Key, false) == null).Select(a => a.Value);
            cache.GetRules(result, types);

            return result;
        }
    }

    public static Dictionary<Type, WithConditions<TypeAllowed>> GetTypeRulesSimple(Lite<RoleEntity> role)
    {
        using (HeavyProfiler.LogNoStackTrace("GetTypeRulesSimple"))
        {
            Schema s = Schema.Current;
            return TypeLogic.TypeToEntity
                .Where(t => !t.Key.IsEnumEntity() && s.IsAllowed(t.Key, false) == null)
                .ToDictionary(kvp => kvp.Key, kvp => GetAllowed(role, kvp.Key));
        }
    }

    public static void SetTypeRules(TypeRulePack rules)
    {
        cache.SetRules(rules, t => true);
    }

    public static WithConditions<TypeAllowed> GetAllowed(Type type)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return WithConditions<TypeAllowed>.Simple(TypeAllowed.Write);

        if (!TypeLogic.TypeToEntity.ContainsKey(type))
            return WithConditions<TypeAllowed>.Simple(TypeAllowed.Write);

        if (type.IsEnumEntity())
            return WithConditions<TypeAllowed>.Simple(TypeAllowed.Read);

        var allowed = cache.GetAllowed(RoleEntity.Current, type);

        var overrideTypeAllowed = TypeAuthLogic.GetOverrideTypeAllowed(type);
        if (overrideTypeAllowed != null)
            return overrideTypeAllowed(allowed);

        return allowed;
    }

    public static WithConditions<TypeAllowed> GetAllowed(Lite<RoleEntity> role, Type type)
    {
        return cache.GetAllowed(role, type);
    }

    public static WithConditions<TypeAllowed> GetAllowedBase(Lite<RoleEntity> role, Type type)
    {
        return cache.GetAllowedBase(role, type);
    }

    public static DefaultDictionary<Type, WithConditions<TypeAllowed>> AuthorizedTypes()
    {
        return cache.GetDefaultDictionary();
    }

    static readonly Variable<ImmutableStack<(Type type, Func<WithConditions<TypeAllowed>, WithConditions<TypeAllowed>> typeAllowedOverride)>> tempAllowed =
        Statics.ThreadVariable<ImmutableStack<(Type type, Func<WithConditions<TypeAllowed>, WithConditions<TypeAllowed>> typeAllowedOverride)>>("temporallyAllowed");

    public static IDisposable OverrideTypeAllowed<T>(Func<WithConditions<TypeAllowed>, WithConditions<TypeAllowed>> typeAllowedOverride)
        where T : Entity
    {
        var old = tempAllowed.Value;

        tempAllowed.Value = (old ?? ImmutableStack<(Type type, Func<WithConditions<TypeAllowed>, WithConditions<TypeAllowed>> typeAllowedOverride)>.Empty).Push((typeof(T), typeAllowedOverride));

        return new Disposable(() => tempAllowed.Value = tempAllowed.Value.Pop());
    }

    internal static Func<WithConditions<TypeAllowed>, WithConditions<TypeAllowed>>? GetOverrideTypeAllowed(Type type)
    {
        var ta = tempAllowed.Value;
        if (ta == null || ta.IsEmpty)
            return null;

        var pair = ta.FirstOrDefault(a => a.type == type);

        if (pair.type == null)
            return null;

        return pair.typeAllowedOverride;
    }
}


public static class AuthThumbnailExtensions
{
    public static AuthThumbnail? Collapse(this IEnumerable<bool> values)
    {
        bool? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return acum.Value ? AuthThumbnail.All : AuthThumbnail.None;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<QueryAllowed> values)
    {
        QueryAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
           acum.Value == QueryAllowed.None ? AuthThumbnail.None :
           acum.Value == QueryAllowed.EmbeddedOnly ? AuthThumbnail.Mix : AuthThumbnail.All;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<OperationAllowed> values)
    {
        OperationAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
           acum.Value == OperationAllowed.None ? AuthThumbnail.None :
           acum.Value == OperationAllowed.DBOnly ? AuthThumbnail.Mix : AuthThumbnail.All;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<PropertyAllowed> values)
    {
        PropertyAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item || acum.Value == PropertyAllowed.Read)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
            acum.Value == PropertyAllowed.None ? AuthThumbnail.None :
            acum.Value == PropertyAllowed.Read ? AuthThumbnail.Mix : AuthThumbnail.All;

    }
}
