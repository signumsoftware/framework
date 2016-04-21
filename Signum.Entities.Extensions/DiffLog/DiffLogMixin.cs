using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DiffLog
{
    [Serializable]
    public class DiffLogMixin : MixinEntity
    {
        protected DiffLogMixin(Entity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        [SqlDbType(Size = int.MaxValue)]
        public string InitialState { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string FinalState { get; set; }

        public bool Cleaned { get; set; }
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
