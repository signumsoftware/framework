using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Isolation;
using Signum.Utilities;

namespace Signum.Engine.Isolation
{
    public enum IsolationStrategy
    {
        Isolated,
        None,
    }

    public static class IsolationLogic
    {
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

                sb.Schema.EntityEventsGlobal.Saving += EntityEventsGlobal_Saving;

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += AssertIsolationStrategies;
            }
        }

        static void EntityEventsGlobal_Saving(IdentifiableEntity ident)
        {
            if (strategies.TryGet(ident.GetType(), IsolationStrategy.None) == IsolationStrategy.Isolated && IsolationDN.Current != null)
            {
                if (ident.Mixin<IsolationMixin>().Isolation == null)
                    ident.Mixin<IsolationMixin>().Isolation = IsolationDN.Current;
                else if (!ident.Mixin<IsolationMixin>().Isolation.Is(IsolationDN.Current))
                    throw new ApplicationException(IsolationMessage.Entity0HasIsolation1ButCurrentIsolationIs2.NiceToString(ident, ident.Mixin<IsolationMixin>().Isolation, IsolationDN.Current));
                    
            }
        }

        static void AssertIsolationStrategies()
        {
            var result = EnumerableExtensions.JoinStrict(
                strategies.Keys,
                Schema.Current.Tables.Keys.Where(a => !a.IsEnumEntity() && !typeof(Symbol).IsAssignableFrom(a) && !typeof(Symbol).IsAssignableFrom(a)),
                a => a,
                a => a,
                (a, b) => 0);

            var extra = result.Extra.OrderBy(a => a.Namespace).ThenBy(a => a.Name).ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.None);".Formato(t.Name), "\r\n");

            var lacking = result.Missing.GroupBy(a => a.Namespace).OrderBy(gr => gr.Key).ToString(gr => "  //{0}\r\n".Formato(gr.Key) +
                gr.ToString(t => "  IsolationLogic.Register<{0}>(IsolationStrategy.None);".Formato(t.Name), "\r\n"), "\r\n\r\n");

            if (extra.HasText() || lacking.HasText())
                throw new InvalidOperationException("IsolationLogic's strategies are not synchronized with the Schema.\r\n" +
                        (extra.HasText() ? ("Remove something like:\r\n" + extra + "\r\n\r\n") : null) +
                        (lacking.HasText() ? ("Add something like:\r\n" + lacking + "\r\n\r\n") : null));
        }


        public static void Register<T>(IsolationStrategy strategy) where T : IdentifiableEntity
        {
            strategies.Add(typeof(T), strategy);

            if (strategy == IsolationStrategy.Isolated)
                MixinDeclarations.Register(typeof(T), typeof(IsolationMixin));
        }
    }
}
