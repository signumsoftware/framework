using System;

namespace Signum.Entities.DiffLog
{
    [Serializable]
    public class DiffLogMixin : MixinEntity
    {
        protected DiffLogMixin(ModifiableEntity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        [SqlDbType(Size = int.MaxValue)]
        public string? InitialState { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string? FinalState { get; set; }

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
