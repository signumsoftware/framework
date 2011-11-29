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
                AuthLogic.ImportFromXml += (x, roles) => cache.ImportXml(x,  roles);

                sb.Schema.Table<RuleTypeDN>().PreDeleteSqlSync += AuthCache_PreDeleteSqlSync;

                Signum.Entities.Audit.Register(Audit.CyclesInRoles);
                Signum.Entities.Audit.Register(Audit.UnsafeRoles);
                Signum.Entities.Audit.Register(Audit.QueriesAndTypes);
            }
        }

      
        static GenericInvoker<Action<Schema>> miRegister = 
            new GenericInvoker<Action<Schema>>(s => RegisterSchemaEvent<TypeDN>(s));
        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(EntityGroupAuthLogic_FilterQuery);
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            var t = Schema.Current.Table<RuleTypeDN>();
            var rec = (FieldReference)t.Fields["resource"].Field;
            var cond = (FieldMList)t.Fields["conditions"].Field;
            var param = SqlParameterBuilder.CreateReferenceParameter("id", false, arg.Id);

            var conditions = new SqlPreCommandSimple("DELETE cond FROM {0} cond INNER JOIN {1} r ON cond.{2} = r.{3} WHERE r.{4} = {5}".Formato(
                cond.RelationalTable.Name.SqlScape(), t.Name.SqlScape(), cond.RelationalTable.BackReference.Name.SqlScape(), "Id", rec.Name.SqlScape(), param.ParameterName.SqlScape()), new List<SqlParameter> { param });
            var rule = new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name.SqlScape(), rec.Name.SqlScape(), param.ParameterName.SqlScape()), new List<SqlParameter> { param });

            return SqlPreCommand.Combine(Spacing.Simple, conditions, rule); 
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => TypeAuthLogic.Start(null)));
        }

        static string Schema_IsAllowedCallback(Type type)
        {
            var allowed = GetAllowed(type);

            if (allowed.Max().GetDB() == TypeAllowedBasic.None)
                return "Type '{0}' is set to None".Formato(type.NiceName());

            return null;
        }

   

        static void Schema_Saving(IdentifiableEntity ident)
        {
            if (ident.Modified.Value)
            {
                TypeAllowedAndConditions access = GetAllowed(ident.GetType());

                var requested = ident.IsNew ? TypeAllowedBasic.Create : TypeAllowedBasic.Modify;

                var min = access.Min().GetDB();
                var max = access.Max().GetDB();
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
            TypeAllowedBasic access = GetAllowed(type).Max().GetDB();
            if (access < TypeAllowedBasic.Read)
                throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieve0.Formato(type.NicePluralName()));
        }

        public static TypeRulePack GetTypeRules(Lite<RoleDN> roleLite)
        {
            var result = new TypeRulePack { Role = roleLite };

            cache.GetRules(result, TypeLogic.TypeToDN.Where(t => !t.Key.IsEnumProxy()).Select(a => a.Value));

            foreach (TypeAllowedRule r in result.Rules)
	        {
               Type type = TypeLogic.DnToType[r.Resource]; 

                if(OperationAuthLogic.IsStarted)
                    r.Operations = OperationAuthLogic.GetAllowedThumbnail(roleLite, type); 
                
                if(PropertyAuthLogic.IsStarted)
                    r.Properties = PropertyAuthLogic.GetAllowedThumbnail(roleLite, type); 
                
                if(QueryAuthLogic.IsStarted)
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
            if (!AuthLogic.IsEnabled || Schema.Current.InGlobalMode)
                return AuthUtils.MaxType.BaseAllowed;

            if (!TypeLogic.TypeToDN.ContainsKey(type))
                return AuthUtils.MaxType.BaseAllowed;

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

        [ThreadStatic]
        static ImmutableStack<Tuple<Type, TypeAllowed>> temporallyAllowed;

        public static IDisposable AllowTemporally<T>(TypeAllowed typeAllowed)
            where T : IdentifiableEntity
        {
            var old = temporallyAllowed;

            temporallyAllowed = (temporallyAllowed ?? ImmutableStack<Tuple<Type, TypeAllowed>>.Empty).Push(Tuple.Create(typeof(T), typeAllowed));

            return new Disposable(() => temporallyAllowed = old);
        }

        internal static TypeAllowed? GetTemporallyAllowed(Type type)
        {
            var ta = temporallyAllowed;
            if (ta == null || ta.IsEmpty)
                return null;

            var pair = temporallyAllowed.FirstOrDefault(a => a.Item1 == type);

            if (pair == null)
                return null;

            return pair.Item2;
        }

        public static class Audit
        {
            public static List<string> CyclesInRoles()
            {
                return AuthLogic.RolesGraph().FeedbackEdgeSet().Edges.Select(e => "{0} -> {1} produces a cycle".Formato(e.From, e.To)).ToList(); 
            }

            public static List<string> UnsafeRoles()
            {
                var g = AuthLogic.RolesGraph();

                var minUsers = g.Where(r2 => cache.GetDefaultRule(r2) == DefaultRule.Min && g.RelatedTo(r2).IsEmpty()).ToList();

                return g.Where(r => !g.IndirectlyRelatedTo(r).Any(r2 => minUsers.Contains(r2)))
                 .Select(r => "{0} does not inherit from {1}".Formato(r, minUsers.CommaAnd())).ToList();
            }

            public static List<string> QueriesAndTypes()
            {
                return (from r in AuthLogic.RolesGraph()
                        from t in Schema.Current.Tables.Keys.Where(a=>!a.IsEnumProxy())
                        let ua = GetAllowed(r,t).Min().GetUI()
                        let qns = (from qn in DynamicQueryManager.Current.GetQueries(t)
                                   where  QueryAuthLogic.GetQueryAllowed(r, qn) != (ua == TypeAllowedBasic.None)
                                   select qn).ToList()
                        where qns.Any()
                        select "Role {0}, type '{1}' is {2} but the queries {3} are {4}".Formato(
                          r, t.Name,  ua,  qns.CommaAnd(qn=>QueryUtils.GetNiceName(qn)), 
                          ua == TypeAllowedBasic.None ? "allowed": "not allowed")).ToList();

            }

            public static List<string> RecursiveAllowed()
            {      
                var graph =  Schema.Current.ToDirectedGraph();

                return (from r in AuthLogic.RolesGraph()
                        from t in graph
                        where !t.Type.IsEnumProxy() && TypeAuthLogic.GetAllowed(r, t.Type).Fallback.GetDB() > TypeAllowedBasic.None
                        from t2 in graph.IndirectlyRelatedTo(t, kvp => kvp.Value)
                        where !t2.Type.IsEnumProxy() && TypeAuthLogic.GetAllowed(r, t2.Type).Fallback.GetDB() == TypeAllowedBasic.None
                        select "Role {0} can retrieve '{1}' but not '{2}'".Formato(r, t.Type.Name, t2.Type.Name)).ToList();
            }
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
