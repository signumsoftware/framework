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

namespace Signum.Engine.Isolation
{
    public enum IsolationStrategy
    {
        Isolated,
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
                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += AssertIsolationStrategies;
                OperationLogic.SurroundOperation += OperationLogic_SurroundOperation;

                Isolations = sb.GlobalLazy(() => Database.RetrieveAllLite<IsolationDN>(),
                    new InvalidateWith(typeof(IsolationDN)));

                ProcessLogic.ApplySession += ProcessLogic_ApplySession;
            }
        }

        static IDisposable ProcessLogic_ApplySession(ProcessDN process)
        {
            return IsolationDN.OverrideIfNecessary(process.Data.TryIsolation());
        }

        static IDisposable OperationLogic_SurroundOperation(IOperation operation, IdentifiableEntity entity, object[] args)
        {
            return IsolationDN.OverrideIfNecessary(entity.Try(e => e.TryIsolation()) ?? args.TryGetArgC<Lite<IsolationDN>>());
        }

        static void EntityEventsGlobal_PreSaving(IdentifiableEntity ident, ref bool graphModified)
        {
            if (strategies.TryGet(ident.GetType(), IsolationStrategy.None) == IsolationStrategy.Isolated && IsolationDN.Current != null)
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
                Schema.Current.Tables.Keys.Where(a => !a.IsEnumEntity() && !typeof(Symbol).IsAssignableFrom(a) && !typeof(SemiSymbol).IsAssignableFrom(a)),
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

            foreach (var item in strategies.Where(kvp => kvp.Value == IsolationStrategy.Isolated).Select(a => a.Key))
            {
                giRegisterFilterQuery.GetInvoker(item)(); 
            }

            Schema.Current.EntityEvents<IsolationDN>().FilterQuery += query =>
            {
                if (IsolationDN.Current == null || ExecutionMode.InGlobal)
                    return query;

                Expression<Func<IsolationDN, bool>> filter = a => a.ToLite() == IsolationDN.Current;

                filter = (Expression<Func<IsolationDN, bool>>)DbQueryProvider.Clean(filter, false, null);

                return query.Where(filter);
            };
        }


        static readonly GenericInvoker<Action> giRegisterFilterQuery = new GenericInvoker<Action>(() => Register_FilterQuery<IdentifiableEntity>());
        static void Register_FilterQuery<T>() where T : IdentifiableEntity
        {
            Schema.Current.EntityEvents<T>().FilterQuery += query =>
            {
                if (IsolationDN.Current == null || ExecutionMode.InGlobal)
                    return query;

                Expression<Func<T, bool>> filter = a => a.Mixin<IsolationMixin>().Isolation == IsolationDN.Current;

                filter = (Expression<Func<T, bool>>)DbQueryProvider.Clean(filter, false, null);

                return query.Where(filter);
            };
        }

        public static void Register<T>(IsolationStrategy strategy) where T : IdentifiableEntity
        {
            strategies.Add(typeof(T), strategy);

            if (strategy == IsolationStrategy.Isolated)
                MixinDeclarations.Register(typeof(T), typeof(IsolationMixin));
        }


        public static IDisposable IsolationFromOperationContext()
        {
            MessageHeaders headers = OperationContext.Current.IncomingMessageHeaders;

            int val = headers.FindHeader("CurrentIsolation", "http://www.signumsoftware.com/Isolation");

            if (val == -1)
                return null;

            return IsolationDN.OverrideIfNecessary(Lite.Parse<IsolationDN>(headers.GetHeader<string>(val)));
        }
    }
}
