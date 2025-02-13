using Signum.Utilities.DataStructures;
using System.Collections;
using System.Xml.Linq;

namespace Signum.Operations;

public interface IGraphHasStatesOperation
{
    bool HasFromStates { get; }
}

public interface IGraphFromToStatesOperations
{
    public List<(object? from, object? to)>? GetUntypedFromTo();
}

public class Graph<T, S>
    where T : Entity
{

    public static string GetNiceToString(S state)
    {
        if (state == null)
            return "null";

        if (state is Enum e)
            return e.NiceToString();

        return state.ToString()!;

    }

    public interface IGraphOperation : IOperation
    {
    }

    public interface IGraphToStateOperation : IGraphOperation
    {
        List<S> ToStates { get; }
    }

    public interface IGraphFromStatesOperation : IGraphOperation, IGraphHasStatesOperation
    {
        List<S> FromStates { get; }
    }



    public class Construct : Graph<T>.Construct, IGraphToStateOperation
    {
        public List<S> ToStates { get; private set; } = new List<S>();
        IList? IOperation.UntypedToStates => ToStates;
        Type? IOperation.StateType => typeof(S);
        LambdaExpression? IOperation.GetStateExpression() => GetState;

        public Construct(ConstructSymbol<T>.Simple symbol)
            : base(symbol)
        {
            ToStates = new List<S>();
        }

        protected override void AssertEntity(T entity)
        {
            Graph<T, S>.AssertToState(entity, this);
        }

        public override string ToString()
        {
            return base.ToString() + " in state " + ToStates.CommaOr();
        }

        public override void AssertIsValid()
        {
            AssertGetState();
            base.AssertIsValid();

            if (ToStates.IsEmpty())
                throw new InvalidOperationException("Operation {0} does not have ToStates initialized".FormatWith(this.operationSymbol));

        }
    }

    private static void AssertGetState()
    {
        if (GetState == null)
        {
            var graphName = typeof(Graph<T, S>).TypeName();
            throw new InvalidOperationException($"{graphName}.GetState is not set. Consider writing something like 'GetState = a => a.State' at the beginning of your Register method.");
        }

    }

    public class ConstructFrom<F> : Graph<T>.ConstructFrom<F>, IGraphToStateOperation
        where F : class, IEntity
    {
        public List<S> ToStates { get; private set; } = new List<S>();
        IList? IOperation.UntypedToStates => ToStates;
        Type? IOperation.StateType => typeof(S);


        LambdaExpression? IOperation.GetStateExpression() => GetState;

        public ConstructFrom(ConstructSymbol<T>.From<F> symbol)
            : base(symbol)
        {
        }

        protected override void AssertEntity(T result)
        {
            base.AssertEntity(result);
            Graph<T, S>.AssertToState(result, this);
        }


        public override string ToString()
        {
            return base.ToString() + " in state " + ToStates.CommaOr();
        }

        public override void AssertIsValid()
        {
            AssertGetState();
            base.AssertIsValid();

            if (ToStates.IsEmpty())
                throw new InvalidOperationException("Operation {0} does not have ToStates initialized".FormatWith(operationSymbol));
        }
    }

    public class ConstructFromMany<F> : Graph<T>.ConstructFromMany<F>, IGraphToStateOperation
        where F : class, IEntity
    {
        public List<S> ToStates { get; private set; } = new List<S>();
        IList? IOperation.UntypedToStates => ToStates;
        Type? IOperation.StateType => typeof(S);
        LambdaExpression? IOperation.GetStateExpression() => GetState;


        public ConstructFromMany(ConstructSymbol<T>.FromMany<F> symbol)
            : base(symbol)
        {
            ToStates = new List<S>();
        }

        protected override void AssertEntity(T result)
        {
            Graph<T, S>.AssertToState(result, this);
        }

        public override string ToString()
        {
            return base.ToString() + " in state " + ToStates.CommaOr();
        }

        public override void AssertIsValid()
        {
            AssertGetState();
            base.AssertIsValid();

            if (ToStates.IsEmpty())
                throw new InvalidOperationException("Operation {0} does not have ToStates initialized".FormatWith(operationSymbol));
        }
    }

    public class Execute : Graph<T>.Execute, IGraphToStateOperation, IGraphFromStatesOperation, IEntityOperation, IGraphFromToStatesOperations
    {
        public List<S> FromStates { get; private set; }
        public List<S> ToStates { get; private set; }
        public List<(S from, S to)> FromToStates { get; private set; }

        IList? IOperation.UntypedToStates => ToStates;
        IList? IOperation.UntypedFromStates => FromStates;
        Type? IOperation.StateType => typeof(S);
        LambdaExpression? IOperation.GetStateExpression() => GetState;


        bool IGraphHasStatesOperation.HasFromStates => !FromStates.IsNullOrEmpty();

        public Execute(ExecuteSymbol<T> symbol)
            : base(symbol)
        {
            FromStates = new List<S>();
            ToStates = new List<S>();
            FromToStates = new List<(S from, S to)>();
        }

        protected override string? OnCanExecute(T entity)
        {
            S state = Graph<T, S>.GetStateFunc(entity);

            if (!FromStates.Contains(state))
                return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                    FromStates.CommaOr(v => GetNiceToString(v)),
                    GetNiceToString(state));

            return base.OnCanExecute(entity);
        }

        protected override Action? AssertEntity(T entity)
        {
            if(this.FromToStates.Count == 0)
            {
                return () =>
                {
                    AssertToState(entity, this);
                };
            }
            else
            {
                S initialState = GetStateFunc(entity);
                var targetStates = this.FromToStates.Where(a => object.Equals(a.from, initialState)).Select(a => a.to).ToList();

                return () =>
                {
                    S endState = GetStateFunc(entity);
                    if (!targetStates.Contains(endState))
                        throw new InvalidOperationException("After executing {0} from state {1} should be {2}, but is {3}".FormatWith(operationSymbol, initialState, targetStates.CommaOr(), endState));
                };
            }
        }

        public override void AssertIsValid()
        {
            AssertGetState();
            base.AssertIsValid();

            if (FromToStates.Any())
            {
                if (FromStates.Any())
                    throw new InvalidOperationException("Operation {0} has FromStates and FromToStates at the same time".FormatWith(operationSymbol));

                if (ToStates.Any())
                    throw new InvalidOperationException("Operation {0} has ToStates and FromToStates at the same time".FormatWith(operationSymbol));

                FromStates.AddRange(FromToStates.Select(a => a.from).Distinct());
                ToStates.AddRange(FromToStates.Select(a => a.to).Distinct());
            }
            else
            {
                if (ToStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have ToStates initialized".FormatWith(operationSymbol));

                if (FromStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".FormatWith(operationSymbol));
            }
        }

        public List<(object? from, object? to)>? GetUntypedFromTo()
        {
            if (FromToStates.IsEmpty())
                return null;

            return FromToStates.Select(t => ((object?)t.from, (object?)t.to)).ToList();
        }
    }

    public class Delete : Graph<T>.Delete, IGraphOperation, IGraphFromStatesOperation
    {
        public List<S> FromStates { get; private set; }
        IList? IOperation.UntypedFromStates => FromStates;
        Type? IOperation.StateType => typeof(S);
        LambdaExpression? IOperation.GetStateExpression() => GetState;

        bool IGraphHasStatesOperation.HasFromStates
        {
            get { return !FromStates.IsNullOrEmpty(); }
        }

        public Delete(DeleteSymbol<T> symbol)
            : base(symbol)
        {
            FromStates = new List<S>();
        }

        protected override string? OnCanDelete(T entity)
        {
            S state = Graph<T, S>.GetStateFunc(entity);

            if (!FromStates.Contains(state))
                return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                    FromStates.CommaOr(v => GetNiceToString(v)),
                    GetNiceToString(state));

            return base.OnCanDelete(entity);
        }

        protected override void OnDelete(T entity, object?[]? args)
        {
            AssertGetState();
            S oldState = Graph<T, S>.GetStateFunc(entity);

            base.OnDelete(entity, args);
        }

        public override void AssertIsValid()
        {
            base.AssertIsValid();

            if (FromStates.IsEmpty())
                throw new InvalidOperationException("Operation {0} does not have FromStates initialized".FormatWith(operationSymbol));
        }
    }

    protected Graph()
    {
        throw new InvalidOperationException("OperationGraphs should not be instantiated");
    }

    static Expression<Func<T, S>> getState = null!;
    public static Expression<Func<T, S>> GetState
    {
        get { return getState; }
        set
        {
            getState = value;
            GetStateFunc = getState.Compile();
        }
    }

    public static Func<T, S> GetStateFunc { get; private set; } = null!;


    public static Action<T, S>? EnterState { get; set; }
    public static Action<T, S>? ExitState { get; set; }



    public static XDocument ToDGML()
    {
        return ToDirectedGraph().ToDGML();
    }

    public static DirectedEdgedGraph<string, string> ToDirectedGraph()
    {
        DirectedEdgedGraph<string, string> result = new DirectedEdgedGraph<string, string>();

        void Add(string from, string to, OperationSymbol key)
        {
            Dictionary<string, string> dic = result.TryRelatedTo(from);
            if (dic == null || !dic.ContainsKey(to))
                result.Add(from, to, key.ToString());
            else
                result.Add(from, to, dic[to] + ", " + key.ToString());
        }

        foreach (var item in OperationLogic.GraphOperations<T, S>())
        {
            switch (item.OperationType)
            {
                case OperationType.Execute:
                    {
                        Execute gOp = (Execute)item;

                        foreach (var f in gOp.FromStates)
                            foreach (var t in gOp.ToStates)
                                Add(f!.ToString()!, t!.ToString()!, item.OperationSymbol);


                    } break;
                case OperationType.Delete:
                    {
                        Delete dOp = (Delete)item;
                        foreach (var f in dOp.FromStates)
                            Add(f!.ToString()!, "[Deleted]", item.OperationSymbol);


                    } break;
                case OperationType.Constructor:
                case OperationType.ConstructorFrom:
                case OperationType.ConstructorFromMany:
                    {
                        string from = item.OperationType == OperationType.Constructor ? "[New]" :
                                        item.OperationType == OperationType.ConstructorFrom ? "[From {0}]".FormatWith(item.GetType().GetGenericArguments()[2].TypeName()) :
                                        item.OperationType == OperationType.ConstructorFromMany ? "[FromMany {0}]".FormatWith(item.GetType().GetGenericArguments()[2].TypeName()) : "";

                        var dtoState = (IGraphToStateOperation)item;
                        foreach (var t in dtoState.ToStates)
                            Add(from, t!.ToString()!, item.OperationSymbol);

                    } break;
            }
        }

        return result;
    }

    internal static void AssertToState(T entity, IGraphToStateOperation operation)
    {
        S state = GetStateFunc(entity);

        if (!operation.ToStates.Contains(state))
            throw new InvalidOperationException("After executing {0} the state should be {1}, but is {2}".FormatWith(operation.OperationSymbol, operation.ToStates.CommaOr(), state));
    }
}
