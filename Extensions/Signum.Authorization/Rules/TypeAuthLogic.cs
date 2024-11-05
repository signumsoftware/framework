using Signum.Authorization.Rules;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;

namespace Signum.Authorization;

public static partial class TypeAuthLogic
{
    static TypeCache cache = null!;

    public static IManualAuth<Type, WithConditions<TypeAllowed>> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
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

            sb.Schema.Synchronizing += Schema_Synchronizing;

            sb.Schema.EntityEventsGlobal.PreUnsafeDelete += query =>
            {
                return OnIsWriting(query.ElementType);
            };

            cache = new TypeCache(sb);

            sb.Include<RuleTypeEntity>()
                .WithUniqueIndex(rt => new { rt.Resource, rt.Role })
                .WithVirtualMList(rt => rt.ConditionRules, c => c.RuleType);

            sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RuleTypeEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
            sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand?>(AuthCache_PreDeleteSqlSync_Type);
            sb.Schema.Table<TypeConditionSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand?>(AuthCache_PreDeleteSqlSync_Condition);
            Validator.PropertyValidator((RuleTypeEntity r) => r.ConditionRules).StaticPropertyValidation += TypeAuthCache_StaticPropertyValidation;

            AuthLogic.ExportToXml += cache.ExportXml;
            AuthLogic.ImportFromXml += cache.ImportXml;

            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
            TypeConditionLogic.Register(UserTypeCondition.DeactivatedUsers, (UserEntity u) => u.State == UserState.Deactivated);
        }
    }

    static SqlPreCommand? AuthCache_PreDeleteSqlSync_Type(Entity arg)
    {
        TypeEntity type = (TypeEntity)arg;

        var ruleTypeConditions = Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeConditionEntity>().Where(a => a.RuleType.Entity.Resource.Is(type)));
        var ruleTypesCommand = Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeEntity>().Where(a => a.Resource.Is(type)));

        return SqlPreCommand.Combine(Spacing.Simple, ruleTypeConditions, ruleTypesCommand);
    }

    static SqlPreCommand? AuthCache_PreDeleteSqlSync_Condition(Entity arg)
    {
        TypeConditionSymbol condition = (TypeConditionSymbol)arg;

        if (!Database.MListQuery((RuleTypeConditionEntity rt) => rt.Conditions).Any(mle => mle.Element.Is(condition)))
            return null;

        var mlist = Administrator.UnsafeDeletePreCommandMList((RuleTypeConditionEntity rt) => rt.Conditions, Database.MListQuery((RuleTypeConditionEntity rt) => rt.Conditions).Where(mle => mle.Element.Is(condition)));
        var emptyRules = Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeConditionEntity>().Where(rt => rt.Conditions.Count == 0), force: true, avoidMList: true);

        return SqlPreCommand.Combine(Spacing.Simple, mlist, emptyRules);
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
    static void RegisterSchemaEvent<T>(Schema sender)
         where T : Entity
    {
        sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(TypeAuthLogic_FilterQuery<T>);
    }

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
            if (requested <= min)
                return;

            if (max < requested)
                throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(requested.NiceToString(), ident.GetType().NiceName(), ident.IdOrNull));

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

    public static TypeRulePack GetTypeRules(Lite<RoleEntity> roleLite)
    {
        var result = new TypeRulePack { Role = roleLite };
        Schema s = Schema.Current;
        cache.GetRules(result, TypeLogic.TypeToEntity.Where(t => !t.Key.IsEnumEntity() && s.IsAllowed(t.Key, false) == null).Select(a => a.Value));

        foreach (TypeAllowedRule r in result.Rules)
        {
            Type type = TypeLogic.EntityToType[r.Resource];

            if (OperationAuthLogic.IsStarted)
                r.Operations = OperationAuthLogic.GetAllowedThumbnail(roleLite, type);

            if (PropertyAuthLogic.IsStarted)
                r.Properties = PropertyAuthLogic.GetAllowedThumbnail(roleLite, type);

            if (QueryAuthLogic.IsStarted)
                r.Queries = QueryAuthLogic.GetAllowedThumbnail(roleLite, type);
        }

        return result;

    }

    public static void SetTypeRules(TypeRulePack rules)
    {
        cache.SetRules(rules, t => true);
    }

    public static WithConditions<TypeAllowed> GetAllowed(Type type)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return new WithConditions<TypeAllowed>(TypeAllowed.Write);

        if (!TypeLogic.TypeToEntity.ContainsKey(type))
            return new WithConditions<TypeAllowed>(TypeAllowed.Write);

        if (EnumEntity.Extract(type) != null)
            return new WithConditions<TypeAllowed>(TypeAllowed.Read);

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
