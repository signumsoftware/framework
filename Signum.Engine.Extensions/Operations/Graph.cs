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
        public class Goto : IOperation
        {
            internal Graph<E, S> Graph { get; set; }
            public S TargetState { get; private set; }
            public S[] FromStates { get; set; } 
            public Action<E, object[]> Execute {get;set;}
            public Func<E, bool> CanExecute {get;set;}
            public OperationFlags Flags { get; set; }

            public Goto(S targetState)
            {
                this.TargetState = targetState;
            }

            bool IOperation.CanExecuteOperation(IIdentifiable ident)
            {
                S state = Graph.GetState((E)ident);
                if (FromStates != null && !FromStates.Contains(state))
                    return false;

                if (CanExecute != null)
                    return CanExecute((E)ident);

                return true;
            }

            void IOperation.ExecuteOperation(IIdentifiable ident, params object[] parameters)
            {
                E entity = (E)ident; 

                S oldState = Graph.GetState(entity);
                if (FromStates!= null && !FromStates.Contains(oldState))
                    throw new ApplicationException("State {0} is not compatible with the action".Formato(oldState));

                StateOptions so = Graph.States.TryGetC(oldState);
                if (so != null && so.Exit != null)
                    so.Exit(entity);

                if (Graph.ExitState != null)
                    Graph.ExitState(entity, oldState);

                Execute(entity, parameters);

                S newState = Graph.GetState(entity);

                if (!newState.Equals(TargetState))
                    throw new ApplicationException("After the action the state should be {0} but is {1}".Formato(TargetState, newState)); 

                if (Graph.EnterState != null)
                    Graph.EnterState(entity, newState);

                StateOptions sn = Graph.States.TryGetC(newState);
                if (sn != null && sn.Enter != null)
                    sn.Enter(entity);
            }
        }

        public class StateOptions
        {
            public Action<E> Enter { get; set; }
            public Action<E> Exit { get; set; }
        }

        protected Func<E, S> GetState { get; set; }

        protected Action<E, S> EnterState {get;set;}
        protected Action<E, S> ExitState {get;set;}

        protected Dictionary<Enum, Goto> Operations { get; set; }
        protected Dictionary<S, StateOptions> States { get; set; }

        protected static bool Registered = false;

        public void Register()
        {
            if (Registered)
                throw new ApplicationException("A {0} have allready been registered".Formato(typeof(Graph<E, S>).TypeName()));

            foreach (var item in Operations)
	        {
                item.Value.Graph= this;
                if (item.Value.Execute == null)
                    throw new ApplicationException("Operation {0} does not have Execute initialized".Formato(item.Key)); 
                OperationLogic.Register<E>(item.Key, item.Value);
	        }

            Registered = true;
        }

        public DirectedEdgedGraph<S, Enum> ToDirectedGraph()
        {
            return DirectedEdgedGraph<S, Enum>.Generate(EnumExtensions.GetValues<S>(), ConnectionsFrom);
        }

        IEnumerable<KeyValuePair<S, Enum>> ConnectionsFrom(S state)
        {
            return from kvp in this.Operations
                   where kvp.Value.FromStates.TryCS(ar => ar.Contains(state)) ?? false
                   select new KeyValuePair<S, Enum>(kvp.Value.TargetState, kvp.Key);
        }
    }
}
