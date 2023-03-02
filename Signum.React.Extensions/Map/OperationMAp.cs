using Signum.Entities.Basics;
using Signum.Entities.Map;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Collections;

namespace Signum.React.Map;

public static class OperationMap
{
    public static OperationMapInfo GetOperationMapInfo(Type type)
    {
        var operations = OperationLogic.TypeOperationsAndConstructors(type);

        var stateTypes = operations.Select(a => a.StateType).Distinct().NotNull().PreAnd(typeof(DefaultState)).ToList();

        Dictionary<Type, LambdaExpression?> expressions = stateTypes
            .ToDictionary(t => t, t => type == typeof(DefaultState) ? null : giGetGraphGetter.GetInvoker(type, t)());

        Dictionary<Type, Dictionary<Enum, int>> counts = expressions.SelectDictionary(t => t.UnNullify(), exp =>
            exp == null ? giCount.GetInvoker(type)() :
            giCountGroupBy.GetInvoker(type, exp.Body.Type)(exp));

        Dictionary<Type, string?> tokens = expressions.SelectDictionary(t => t.UnNullify(), exp => exp == null ? null : GetToken(exp));

        var symbols = operations.Select(a => a.OperationSymbol).ToList();

        var operationCounts = Database.Query<OperationLogEntity>()
            .Where(log => symbols.Contains(log.Operation))
            .GroupBy(log => log.Operation)
            .Select(a => KeyValuePair.Create(a.Key, a.Count()))
            .ToDictionary();

        return new OperationMapInfo
        {
            states = (from t in stateTypes
                      from e in Enum.GetValues(t.UnNullify()).Cast<Enum>()
                      let ignored = e.GetType().GetField(e.ToString(), BindingFlags.Static | BindingFlags.Public)!.HasAttribute<IgnoreAttribute>()
                      select new MapState
                      {
                          count = counts.GetOrThrow(e.GetType()).TryGet(e, 0),
                          ignored = ignored,
                          key = e.ToString(),
                          niceName = e.NiceToString(),
                          isSpecial = t == typeof(DefaultState),
                          color = Engine.Chart.ColorPaletteLogic.ColorFor(EnumEntity.FromEnumUntyped(e)),
                          token = tokens.GetOrThrow(e.GetType()),
                      }).ToList(),

            operations = (from o in operations
                          select new MapOperation
                          {
                              niceName = o.OperationSymbol.NiceToString(),
                              key = o.OperationSymbol.Key,
                              count = operationCounts.TryGet(o.OperationSymbol, 0),
                              fromStates = WithDefaultStateArray(o.UntypedFromStates, DefaultState.Start).Select(a => a.ToString()).ToArray(),
                              toStates = WithDefaultStateArray(o.UntypedToStates, DefaultState.End).Select(a => a.ToString()).ToArray(),
                              fromToStates = o is IGraphFromToStatesOperations gfts ? gfts.GetUntypedFromTo()?.Select(t => new FromToState
                              {
                                  From = t.@from?.ToString() ?? "null",
                                  To = t.to?.ToString() ?? "null"
                              }).ToArray() : null
                          }).ToList()
        };
    }

    static IEnumerable<string> WithDefaultStateArray(IList? enumerable, DefaultState forNull)
    {
        if (enumerable == null)
            return new string[] { forNull.ToString() };

        if (enumerable.Count == 0)
            return new string[] { DefaultState.All.ToString() };

        return enumerable.Cast<object?>().Select(a => a?.ToString() ?? "null");
    }

    static readonly GenericInvoker<Func<LambdaExpression, Dictionary<Enum, int>>> giCountGroupBy =
      new(exp => CountGroupBy((Expression<Func<Entity, DayOfWeek>>)exp));
    static Dictionary<Enum, int> CountGroupBy<T, S>(Expression<Func<T, S>> expression)
        where T : Entity
    {
        return Database.Query<T>().GroupBy(expression).Where(a => a.Key != null).Select(gr => KeyValuePair.Create((Enum)((object)gr.Key!), gr.Count())).ToDictionary();
    }

    static readonly GenericInvoker<Func<Dictionary<Enum, int>>> giCount =
        new(() => Count<Entity>());
    static Dictionary<Enum, int> Count<T>()
        where T : Entity
    {
        return new Dictionary<Enum, int> { { DefaultState.All, Database.Query<T>().Count() } };
    }

    public static Dictionary<(Type fromType, Type toType), string> Tokens = new Dictionary<(Type fromType, Type toType), string>();

    static string GetToken(LambdaExpression expr)
    {
        var tuple = (fromType: expr.Parameters.Single().Type, toType: expr.Body.Type);

        return Tokens.GetOrCreate(tuple, () =>
            "Entity." + Reflector.GetMemberListBase(expr.Body).ToString(a => a.Name, "."));
    }

    static readonly GenericInvoker<Func<LambdaExpression>> giGetGraphGetter =
        new(() => GetGraphGetter<Entity, DayOfWeek>());
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
    public FromToState[]? fromToStates;
}

public class FromToState
{
    public string From;
    public string To; 
}

public class MapState
{
    public string key;
    public string niceName;
    public int count;
    public bool ignored;
    public string? color;
    public string? token;
    public bool isSpecial;
}
