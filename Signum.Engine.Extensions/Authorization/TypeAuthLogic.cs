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
using Signum.Engine.Extensions.Properties;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Engine.Exceptions;
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
                AuthLogic.AssertStarted(sb);
                TypeConditionLogic.Start(sb);

                sb.Schema.EntityEventsGlobal.Saving += Schema_Saving; //because we need Modifications propagated
                sb.Schema.EntityEventsGlobal.Retrieved += EntityEventsGlobal_Retrieved;
                sb.Schema.IsAllowedCallback += Schema_IsAllowedCallback;

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += () =>
                {
                    foreach (var type in TypeConditionLogic.Types)
                    {
                        miRegister.GetInvoker(type)(Schema.Current);
                    }
                };

                cache = new TypeAuthCache(sb,
                    AuthUtils.MaxType,
                    AuthUtils.MinType);

                AuthLogic.ExportToXml += () => cache.ExportXml();
                AuthLogic.ImportFromXml += (x, roles, replacements) => cache.ImportXml(x, roles, replacements);
                AuthLogic.SuggestRuleChanges += SuggestTypeRules;
            }
        }

        static Action<Lite<RoleDN>> SuggestTypeRules()
        {
            var graph = Schema.Current.ToDirectedGraph();
            graph.RemoveAll(graph.FeedbackEdgeSet().Edges);
            var compilationOrder = graph.CompilationOrder().ToList();
            var entityTypes = graph.ToDictionary(t => t.Type, t => TypeLogic.GetEntityType(t.Type));

            return role =>
            {
                var result = (from parent in compilationOrder
                              let parentAllowed = GetAllowed(role, parent.Type)
                              where parentAllowed.MaxCombined() > TypeAllowed.None
                              from kvp in graph.RelatedTo(parent)
                              where !kvp.Value.IsLite && !kvp.Value.IsNullable && !kvp.Value.IsCollection && !kvp.Key.Type.IsEnumEntity()
                              let relAllowed = GetAllowed(role, kvp.Key.Type)
                              where relAllowed.MaxCombined() == TypeAllowed.None
                              select new
                              {
                                  parent,
                                  parentAllowed,
                                  related = kvp.Key,
                                  relAllowed
                              }).ToList();

                foreach (var tuple in result)
	            {
                    SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Type: {0} is [{1}] but the related entity {2} is just [{3}]".Formato(
                        tuple.parent.Type.Name,
                        tuple.parentAllowed,
                        tuple.related.Type.Name,
                        tuple.relAllowed                       
                        ));

                    if (tuple.relAllowed.Conditions.IsNullOrEmpty() && tuple.relAllowed.Conditions.IsNullOrEmpty())
                    {
                        var suggested = new TypeAllowedAndConditions(TypeAllowed.DBReadUINone);

                        SafeConsole.WriteColor(ConsoleColor.DarkGreen, "Grant ");
                        if (SafeConsole.Ask("{0} for {1} to {2}?".Formato(suggested, tuple.related.Type.Name, role)))
                        {
                            Manual.SetAllowed(role, tuple.related.Type, suggested);
                            SafeConsole.WriteLineColor(ConsoleColor.Green, "Granted");
                        }
                        else
                        {   
                            SafeConsole.WriteLineColor(ConsoleColor.White, "Skipped");
                        }
                    }
	            }
            };
        }


        static GenericInvoker<Action<Schema>> miRegister =
            new GenericInvoker<Action<Schema>>(s => RegisterSchemaEvent<TypeDN>(s));
        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(TypeAuthLogic_FilterQuery);
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => TypeAuthLogic.Start(null)));
        }

        static string Schema_IsAllowedCallback(Type type)
        {
            var allowed = GetAllowed(type);

            if (allowed.MaxDB() == TypeAllowedBasic.None)
                return "Type '{0}' is set to None".Formato(type.NiceName());

            return null;
        }

        static void Schema_Saving(IdentifiableEntity ident)
        {
            if (ident.Modified.Value && !saveDisabled.Value)
            {
                TypeAllowedAndConditions access = GetAllowed(ident.GetType());

                var requested = ident.IsNew ? TypeAllowedBasic.Create : TypeAllowedBasic.Modify;

                var min = access.MinDB();
                var max = access.MaxDB();
                if (requested <= min)
                    return;

                if (max < requested)
                    throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(requested.NiceToString(), ident.GetType().NiceName(), ident.IdOrNull));

                Schema_Saving_Instance(ident);
            }
        }


        static void EntityEventsGlobal_Retrieved(IdentifiableEntity ident)
        {
            Type type = ident.GetType();
            TypeAllowedBasic access = GetAllowed(type).MaxDB();
            if (access < TypeAllowedBasic.Read)
                throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieve0.Formato(type.NicePluralName()));
        }

        public static TypeRulePack GetTypeRules(Lite<RoleDN> roleLite)
        {
            var result = new TypeRulePack { Role = roleLite };

            cache.GetRules(result, TypeLogic.TypeToDN.Where(t => !t.Key.IsEnumEntity()).Select(a => a.Value));

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
                return AuthUtils.MaxType.BaseAllowed;

            if (!TypeLogic.TypeToDN.ContainsKey(type))
                return AuthUtils.MaxType.BaseAllowed;

            if (EnumEntity.Extract(type) != null)
                return new TypeAllowedAndConditions(TypeAllowed.Read);

            TypeAllowed? temp = TypeAuthLogic.GetTemporallyAllowed(type);
            if (temp.HasValue)
                return new TypeAllowedAndConditions(temp.Value);

            return cache.GetAllowed(RoleDN.Current.ToLite(), type);
        }

        public static TypeAllowedAndConditions GetAllowed(Lite<RoleDN> role, Type type)
        {
            return cache.GetAllowed(role, type);
        }

        public static DefaultDictionary<Type, TypeAllowedAndConditions> AuthorizedTypes()
        {
            return cache.GetDefaultDictionary();
        }

        static readonly Variable<ImmutableStack<Tuple<Type, TypeAllowed>>> tempAllowed = Statics.ThreadVariable<ImmutableStack<Tuple<Type, TypeAllowed>>>("temporallyAllowed");

        public static IDisposable AllowTemporally<T>(TypeAllowed typeAllowed)
            where T : IdentifiableEntity
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
