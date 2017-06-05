using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using System.Threading;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.SchemaInfoTables;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using System.Xml.Linq;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Entities.Basics;

namespace Signum.Engine.Operations
{
    public interface IGraphHasFromStatesOperation
    {
        bool HasFromStates { get; }
    }

    public class Graph<T, S>
        where T : Entity
    {
        public interface IGraphOperation : IOperation
        {
        }

        public interface IGraphToStateOperation : IGraphOperation
        {
            List<S> ToStates { get; }
        }

        public interface IGraphFromStatesOperation : IGraphOperation, IGraphHasFromStatesOperation
        {
            List<S> FromStates { get; }
        }

        public class Construct : Graph<T>.Construct, IGraphToStateOperation
        {
            public List<S> ToStates { get; private set; } = new List<S>();
            IEnumerable<Enum> IOperation.UntypedToStates { get { return ToStates.Cast<Enum>(); } }
            Type IOperation.StateType { get { return typeof(S); } }

            public Construct(ConstructSymbol<T>.Simple symbol)
                : base(symbol)
            {
                ToStates = new List<S>();
            }

            protected Construct(OperationSymbol symbol)
                : base(symbol)
            {
            }

            public static new Construct Untyped<B>(ConstructSymbol<B>.Simple symbol)
                 where B : class, IEntity
            {
                return new Construct(symbol.Symbol);
            }

            protected override void AssertEntity(T entity)
            {
                Graph<T, S>.AssertEnterState((T)entity, this);
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
            IEnumerable<Enum> IOperation.UntypedToStates { get { return ToStates.Cast<Enum>(); } }
            Type IOperation.StateType { get { return typeof(S); } }

            public ConstructFrom(ConstructSymbol<T>.From<F> symbol)
                : base(symbol)
            {
            }

            protected ConstructFrom(OperationSymbol operationSymbol, Type baseType)
                : base(operationSymbol, baseType)
            {
            }

            public static new ConstructFrom<F> Untyped<B>(ConstructSymbol<B>.From<F> symbol)
                 where B : class, IEntity
            {
                return new ConstructFrom<F>(symbol.Symbol, symbol.BaseType);
            }

            protected override void AssertEntity(T result)
            {
                if (result != null)
                    Graph<T, S>.AssertEnterState(result, this);
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
            IEnumerable<Enum> IOperation.UntypedToStates { get { return ToStates.Cast<Enum>(); } }
            Type IOperation.StateType { get { return typeof(S); } }

            public ConstructFromMany(ConstructSymbol<T>.FromMany<F> symbol)
                : base(symbol)
            {
                ToStates = new List<S>();
            }


            protected ConstructFromMany(OperationSymbol operationSymbol, Type baseType)
                : base(operationSymbol, baseType)
            {
            }

            public static new ConstructFromMany<F> Untyped<B>(ConstructSymbol<B>.FromMany<F> symbol)
                 where B : class, IEntity
            {
                return new ConstructFromMany<F>(symbol.Symbol, symbol.BaseType);
            }


            protected override void AssertEntity(T result)
            {
                if (result != null)
                    Graph<T, S>.AssertEnterState(result, this);
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

        public class Execute : Graph<T>.Execute, IGraphToStateOperation, IGraphFromStatesOperation, IEntityOperation
        {
            public List<S> FromStates { get; private set; }
            public List<S> ToStates { get; private set; }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return ToStates.Cast<Enum>(); } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return FromStates.Cast<Enum>(); } }
            Type IOperation.StateType { get { return typeof(S); } }

            bool IGraphHasFromStatesOperation.HasFromStates
            {
                get { return !FromStates.IsNullOrEmpty(); }
            }

            public Execute(ExecuteSymbol<T> symbol)
                : base(symbol)
            {
                FromStates = new List<S>();
                ToStates = new List<S>();
            }

            bool IEntityOperation.HasCanExecute { get { return true; } }

            protected override string OnCanExecute(T entity)
            {
                S state = Graph<T, S>.GetStateFunc(entity);

                if (!FromStates.Contains(state))
                    return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                        FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                        ((Enum)(object)state).NiceToString());

                return base.OnCanExecute(entity);
            }

            protected override void AssertEntity(T entity)
            {
                Graph<T, S>.AssertEnterState(entity, this);
            }

            public override void AssertIsValid()
            {
                AssertGetState();
                base.AssertIsValid();

                if (ToStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have ToStates initialized".FormatWith(Symbol.Symbol));

                if (FromStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".FormatWith(Symbol));
            }
        }

        public class Delete : Graph<T>.Delete, IGraphOperation, IGraphFromStatesOperation
        {
            public List<S> FromStates { get; private set; }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return FromStates.Cast<Enum>(); } }
            Type IOperation.StateType { get { return typeof(S); } }

            bool IGraphHasFromStatesOperation.HasFromStates
            {
                get { return !FromStates.IsNullOrEmpty(); }
            }

            public Delete(DeleteSymbol<T> symbol)
                : base(symbol)
            {
                FromStates = new List<S>();
            }

            protected override string OnCanDelete(T entity)
            {
                S state = Graph<T, S>.GetStateFunc(entity);

                if (!FromStates.Contains(state))
                    return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().FormatWith(
                        FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                        ((Enum)(object)state).NiceToString());

                return base.OnCanDelete(entity);
            }

            protected override void OnDelete(T entity, object[] args)
            {
                AssertGetState();
                S oldState = Graph<T, S>.GetStateFunc(entity);

                base.OnDelete(entity, args);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (FromStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".FormatWith(Symbol.Symbol));
            }
        }

        protected Graph()
        {
            throw new InvalidOperationException("OperationGraphs should not be instantiated");
        }

        static Expression<Func<T, S>> getState;
        public static Expression<Func<T, S>> GetState
        {
            get { return getState; }
            set
            {
                getState = value;
                GetStateFunc = getState?.Compile();
            }
        }

        public static Func<T, S> GetStateFunc{get; private set;}


        public static Action<T, S> EnterState { get; set; }
        public static Action<T, S> ExitState { get; set; }



        public static XDocument ToDGML()
        {
            return ToDirectedGraph().ToDGML();
        }

        public static DirectedEdgedGraph<string, string> ToDirectedGraph()
        {
            DirectedEdgedGraph<string, string> result = new DirectedEdgedGraph<string, string>();

            Action<string, string, OperationSymbol> Add = (from, to, key) =>
                {
                    Dictionary<string, string> dic = result.TryRelatedTo(from);
                    if (dic == null || !dic.ContainsKey(to))
                        result.Add(from, to, key.ToString());
                    else
                        result.Add(from, to, dic[to] + ", " + key.ToString());
                };

            foreach (var item in OperationLogic.GraphOperations<T, S>())
            {
                switch (item.OperationType)
                {
                    case OperationType.Execute:
                        {
                            Execute gOp = (Execute)item;

                            foreach (var f in gOp.FromStates)
                                foreach (var t in gOp.ToStates)
                                    Add(f.ToString(), t.ToString(), item.OperationSymbol);


                        } break;
                    case OperationType.Delete:
                        {
                            Delete dOp = (Delete)item;
                            foreach (var f in dOp.FromStates)
                                Add(f.ToString(), "[Deleted]", item.OperationSymbol);


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
                                Add(from, t.ToString(), item.OperationSymbol);

                        } break;
                }
            }

            return result;
        }

        internal static void AssertEnterState(T entity, IGraphToStateOperation operation)
        {
            S state = GetStateFunc(entity);

            if (!operation.ToStates.Contains(state))
                throw new InvalidOperationException("After executing {0} the state should be {1}, but is {2}".FormatWith(operation.OperationSymbol, operation.ToStates.CommaOr(), state));
        }
    }
}
