using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Alerts;
using System.Linq.Expressions;

namespace Signum.Engine.Alerts
{
    public static class AlertLogic
    {
        static Expression<Func<IdentifiableEntity, IQueryable<AlertDN>>> AlertasExpression =
            e => Database.Query<AlertDN>().Where(a => a.Target.RefersTo(e));
        public static IQueryable<AlertDN> Alertas(this IdentifiableEntity e)
        {
            return AlertasExpression.Evaluate(e);
        }

        public static HashSet<Enum> SystemAlertTypes = new HashSet<Enum>();
        static bool started = false;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AlertDN>();

                dqm[typeof(AlertDN)] =
                    (from a in Database.Query<AlertDN>()
                     select new
                            {
                                Entity = a,
                                a.Id,
                                a.AlertType,
                                Texto = a.Text.Etc(100),
                                CreacionDate = a.CreationDate,
                                a.CreatedBy,
                                a.AttendedDate,
                                a.AttendedBy,
                                a.Target
                            }).ToDynamic();

                dqm[typeof(AlertTypeDN)] = (from t in Database.Query<AlertTypeDN>()
                                            select new
                                            {
                                                Entity = t,
                                                t.Id,
                                                Nombre = t.Name,
                                                t.Key,
                                            }).ToDynamic();

                AlertGraph.Register();

                AlertTypeEnumLogic.Start(sb, () => SystemAlertTypes);

                new BasicExecute<AlertTypeDN>(AlertTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (a, _) => {  }
                }.Register();

                started = true;
            }
        }

        public static void RegisterAlertType(Enum alertType)
        {
            SystemAlertTypes.Add(alertType); 
        }

        public static AlertDN CreateAlert(this IIdentifiable entity, string text, Enum alertType, DateTime? alertDate = null, Lite<UserDN> user = null, string title = null)
        {
            return CreateAlert(entity.ToLite(), text, alertType, alertDate, user, title);
        }

        public static AlertDN CreateAlert<T>(this Lite<T> entity, string text, Enum alertType, DateTime? alertDate = null, Lite<UserDN> user = null, string title = null) where T : class, IIdentifiable
        {
            if (started == false)
                return null;

            return new AlertDN
            {
                AlertDate = alertDate ?? TimeZoneManager.Now,
                CreatedBy = user ?? UserDN.Current.ToLite(),
                Text = text,
                Title = title,
                Target = (Lite<IdentifiableEntity>)Lite.Create(entity.EntityType, entity.Id, entity.ToString()),
                AlertType = AlertTypeEnumLogic.ToEntity(alertType)
            }.Execute(AlertOperation.SaveNew);
        }

        public static AlertDN CreateAlertForceNew(this IIdentifiable entity, string text, Enum alertType, DateTime? alertDate = null, Lite<UserDN> user = null)
        {
            return CreateAlertForceNew(entity.ToLite(), text, alertType, alertDate, user);
        }

        public static AlertDN CreateAlertForceNew<T>(this Lite<T> entity, string text, Enum alertType, DateTime? alertDate = null, Lite<UserDN> user = null) where T : class, IIdentifiable
        {
            if (started == false)
                return null;

            using (Transaction tr = Transaction.ForceNew())
            {
                var alerta = entity.CreateAlert(text, alertType, alertDate, user);

                return tr.Commit(alerta);
            }
        }

        public static AlertTypeDN GetAlertType(Enum alertType)
        {
            return AlertTypeEnumLogic.ToEntity(alertType);
        }
    }

    public class AlertGraph : Graph<AlertDN, AlertState>
    {
        public static void Register()
        {
            GetState = a => a.State;

            new Execute(AlertOperation.SaveNew)
            {
                FromStates = new[] { AlertState.New },
                ToState = AlertState.Saved,
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Save)
            {
                FromStates = new[] { AlertState.Saved },
                ToState = AlertState.Saved,
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Attend)
            {
                FromStates = new[] { AlertState.Saved },
                ToState = AlertState.Attended,
                Lite = false,
                Execute = (a, _) =>
                {
                    a.State = AlertState.Attended;
                    a.AttendedDate = TimeZoneManager.Now;
                    a.AttendedBy = UserDN.Current.ToLite();
                }
            }.Register();

            new Execute(AlertOperation.Unattend)
            {
                FromStates = new[] { AlertState.Attended },
                ToState = AlertState.Saved,
                Execute = (a, _) =>
                {
                    a.State = AlertState.Saved;
                    a.AttendedDate = null;
                    a.AttendedBy = null;
                }
            }.Register();
        }
    }

    static class AlertTypeEnumLogic
    {
        public static HashSet<Enum> Keys { get; set; }
        static Dictionary<Enum, AlertTypeDN> toEntity;
        static Dictionary<string, Enum> toEnum;
        static Func<HashSet<Enum>> getKeys;

        public static void Start(SchemaBuilder sb, Func<HashSet<Enum>> getKeys)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AlertTypeDN>();

                AlertTypeEnumLogic.getKeys = getKeys;

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing()
        {
            using (new EntityCache(true))
            {
                Keys = getKeys();

                var joinResult = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<AlertTypeDN>().Where(a => a.Key.HasText()),
                     Keys,
                     a => a.Key,
                     k => MultiEnumDN.UniqueKey(k),
                     (a, k) => new { a, k });

                if (joinResult.Lacking.Count != 0)
                    throw new InvalidOperationException("Error loading {0}\r\n Lacking: {1}".Formato(typeof(AlertTypeDN).Name, joinResult.Lacking.ToString(", ")));

                toEntity = joinResult.Result.ToDictionary(p => p.k, p => p.a);

                toEnum = toEntity.Keys.ToDictionary(k => MultiEnumDN.UniqueKey(k));
            }
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<AlertTypeDN>();

            List<AlertTypeDN> should = GenerateEntities();

            return should.Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<AlertTypeDN>();

            List<AlertTypeDN> current = Administrator.TryRetrieveAll<AlertTypeDN>(replacements);
            List<AlertTypeDN> should = GenerateEntities();

            return Synchronizer.SynchronizeScriptReplacing(replacements, 
                typeof(AlertTypeDN).Name, 
                should.ToDictionary(s => s.Key), 
                current.Where(c => c.Key.HasText()).ToDictionary(c => c.Key), 
                (k, s) => table.InsertSqlSync(s), 
                (k, c) => table.DeleteSqlSync(c), 
                (k, s, c) =>
                {
                    if (c.Name != s.Name || c.Key != s.Key)
                    {
                        c.Key = null;
                        c.Name = s.Name;
                        c.Key = s.Key;
                    }
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }



        static List<AlertTypeDN> GenerateEntities()
        {
            return getKeys().Select(k => new AlertTypeDN
            {
                Name = k.NiceToString(),
                Key = MultiEnumDN.UniqueKey(k),
            }).ToList();
        }

        public static AlertTypeDN ToEntity(Enum key)
        {
            AssertInitialized();

            return toEntity.GetOrThrow(key);
        }

        private static void AssertInitialized()
        {
            if (Keys == null)
                throw new InvalidOperationException("{0} is not initialized. Consider calling Schema.InitializeUntil(InitLevel.Level0SyncEntities)".Formato(typeof(AlertTypeEnumLogic).TypeName()));
        }

        public static AlertTypeDN ToEntity(string keyName)
        {
            return ToEntity(ToEnum(keyName));
        }

        public static AlertTypeDN TryToEntity(Enum key)
        {
            AssertInitialized();

            return toEntity.TryGetC(key);
        }

        public static AlertTypeDN TryToEntity(string keyName)
        {
            Enum en = TryToEnum(keyName);

            if (en == null)
                return null;

            return TryToEntity(en);
        }

        public static Enum ToEnum(AlertTypeDN entity)
        {
            return ToEnum(entity.Key);
        }

        public static Enum ToEnum(string keyName)
        {
            AssertInitialized();

            return toEnum.GetOrThrow(keyName);
        }

        public static Enum TryToEnum(string keyName)
        {
            AssertInitialized();

            return toEnum.TryGetC(keyName);
        }

        internal static IEnumerable<AlertTypeDN> AllEntities()
        {
            AssertInitialized();

            return toEntity.Values;
        }
    }
}
