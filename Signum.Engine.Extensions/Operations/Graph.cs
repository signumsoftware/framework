using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Operations;
using Signum.Engine.Maps;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Engine.Extensions.Properties;
using System.Xml.Linq;
using System.ComponentModel;

namespace Signum.Engine.Operations
{
    public class Graph<E, S>
        where E : IdentifiableEntity
        where S : struct
    {
        public interface IGraphOperation : IOperation
        {
        }

        public interface IGraphInnerOperation : IGraphOperation
        { 
            S ToState { get; }
        }

        public class Execute : BasicExecute<E>, IGraphInnerOperation
        {
            S? toState;
            public S ToState
            {
                get { return toState.Value; }
                set { toState = value; }
            }
            public S[] FromStates { get; set; }

            public Execute(Enum key) : base(key)
            {
            }

            protected override string OnCanExecute(E entity)
            {
                S state = Graph<E, S>.GetState(entity);
                if (FromStates != null && !FromStates.Contains(state))
                    return Resources.ImpossibleToExecute0FromState1.Formato(Key, state); 

                return base.OnCanExecute(entity);
            }

            protected override void OnExecute(E entity, object[] args)
            {
                S oldState = Graph<E, S>.GetState(entity);

                Graph<E, S>.OnExitState(oldState, entity);

                base.OnExecute(entity, args);

                Graph<E, S>.AssertEnterState(entity, this);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Key));

                if (FromStates == null)
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(Key));
            }
        }

        public class Delete : BasicDelete<E>, IGraphOperation
        {
            public S[] FromStates { get; set; }

            public Delete(Enum key) : base(key)
            {
            }

            protected override string OnCanDelete(E entity)
            {
                S state = Graph<E, S>.GetState(entity);
                if (FromStates != null && !FromStates.Contains(state))
                    return Resources.ImpossibleToExecute0FromState1.Formato(Key, state);

                return base.OnCanDelete(entity);
            }

            protected override void OnDelete(E entity, object[] args)
            {
                S oldState = Graph<E, S>.GetState(entity);

                Graph<E, S>.OnExitState(oldState, entity);

                base.OnDelete(entity, args);
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (FromStates == null)
                    throw new InvalidOperationException("Operation {0} does not have FromStates initialized".Formato(Key));
            }
        }

        public class Construct : BasicConstruct<E>, IGraphInnerOperation
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

            protected override E OnConstruct(object[] args)
            {
                E result = base.OnConstruct(args);

                Graph<E, S>.AssertEnterState(result, this);

                return result;
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Key));
             
            }
        }

        public class ConstructFrom<F> : BasicConstructFrom<F, E>, IGraphInnerOperation
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

            protected override E OnConstruct(F entity, object[] args)
            {
                E result = base.OnConstruct(entity, args);

                Graph<E, S>.AssertEnterState(result, this);

                return result;
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Key));
            }
        }

        public class ConstructFromMany<F> : BasicConstructFromMany<F, E>, IGraphInnerOperation
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

            protected override E OnConstruct(List<Lite<F>> lites, object[] args)
            {
                E result = base.OnConstruct(lites, args);

                Graph<E,S>.AssertEnterState(result, this);

                return result;
            }

            public override string ToString()
            {
                return base.ToString() + " in state " + ToState;
            }

            public override void AssertIsValid()
            {
                base.AssertIsValid();

                if (toState == null)
                    throw new InvalidOperationException("Operation {0} does not have ToState initialized".Formato(Key));
            }
        }

        public class StateOptions
        {
            public Action<E> Enter { get; set; }
            public Action<E> Exit { get; set; }
        }

        protected Graph()
        {
            throw new InvalidOperationException("OperationGraphs should not be instantiated");
        }

        public static Func<E, S> GetState { get; set; }

        public static Action<E, S> EnterState { get; set; }
        public static Action<E, S> ExitState { get; set; }

        public static Dictionary<S, StateOptions> States { get; set; }

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
            
            foreach (var item in OperationLogic.GraphOperations<E,S>())
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
                                Add(s.ToString(), "[Deleted]", dOp.Key);


                    } break;
                    case OperationType.Constructor:
                    case OperationType.ConstructorFrom:
                    case OperationType.ConstructorFromMany:
                    {
                        string from = item.OperationType == OperationType.Constructor ? "[New]" :
                                        item.OperationType == OperationType.ConstructorFrom ? "[From {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) :
                                        item.OperationType == OperationType.ConstructorFromMany ? "[FromMany {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()) : "";

                        Add(from, ((IGraphInnerOperation)item).ToState.ToString(), item.Key);


                    } break;
                }
            }

            return result;
        }

        internal static void OnExitState(S state, E entity)
        {
            StateOptions so = States.TryGetC(state);
            if (so != null && so.Exit != null)
                so.Exit(entity);

            if (ExitState != null)
                ExitState(entity, state);
        }

        internal static void OnEnterState(S state, E entity)
        {
            if (EnterState != null)
                EnterState(entity, state);

            StateOptions sn = States.TryGetC(state);
            if (sn != null && sn.Enter != null)
                sn.Enter(entity);
        }

        internal static void AssertEnterState(E entity, IGraphInnerOperation operation)
        {
            S state = GetState(entity);

            if (!state.Equals(operation.ToState))
                throw new InvalidOperationException("After executing {0} the state should be {1}, but is {2}".Formato(operation.Key ,operation.ToState, state));

            OnEnterState(state, entity);
        }
    }
}
