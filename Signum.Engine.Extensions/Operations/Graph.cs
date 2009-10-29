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

namespace Signum.Engine.Operations
{
    public class Graph<E, S>
        where E : IdentifiableEntity
        where S : struct
    {
        public interface IGraphOperation : IOperation
        {
            Graph<E, S> Graph{get;set;} 
            S TargetState { get; }
        }

        public class Goto : BasicExecute<E>, IGraphOperation
        {
            public Graph<E, S> Graph { get; set; } 
            public S TargetState { get; private set; }
            public S[] FromStates { get; set; }

            public Goto(Enum key, S targetState) : base(key)
            {
                this.TargetState = targetState;
            }

            protected override string OnCanExecute(E entity)
            {
                S state = Graph.GetState(entity);
                if (FromStates != null && !FromStates.Contains(state))
                    return "Impossible to execute {0} from state {1}".Formato(Key, state); 

                return base.OnCanExecute(entity);
            }

            protected override void OnExecute(E entity, object[] args)
            {
                S oldState = Graph.GetState(entity);

                Graph.OnExitState(oldState, entity);

                base.OnExecute(entity, args);

                Graph.AssertEnterState(entity, this);
            }
        }

        public class Construct : BasicConstructor<E>, IGraphOperation
        {
            public Graph<E, S> Graph { get; set; }
            public S TargetState { get; private set; }

            public Construct(Enum key, S targetState)
                : base(key)
            {
                this.TargetState = targetState;
            }

            protected override E OnConstruct(object[] args)
            {
                E result = base.OnConstruct(args);

                Graph.AssertEnterState(result, this);

                return result;
            }
        }

        public class ConstructFrom<F> : BasicConstructorFrom<F, E>, IGraphOperation
            where F : class, IIdentifiable
        {
            public Graph<E, S> Graph { get; set; }
            public S TargetState { get; private set; }

            public ConstructFrom(Enum key, S targetState)
                : base(key)
            {
                this.TargetState = targetState;
            }

            protected override E OnConstruct(F entity, object[] args)
            {
                E result = base.OnConstruct(entity, args);

                Graph.AssertEnterState(result, this);

                return result;
            }
        }

        public class ConstructFromMany<F> : BasicConstructorFromMany<F, E>, IGraphOperation
            where F : class, IIdentifiable
        {
            public Graph<E, S> Graph { get; set; }
            public S TargetState { get; private set; }

            public ConstructFromMany(Enum key, S targetState)
                : base(key)
            {
                this.TargetState = targetState;
            }

            protected override E OnConstructor(List<Lite<F>> lites, object[] args)
            {
                E result = base.OnConstructor(lites, args);

                Graph.AssertEnterState(result, this);

                return result;
            }
        }

        public class StateOptions
        {
            public Action<E> Enter { get; set; }
            public Action<E> Exit { get; set; }
        }

        protected Func<E, S> GetState { get; set; }

        protected Action<E, S> EnterState { get; set; }
        protected Action<E, S> ExitState { get; set; }

        protected List<IGraphOperation> Operations { get; set; }
        protected Dictionary<S, StateOptions> States { get; set; }

        protected static bool Registered = false;

        public void Register()
        {
            if (Registered)
                throw new ApplicationException("A {0} have already been registered".Formato(typeof(Graph<E, S>).TypeName()));

            var errors = Operations.GroupCount(a => a.Key).Where(kvp => kvp.Value > 1).ToList();

            if (errors.Count != 0)
                throw new ApplicationException("The Following Keys have been repeated in {0}:\r\n{1}".Formato(GetType(), errors.ToString(a => " - {0} ({1})".Formato(a.Key, a.Value), "\r\n")));

            foreach (var operation in Operations)
	        {
                operation.Graph = this;

                OperationLogic.Register(operation);
	        }

            Registered = true;
        }

        public DirectedEdgedGraph<string, Enum> ToDirectedGraph()
        {

            DirectedEdgedGraph<string, Enum> result = new DirectedEdgedGraph<string, Enum>();
            foreach (var item in Operations)
            {
                switch (item.OperationType)
                {
                    case OperationType.Execute:
                        foreach (var s in ((Goto)item).FromStates)
                            result.Add(s.ToString(), item.TargetState.ToString(), item.Key);
                        break;
                    case OperationType.Constructor: result.Add("[New]", item.TargetState.ToString(), item.Key); break;
                    case OperationType.ConstructorFrom: result.Add("[From {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()), item.TargetState.ToString(), item.Key); break;
                    case OperationType.ConstructorFromMany: result.Add("[FromMany {0}]".Formato(item.GetType().GetGenericArguments()[2].TypeName()), item.TargetState.ToString(), item.Key); break;
                }
            }

            return result;
        }

        internal void OnExitState(S state, E entity)
        {
            StateOptions so = States.TryGetC(state);
            if (so != null && so.Exit != null)
                so.Exit(entity);

            if (ExitState != null)
                ExitState(entity, state);
        }

        internal void OnEnterState(S state, E entity)
        {
            if (EnterState != null)
                EnterState(entity, state);

            StateOptions sn = States.TryGetC(state);
            if (sn != null && sn.Enter != null)
                sn.Enter(entity);
        }

        internal void AssertEnterState(E entity, IGraphOperation operation)
        {
            S state = GetState(entity);

            if (!state.Equals(operation.TargetState))
                throw new ApplicationException("After the action the state should be {0} but is {1}".Formato(operation.TargetState, state));

            OnEnterState(state, entity);
        }
    }
}
