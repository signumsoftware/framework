using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Isolation;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using System.ServiceModel.Channels;
using System.ServiceModel;
using Signum.Engine.Scheduler;
using Signum.Entities.Scheduler;

namespace Signum.Engine.Isolation
{
    public enum IsolationStrategy
    {
        Isolated,
        Optional,
        None,
    }

    public static class IsolationLogic
    {
        public static ResetLazy<List<Lite<IsolationDN>>> Isolations;

        internal static Dictionary<Type, IsolationStrategy> strategies = new Dictionary<Type, IsolationStrategy>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<IsolationDN>();

                dqm.RegisterQuery(typeof(IsolationDN), () =>
                    from iso in Database.Query<IsolationDN>()
                    select new
                    {
                        Entity = iso,
                        iso.Id,
                        iso.Name
                    });

                new Graph<IsolationDN>.Execute(IsolationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                sb.Schema.EntityEventsGlobal.PreSaving += EntityEventsGlobal_PreSaving;
                sb.Schema.Initializing += AssertIsolationStrategies;
                OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

                Isolations = sb.GlobalLazy(() => Database.RetrieveAllLite<IsolationDN>(),
                    new InvalidateWith(typeof(IsolationDN)));

                ProcessLogic.ApplySession += ProcessLogic_ApplySession;
                SchedulerLogic.ApplySession += SchedulerLogic_ApplySession;

                Validator.OverridePropertyValidator((IsolationMixin m) => m.Isolation).StaticPropertyValidation += (mi, pi) =>
                {
                    if (strategies.GetOrThrow(mi.MainEntity.GetType()) == IsolationStrategy.Isolated && mi.Isolation == null)
                        return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                    return null;
                };
            }
        }


        static IDisposable ProcessLogic_ApplySession(ProcessDN process)
        {
            return IsolationDN.Override(process.Data.TryIsolation());
        }

        static IDisposable SchedulerLogic_ApplySession(ITaskDN task)
        {
            return IsolationDN.Override(task.TryIsolation());
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, OperationLogDN log, IdentifiableEntity entity, object[] args)
        {
            return IsolationDN.Override(entity.Try(e => e.TryIsolation()) ?? args.TryGetArgC<Lite<IsolationDN>>());
        }

        static void EntityEventsGlobal_PreSaving(IdentifiableEntity ident, ref bool graphModified)
        {
            if (strategies.TryGet(ident.GetType(), IsolationStrategy.None) != IsolationStrategy.None && IsolationDN.Current != null)
            {
                if (ident.Mixin<IsolationMixin>().Isolation == null)
                {
                    ident.Mixin<IsolationMixin>().Isolation = IsolationDN.Current;
                    graphModified = true;
                }
                else if (!ident.Mixin<IsolationMixin>().Isolation.Is(IsolationDN.Current))
                    throw new ApplicationException(IsolationMessage.Entity0HasIsolation1ButCurrentIsolationIs2.NiceToString(ident, ident.Mixin<IsolationMixin>().Isolation, IsolationDN.Current));
            }
        }

        static void AssertIsolationStrategies()
        {
            var result = EnumerableExtensions.JoinStrict(
                strategies.Keys,
                Schema.Current.Tables.Keys.Where(a => !a.IsEnumEntityOrSymbol() && !typeof(SemiSymbol).IsAssignableFrom(a)),
                a => a,
                a => a,
                (a, b) => 0);

            var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.XXX);".Formato(t.Name), "\r\n");

            var lacking = result.Missing.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\r\n".Formato(gr.Key) +
                gr.ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.XXX);".Formato(t.Name), "\r\n"), "\r\n\r\n");

            if (extra.HasText() || lacking.HasText())
                throw new InvalidOperationException("IsolationLogic's strategies are not synchronized with the Schema.\r\n" +
                        (extra.HasText() ? ("Remove something like:\r\n" + extra + "\r\n\r\n") : null) +
                        (lacking.HasText() ? ("Add something like:\r\n" + lacking + "\r\n\r\n") : null));

            foreach (var item in strategies.Where(kvp => kvp.Value == IsolationStrategy.Isolated || kvp.Value == IsolationStrategy.Optional).Select(a => a.Key))
            {
                giRegisterFilterQuery.GetInvoker(item)(); 
            }

            Schema.Current.EntityEvents<IsolationDN>().FilterQuery += () =>
            {
                if (IsolationDN.Current == null || ExecutionMode.InGlobal)
                    return null;

                return new FilterQueryResult<IsolationDN>(
                    a => a.ToLite().Is(IsolationDN.Current), 
                    a => a.ToLite().Is(IsolationDN.Current));
            };
        }

        public static IsolationStrategy GetStrategy(Type type)
        {
            return strategies[type];
        }

        static readonly GenericInvoker<Action> giRegisterFilterQuery = new GenericInvoker<Action>(() => Register_FilterQuery<IdentifiableEntity>());
        static void Register_FilterQuery<T>() where T : IdentifiableEntity
        {
            Schema.Current.EntityEvents<T>().FilterQuery += () =>
            {
                if (ExecutionMode.InGlobal || IsolationDN.Current == null)
                    return null;

                return new FilterQueryResult<T>(
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationDN.Current),
                    a => a.Mixin<IsolationMixin>().Isolation.Is(IsolationDN.Current));
            };

            Schema.Current.EntityEvents<T>().PreUnsafeInsert += (IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery) =>
            {
                if (constructor.Body.Type == typeof(T)) 
                {
                    var newBody = Expression.Call(
                      miSetMixin.MakeGenericMethod(typeof(T), typeof(IsolationMixin), typeof(Lite<IsolationDN>)),
                      constructor.Body,
                      Expression.Quote(isolationProperty),
                      Expression.Constant(IsolationDN.Current));

                    return Expression.Lambda(newBody, constructor.Parameters);
                }

                return constructor; //MListTable
            }; 
        }

        static MethodInfo miSetMixin = ReflectionTools.GetMethodInfo((IdentifiableEntity a) => a.SetMixin((IsolationMixin m) => m.Isolation, null)).GetGenericMethodDefinition();
        static Expression<Func<IsolationMixin, Lite<IsolationDN>>> isolationProperty = (IsolationMixin m) => m.Isolation;


        public static void Register<T>(IsolationStrategy strategy) where T : IdentifiableEntity
        {
            strategies.Add(typeof(T), strategy);

            if (strategy == IsolationStrategy.Isolated || strategy == IsolationStrategy.Optional)
                MixinDeclarations.Register(typeof(T), typeof(IsolationMixin));

            if (strategy == IsolationStrategy.Optional)
            {
                Schema.Current.Settings.OverrideAttributes((T e) => e.Mixin<IsolationMixin>().Isolation, new AttachToUniqueIndexesAttribute()); //Remove non-null 
            }
        }


        public static IDisposable IsolationFromOperationContext()
        {
            MessageHeaders headers = OperationContext.Current.IncomingMessageHeaders;

            int val = headers.FindHeader("CurrentIsolation", "http://www.signumsoftware.com/Isolation");

            if (val == -1)
                return null;

            return IsolationDN.Override(Lite.Parse<IsolationDN>(headers.GetHeader<string>(val)));
        }

        public static IEnumerable<T> WhereCurrentIsolationInMemory<T>(this IEnumerable<T> collection) where T : Entity
        {
            var curr = IsolationDN.Current;

            if (curr == null || strategies[typeof(T)] == IsolationStrategy.None)
                return collection;

            return collection.Where(a => a.Isolation().Is(curr));
        }

        public static Lite<IsolationDN> GetOnlyIsolation(List<Lite<IdentifiableEntity>> selectedEntities)
        {
            return selectedEntities.GroupBy(a => a.EntityType)
                .Select(gr => strategies[gr.Key] == IsolationStrategy.None ? null : giGetOnlyIsolation.GetInvoker(gr.Key)(gr))
                .NotNull()
                .Only();
        }


        static GenericInvoker<Func<IEnumerable<Lite<IdentifiableEntity>>, Lite<IsolationDN>>> giGetOnlyIsolation = 
            new GenericInvoker<Func<IEnumerable<Lite<IdentifiableEntity>>, Lite<IsolationDN>>>(list => GetOnlyIsolation<IdentifiableEntity>(list));
        public static Lite<IsolationDN> GetOnlyIsolation<T>(IEnumerable<Lite<IdentifiableEntity>> selectedEntities) where T : IdentifiableEntity
        {
            return selectedEntities.Cast<Lite<T>>().GroupsOf(100).Select(gr =>
                Database.Query<T>().Where(e => gr.Contains(e.ToLite())).Select(e => e.Isolation()).Only()
                ).NotNull().Only();
        }
        
    }
}
