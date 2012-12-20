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
using Signum.Engine.Properties;

namespace Signum.Engine.Operations
{
    public interface IGraphHasFromStatesOperation
    {
        bool HasFromStates { get; }
    }

    public class Graph<E, S>
        where E : IdentifiableEntity
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
            S[] FromStates { get; }
        }

        public class Execute : BasicExecute<E>, IGraphToStateOperation, IGraphFromStatesOperation, IEntityOperation
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }
            
            public S[] FromStates { get; set; }

            bool IGraphHasFromStatesOperation.HasFromStates
            {
                get { return !FromStates.IsNullOrEmpty(); }
            }

            public Execute(Enum key)
                : base(key)
            {
            }

            bool IEntityOperation.HasCanExecute { get { return true; } }

            protected override string OnCanExecute(E entity)
            {
                S state = Graph<E, S>.GetStateFunc(entity);

                string stateError = state.InState(key, FromStates);
                if (stateError.HasText())
                    return stateError;

                return base.OnCanExecute(entity);
            }

            protected override void OnBeginOperation(E entity)
            {
                base.OnBeginOperation(entity);

                S oldState = Graph<E, S>.GetStateFunc(entity);
            }

            protected override void OnEndOperation(E entity)
            {
                Graph<E, S>.AssertEnterState(entity, this);

                base.OnEndOperation(entity);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(key));

                if (FromStates == null)
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(key));
            }
        }

        public class Delete : BasicDelete<E>, IGraphOperation, IGraphFromStatesOperation
        {
            public S[] FromStates { get; set; }

            bool IGraphHasFromStatesOperation.HasFromStates
            {
                get { return !FromStates.IsNullOrEmpty(); }
            }

            public Delete(Enum key)
                : base(key)
            {
            }

            protected override string OnCanDelete(E entity)
            {
                S state = Graph<E, S>.GetStateFunc(entity);
                if (FromStates != null && !FromStates.Contains(state))
                    return Resources.ImpossibleToExecute0FromState1.Formato(key, ((Enum)(object)state).NiceToString());

                return base.OnCanDelete(entity);
            }

            protected override void OnDelete(E entity, object[] args)
            {
                S oldState = Graph<E, S>.GetStateFunc(entity);

                base.OnDelete(entity, args);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (FromStates == null)
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(key));
            }
        }

        public class Construct : BasicConstruct<E>, IGraphToStateOperation
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public Construct(Enum key)
                : base(key)
            {
            }

            protected override void OnEndOperation(E entity)
            {
                Graph<E, S>.AssertEnterState((E)entity, this);

                base.OnEndOperation(entity);
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(key));

            }
        }

        public class ConstructFrom<F> : BasicConstructFrom<F, E>, IGraphToStateOperation
            where F : class, IIdentifiable
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public ConstructFrom(Enum key)
                : base(key)
            {
            }

            protected override void OnEndOperation(E result)
            {
                Graph<E, S>.AssertEnterState(result, this);

                base.OnEndOperation(result);
            }


            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(key));
            }
        }

        public class ConstructFromMany<F> : BasicConstructFromMany<F, E>, IGraphToStateOperation
            where F : class, IIdentifiable
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }

            public ConstructFromMany(Enum key)
                : base(key)
            {
            }

            protected override void OnEndOperation(E result)
            {
                Graph<E, S>.AssertEnterState(result, this);

                base.OnEndOperation(result);
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(key));
            }
        }

        protected Graph()
        {
            throw new InvalidOperationException("OperationGraphs should not be instantiated");
        }

        static Expression<Func<E, S>> getState;
        public static Expression<Func<E, S>> GetState
        {
            get { return getState; }
            set
            {
                getState = value;
                GetStateFunc = getState == null ? null : getState.Compile();
            }
        }

        public static Func<E, S> GetStateFunc{get; private set;}


        public static Action<E, S> EnterState { get; set; }
        public static Action<E, S> ExitState { get; set; }



        public static XDocument ToDGML()
        {
            return ToDirectedGraph().ToDGML();
        }

        public static DirectedEdgedGraph<string, string> ToDirectedGraph()
        {
            DirectedEdgedGraph<string, string> result = new DirectedEdgedGraph<string, string>();

            Action<string, string, Enum> Add = (from, to, key) =>
                {
                    Dictionary<string, string> dic = result.TryRelatedTo(from);
                    if (dic == null || !dic.ContainsKey(to))
                        result.Add(from, to, key.ToString());
                    else
                        result.Add(from, to, dic[to] + ", " + key.ToString());
                };

            foreach (var item in OperationLogic.GraphOperations<E, S>())
            {
                switch (item.OperationType)
                {
                    case OperationType.Execute:
                        {
                            Execute gOp = (Execute)item;

                            if (gOp.FromStates == null)
                                Add("[All States]", gOp.ToState.ToString(), item.Key);
                            else
                                foreach (var s in gOp.FromStates)
                                    Add(s.ToString(), gOp.ToState.ToString(), item.Key);


                        } break;
                    case OperationType.Delete:
                        {
                            Delete dOp = (Delete)item;
                            if (dOp.FromStates == null)
                                Add("[All States]", "[Deleted]", item.Key);
                            else
                                foreach (var s in dOp.FromStates)
                                    Add(s.ToString(), "[Deleted]", item.Key);


                        } break;
                    case OperationType.Constructor:
                    case OperationType.ConstructorFrom:
                    case OperationType.ConstructorFromMany:
                        {
                            string from = item.OperationType == OperationType.Constructor ? "[New]" :
                                            item.OperationType == OperationType.ConstructorFrom ? "[From {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) :
                                            item.OperationType == OperationType.ConstructorFromMany ? "[FromMany {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) : "";

                            Add(from, ((IGraphToStateOperation)item).ToState.ToString(), item.Key);


                        } break;
                }
            }

            return result;
        }

        internal static void AssertEnterState(E entity, IGraphToStateOperation operation)
        {
            S state = GetStateFunc(entity);

            if (!state.Equals(operation.ToState))
                throw new InvalidOperationException("After executing {0} the state should be {1}, but is {2}".Formato(operation.Key, operation.ToState, state));
        }
    }
}
