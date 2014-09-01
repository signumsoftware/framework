using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DiffLog
{
    [Serializable]
    public class DiffLogMixin : MixinEntity
    {
        protected DiffLogMixin(IdentifiableEntity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        [SqlDbType(Size = int.MaxValue)]
        string initialState;
        public string InitialState
        {
            get { return initialState; }
            set { Set(ref initialState, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string finalState;
        public string FinalState
        {
            get { return finalState; }
            set { Set(ref finalState, value); }
        }
    }

    public enum DiffLogMessage
    {
        PreviousLog,
        NextLog,
        CurrentEntity,

        NavigatesToThePreviousOperationLog,
        DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState,
        StateWhenTheOperationStarted,
        DifferenceBetweenInitialStateAndFinalState,
        StateWhenTheOperationFinished,
        DifferenceBetweenFinalStateAndTheInitialStateOfNextLog,
        NavigatesToTheNextOperationLog,
        DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity,
        NavigatesToTheCurrentEntity,
    }
}
