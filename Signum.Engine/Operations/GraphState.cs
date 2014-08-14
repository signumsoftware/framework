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
        where T : IdentifiableEntity
        where S : struct
    {
        public interface IGraphOperation : IOperation
        {
        }

        public interface IGraphToStateOperation : IGraphOperation
        {
            S ToState { get; }
        }

        public interface IGraphFromStatesOperation : IGraphOperation, IGraphHasFromStatesOperation
        {
            List<S> FromStates { get; }
        }

        public class Construct : Graph<T>.Construct, IGraphToStateOperation
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public Construct(ConstructSymbol<T>.Simple symbol)
                : base(symbol)
            {
            }

            protected override void AssertEntity(T entity)
            {
                Graph<T, S>.AssertEnterState((T)entity, this);
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Symbol.Operation));

            }
        }

        public class ConstructFrom<F> : Graph<T>.ConstructFrom<F>, IGraphToStateOperation
            where F : class, IIdentifiable
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public ConstructFrom(ConstructSymbol<T>.From<F> symbol)
                : base(symbol)
            {
            }

            protected override void AssertEntity(T result)
            {
                if (result != null)
                    Graph<T, S>.AssertEnterState(result, this);
            }


            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Symbol.Operation));
            }
        }

        public class ConstructFromMany<F> : Graph<T>.ConstructFromMany<F>, IGraphToStateOperation
            where F : class, IIdentifiable
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public ConstructFromMany(ConstructSymbol<T>.FromMany<F> symbol)
                : base(symbol)
            {
            }

            protected override void AssertEntity(T result)
            {
                if (result != null)
                    Graph<T, S>.AssertEnterState(result, this);
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Symbol));
            }
        }

        public class Execute : Graph<T>.Execute, IGraphToStateOperation, IGraphFromStatesOperation, IEntityOperation
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }
            
            public List<S> FromStates { get; private set; }

            bool IGraphHasFromStatesOperation.HasFromStates
            {
                get { return !FromStates.IsNullOrEmpty(); }
            }

            public Execute(ExecuteSymbol<T> symbol)
                : base(symbol)
            {
                FromStates = new List<S>();
            }

            bool IEntityOperation.HasCanExecute { get { return true; } }

            protected override string OnCanExecute(T entity)
            {
                S state = Graph<T, S>.GetStateFunc(entity);

                if (!FromStates.Contains(state))
                    return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().Formato(
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
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Symbol));

                if (FromStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(Symbol));
            }
        }

        public class Delete : Graph<T>.Delete, IGraphOperation, IGraphFromStatesOperation
        {
            public List<S> FromStates { get; private set; }

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
                    return OperationMessage.StateShouldBe0InsteadOf1.NiceToString().Formato(
                        FromStates.CommaOr(v => ((Enum)(object)v).NiceToString()),
                        ((Enum)(object)state).NiceToString());

                return base.OnCanDelete(entity);
            }

            protected override void OnDelete(T entity, object[] args)
            {
                S oldState = Graph<T, S>.GetStateFunc(entity);

                base.OnDelete(entity, args);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (FromStates.IsEmpty())
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(Symbol.Operation));
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
                GetStateFunc = getState == null ? null : getState.Compile();
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

                            if (gOp.FromStates == null)
                                Add("[All States]", gOp.ToState.ToString(), item.OperationSymbol);
                            else
                                foreach (var s in gOp.FromStates)
                                    Add(s.ToString(), gOp.ToState.ToString(), item.OperationSymbol);


                        } break;
                    case OperationType.Delete:
                        {
                            Delete dOp = (Delete)item;
                            if (dOp.FromStates == null)
                                Add("[All States]", "[Deleted]", item.OperationSymbol);
                            else
                                foreach (var s in dOp.FromStates)
                                    Add(s.ToString(), "[Deleted]", item.OperationSymbol);


                        } break;
                    case OperationType.Constructor:
                    case OperationType.ConstructorFrom:
                    case OperationType.ConstructorFromMany:
                        {
                            string from = item.OperationType == OperationType.Constructor ? "[New]" :
                                            item.OperationType == OperationType.ConstructorFrom ? "[From {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) :
                                            item.OperationType == OperationType.ConstructorFromMany ? "[FromMany {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) : "";

                            Add(from, ((IGraphToStateOperation)item).ToState.ToString(), item.OperationSymbol);


                        } break;
                }
            }

            return result;
        }

        internal static void AssertEnterState(T entity, IGraphToStateOperation operation)
        {
            S state = GetStateFunc(entity);

            if (!state.Equals(operation.ToState))
                throw new InvalidOperationException("After executing {0} the state should be {1}, but is {2}".Formato(operation.OperationSymbol, operation.ToState, state));
        }
    }
}
