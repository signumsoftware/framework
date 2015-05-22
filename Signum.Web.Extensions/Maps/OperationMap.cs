using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.SchemaInfoTables;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Map;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Drawing;

namespace Signum.Web.Maps
{
    public static class OperationMap
    {
        public static OperationMapInfo GetOperationMapInfo(Type type)
        {
            var operations = OperationLogic.TypeOperationsAndConstructors(type);

            var stateTypes = operations.Select(a => a.UntypedFromStates == null ? typeof(DefaultState) : a.StateType)
                .Concat(operations.Select(a => a.UntypedToStates == null ? typeof(DefaultState) : a.StateType))
                .Distinct().Where(t=>t!=null).ToList();

            Dictionary<Type, LambdaExpression> expressions = stateTypes
                .ToDictionary(t => t, t => type == typeof(DefaultState) ? null : giGetGraphGetter.GetInvoker(type, t)());

            Dictionary<Type, Dictionary<Enum, int>> counts = expressions.SelectDictionary(t => t, exp => 
                exp == null ? giCount.GetInvoker(type)(): 
                giCountGroupBy.GetInvoker(type, exp.Body.Type)(exp));

            Dictionary<Type, string> tokens = expressions.SelectDictionary(t => t, exp => exp == null ? null: GetToken(exp));

            var symbols = operations.Select(a=>a.OperationSymbol).ToList();

            var operationCounts = Database.Query<OperationLogEntity>()
                .Where(log => symbols.Contains(log.Operation))
                .GroupBy(log => log.Operation)
                .Select(a => KVP.Create(a.Key, a.Count()))
                .ToDictionary();

            return new OperationMapInfo
            {
                states = (from t in stateTypes
                          from e in Enum.GetValues(t.UnNullify()).Cast<Enum>()
                          let ignored = e.GetType().GetField(e.ToString(), BindingFlags.Static | BindingFlags.Public).HasAttribute<IgnoreAttribute>()
                          select new MapState
                          {
                              count = counts.GetOrThrow(e.GetType()).TryGet(e, 0),
                              ignored = ignored,
                              key = StateKey(e),
                              niceName = e.NiceToString(),
                              color = Engine.Chart.ChartColorLogic.ColorFor(EnumEntity.FromEnumUntyped(e)).TryToHtml(),
                              link = e.Equals(DefaultState.Start) || e.Equals(DefaultState.End) || ignored ? null :
                              e.Equals(DefaultState.All) ? new FindOptions(type) { SearchOnLoad = true }.ToString() :
                              new FindOptions(type)
                              {
                                  FilterOptions = { new FilterOption(tokens.GetOrThrow(e.GetType()), e) },
                                  SearchOnLoad = true
                              }.ToString(),
                          }).ToList(),
                operations = (from o in operations
                              select new MapOperation
                              {
                                  niceName = o.OperationSymbol.NiceToString(),
                                  key = o.OperationSymbol.Key,
                                  count = operationCounts.TryGet(o.OperationSymbol, 0),
                                  fromStates = WithDefaultStateArray(o.UntypedFromStates, DefaultState.Start).Select(StateKey).ToArray(),
                                  toStates = WithDefaultStateArray(o.UntypedToStates, DefaultState.End).Select(StateKey).ToArray(),
                                  link = new FindOptions(typeof(OperationLogEntity))
                                  {
                                      FilterOptions = { new FilterOption("Operation", o.OperationSymbol) },
                                      SearchOnLoad = true
                                  }.ToString(),
                              }).ToList()
            };
        }

        static IEnumerable<Enum> WithDefaultStateArray(IEnumerable<Enum> enumerable, DefaultState forNull)
        {
            if (enumerable == null)
                return new Enum[] { forNull };

            if (enumerable.IsEmpty())
                return new Enum[] { DefaultState.All };

            return enumerable;
        }

        private static string StateKey(Enum e)
        {
            return e.GetType().Name + "." + e.ToString();
        }

        static readonly GenericInvoker<Func<LambdaExpression, Dictionary<Enum, int>>> giCountGroupBy =
          new GenericInvoker<Func<LambdaExpression, Dictionary<Enum, int>>>(exp => CountGroupBy((Expression<Func<Entity, DayOfWeek>>)exp));
        static Dictionary<Enum, int> CountGroupBy<T, S>(Expression<Func<T, S>> expression)
            where T : Entity
        {
            return Database.Query<T>().GroupBy(expression).Select(gr => KVP.Create((Enum)(object)gr.Key, gr.Count())).ToDictionary();
        }

        static readonly GenericInvoker<Func<Dictionary<Enum, int>>> giCount =
            new GenericInvoker<Func<Dictionary<Enum, int>>>(() => Count<Entity>());
        static Dictionary<Enum, int> Count<T>()
            where T : Entity
        {
            return new Dictionary<Enum, int> { { DefaultState.All, Database.Query<T>().Count() } };
        }

        public static Dictionary<Tuple<Type, Type>, string> Tokens = new Dictionary<Tuple<Type, Type>, string>();

        static string GetToken(LambdaExpression expr)
        {
            var tuple = Tuple.Create(expr.Parameters.Single().Type, expr.Body.Type);

            return Tokens.GetOrCreate(tuple, () =>
                "Entity." + Reflector.GetMemberListBase(expr.Body).ToString(a => a.Name, "."));
        }

        static readonly GenericInvoker<Func<LambdaExpression>> giGetGraphGetter = 
            new GenericInvoker<Func<LambdaExpression>>(() => GetGraphGetter<Entity, DayOfWeek>());
        static Expression<Func<T, S>> GetGraphGetter<T, S>()
              where T : Entity
        {
            return Graph<T, S>.GetState;
        }
    }

    public class OperationMapInfo
    {
        public List<MapState> states;
        public List<MapOperation> operations;
    }

    public class MapOperation
    {
        public string key;
        public string niceName;
        public int count;
        public string[] fromStates;
        public string[] toStates;
        public string link;
    }

    public class MapState
    {
        public string key;
        public string niceName;
        public string link;
        public int count;
        public bool ignored;
        public string color;
    }
}