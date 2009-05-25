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

namespace Signum.Engine.Operations
{
    public class Graph<E, S>
        where E : IdentifiableEntity
        where S : struct
    {
        public class GraphOption : IActionOption
        {
            Graph<E, S> graph;
            S targetState;
            S[] fromStates; 
            Action<E, object[]> execute;
            Func<E, bool> canExecute;

            public ActionType ActionType { get; set; }

            public GraphOption(Graph<E, S> graph, S targetState, S[] fromStates,
                Action<E, object[]> execute,
                Func<E, bool> canExecute, ActionType actionType)
            {
                this.graph = graph;
                this.targetState = targetState;
                this.fromStates = fromStates;

                if (execute == null)
                    throw new ArgumentException("execute");
 
                this.execute = execute;
                this.canExecute = canExecute;
                this.ActionType = actionType; 
            }

            public bool CanExecute(IIdentifiable ident)
            {
                S state = graph.GetState((E)ident);
                if (fromStates != null && !fromStates.Contains(state))
                    return false;

                if (canExecute != null)
                    return canExecute((E)ident);

                return true;
            }

            public void Execute(IIdentifiable ident, params object[] parameters)
            {
                E entity = (E)ident; 

                S oldState = graph.GetState(entity);
                if (fromStates!= null && !fromStates.Contains(oldState))
                    throw new ApplicationException("State {0} is not compatible with the action".Formato(oldState));

                StateOptions so = graph.States.TryGetC(oldState);
                if (so != null && so.Exit != null)
                    so.Exit(entity);

                if (graph.ExitState != null)
                    graph.ExitState(entity, oldState);

                execute(entity, parameters);

                S newState = graph.GetState(entity);

                if (!newState.Equals(targetState))
                    throw new ApplicationException("After the action the state should be {0} but is {1}".Formato(targetState, newState)); 

                if (graph.EnterState != null)
                    graph.EnterState(entity, newState);

                StateOptions sn = graph.States.TryGetC(newState);
                if (sn != null && sn.Enter != null)
                    sn.Enter(entity);
            }
        }

        public class StateOptions
        {
            public Action<E> Enter;
            public Action<E> Exit;
        }

        protected Func<E, S> GetState { get; set; }

        protected Action<E, S> EnterState {get;set;}
        protected Action<E, S> ExitState {get;set;}

        protected Dictionary<Enum, IActionOption> Actions { get; set; }
        protected Dictionary<S, StateOptions> States { get; set; }

        protected static bool Registered = false;

        public IActionOption Goto(S targetState, S[] fromStates, Action<E, object[]> execute)
        {
            return new GraphOption(this, targetState, fromStates, execute, null, ActionType.Both);
        }

        public IActionOption Goto(S targetState, S[] fromStates, Action<E, object[]> execute, Func<E, bool> canExecute)
        {
            return new GraphOption(this, targetState, fromStates, execute, canExecute, ActionType.Both);
        }

        public IActionOption Goto(S targetState, S[] fromStates, Action<E, object[]> execute, Func<E, bool> canExecute, ActionType actionType)
        {
            return new GraphOption(this, targetState, fromStates, execute, canExecute, actionType);
        }

        public void Register()
        {
            if (Registered)
                throw new ApplicationException("A {0} have allready been registered".Formato(typeof(Graph<E, S>).TypeName()));

            foreach (var item in Actions)
	        {
                ActionLogic.Register<E>(item.Key, item.Value);
	        }

            Registered = true;
        }
    }
}
