using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using System.Threading;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using System.Security.Authentication;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Data.SqlClient;

namespace Signum.Engine.Authorization
{
    public static partial class TypeAuthLogic
    {
        static TypeAuthCache cache;

        public static IManualAuth<Type, TypeAllowedAndConditions> Manual { get { return cache; } }

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

                cache = new TypeAuthCache(sb, merger: TypeAllowedMerger.Instance);

                AuthLogic.ExportToXml += exportAll => cache.ExportXml(exportAll ? TypeLogic.TypeToEntity.Keys.ToList() : null);
                AuthLogic.ImportFromXml += (x, roles, replacements) => cache.ImportXml(x, roles, replacements);
            }
        }

        static GenericInvoker<Action<Schema>> miRegister =
            new GenericInvoker<Action<Schema>>(s => RegisterSchemaEvent<TypeEntity>(s));
        static void RegisterSchemaEvent<T>(Schema sender)
             where T : Entity
        {
            sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(TypeAuthLogic_FilterQuery<T>);
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => TypeAuthLogic.Start(null)));
        }

        static string Schema_IsAllowedCallback(Type type, bool inUserInterface)
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
                TypeAllowedAndConditions access = GetAllowed(ident.GetType());

                var requested = ident.IsNew ? TypeAllowedBasic.Create : TypeAllowedBasic.Modify;

                var min = access.MinDB();
                var max = access.MaxDB();
                if (requested <= min)
                    return;

                if (max < requested)
                    throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(requested.NiceToString(), ident.GetType().NiceName(), ident.IdOrNull));

                Schema_Saving_Instance(ident);
            }
        }


        static void EntityEventsGlobal_Retrieved(Entity ident)
        {
            Type type = ident.GetType();
            TypeAllowedBasic access = GetAllowed(type).MaxDB();
            if (access < TypeAllowedBasic.Read)
                throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedToRetrieve0.NiceToString().FormatWith(type.NicePluralName()));
        }

        public static TypeRulePack GetTypeRules(Lite<RoleEntity> roleLite)
        {
            var result = new TypeRulePack { Role = roleLite };

            cache.GetRules(result, TypeLogic.TypeToEntity.Where(t => !t.Key.IsEnumEntity()).Select(a => a.Value));

            foreach (TypeAllowedRule r in result.Rules)
            {
                Type type = TypeLogic.DnToType[r.Resource];

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
            cache.SetRules(rules);
        }

        public static TypeAllowedAndConditions GetAllowed(Type type)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return new TypeAllowedAndConditions(TypeAllowed.Create);

            if (!TypeLogic.TypeToEntity.ContainsKey(type))
                return new TypeAllowedAndConditions(TypeAllowed.Create);

            if (EnumEntity.Extract(type) != null)
                return new TypeAllowedAndConditions(TypeAllowed.Read);

            TypeAllowed? temp = TypeAuthLogic.GetTemporallyAllowed(type);
            if (temp.HasValue)
                return new TypeAllowedAndConditions(temp.Value);

            return cache.GetAllowed(RoleEntity.Current.ToLite(), type);
        }

        public static TypeAllowedAndConditions GetAllowed(Lite<RoleEntity> role, Type type)
        {
            return cache.GetAllowed(role, type);
        }

        public static TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role, Type type)
        {
            return cache.GetAllowedBase(role, type);
        }

        public static DefaultDictionary<Type, TypeAllowedAndConditions> AuthorizedTypes()
        {
            return cache.GetDefaultDictionary();
        }

        static readonly Variable<ImmutableStack<Tuple<Type, TypeAllowed>>> tempAllowed = Statics.ThreadVariable<ImmutableStack<Tuple<Type, TypeAllowed>>>("temporallyAllowed");

        public static IDisposable AllowTemporally<T>(TypeAllowed typeAllowed)
            where T : Entity
        {
            tempAllowed.Value = (tempAllowed.Value ?? ImmutableStack<Tuple<Type, TypeAllowed>>.Empty).Push(Tuple.Create(typeof(T), typeAllowed));

            return new Disposable(() => tempAllowed.Value = tempAllowed.Value.Pop());
        }

        internal static TypeAllowed? GetTemporallyAllowed(Type type)
        {
            var ta = tempAllowed.Value;
            if (ta == null || ta.IsEmpty)
                return null;

            var pair = ta.FirstOrDefault(a => a.Item1 == type);

            if (pair == null)
                return null;

            return pair.Item2;
        }
    }

    class TypeAllowedMerger : IMerger<Type, TypeAllowedAndConditions>
    {
        public static readonly TypeAllowedMerger Instance = new TypeAllowedMerger();

        TypeAllowedMerger() { }

        public TypeAllowedAndConditions Merge(Type key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, TypeAllowedAndConditions>> baseValues)
        {
            if (AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union)
                return MergeBase(baseValues.Select(a => a.Value), MaxTypeAllowed, TypeAllowed.Create, TypeAllowed.None);
            else
                return MergeBase(baseValues.Select(a => a.Value), MinTypeAllowed, TypeAllowed.None, TypeAllowed.Create);
        }

        static TypeAllowed MinTypeAllowed(IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.Create;

            foreach (var item in collection)
            {
                if (item < result)
                    result = item;

                if (result == TypeAllowed.None)
                    return result;

            }
            return result;
        }

        static TypeAllowed MaxTypeAllowed(IEnumerable<TypeAllowed> collection)
        {
            TypeAllowed result = TypeAllowed.None;

            foreach (var item in collection)
            {
                if (item > result)
                    result = item;

                if (result == TypeAllowed.Create)
                    return result;

            }
            return result;
        }

        public Func<Type, TypeAllowedAndConditions> MergeDefault(Lite<RoleEntity> role)
        {
            var taac = new TypeAllowedAndConditions(AuthLogic.GetDefaultAllowed(role) ? TypeAllowed.Create : TypeAllowed.None);
            return new ConstantFunction<Type, TypeAllowedAndConditions>(taac).GetValue;
        }

        public static TypeAllowedAndConditions MergeBase(IEnumerable<TypeAllowedAndConditions> baseRules, Func<IEnumerable<TypeAllowed>, TypeAllowed> maxMerge, TypeAllowed max, TypeAllowed min)
        {
            TypeAllowedAndConditions only = baseRules.Only();
            if (only != null)
                return only;

            if (baseRules.Any(a => a.Fallback == null))
                return new TypeAllowedAndConditions(null);

            if (baseRules.Any(a => a.Exactly(max)))
                return new TypeAllowedAndConditions(max);

            TypeAllowedAndConditions onlyNotOposite = baseRules.Where(a => !a.Exactly(min)).Only();
            if (onlyNotOposite != null)
                return onlyNotOposite;

            var first = baseRules.FirstOrDefault(c => !c.Conditions.IsNullOrEmpty());

            if (first == null)
                return new TypeAllowedAndConditions(maxMerge(baseRules.Select(a => a.Fallback.Value)));

            var conditions = first.Conditions.Select(c => c.TypeCondition).ToList();

            if (baseRules.Where(c => !c.Conditions.IsNullOrEmpty() && c != first).Any(br => !br.Conditions.Select(c => c.TypeCondition).SequenceEqual(conditions)))
                return new TypeAllowedAndConditions(null);

            return new TypeAllowedAndConditions(maxMerge(baseRules.Select(a => a.Fallback.Value)),
                conditions.Select((c, i) => new TypeConditionRule(c, maxMerge(baseRules.Where(br => !br.Conditions.IsNullOrEmpty()).Select(br => br.Conditions[i].Allowed)))).ToArray());
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
}
