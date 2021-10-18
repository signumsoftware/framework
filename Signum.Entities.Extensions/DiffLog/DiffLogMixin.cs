using Signum.Entities.Basics;
using System;

namespace Signum.Entities.DiffLog
{
    [Serializable]
    public class DiffLogMixin : MixinEntity
    {
        protected DiffLogMixin(ModifiableEntity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
            this.RebindEvents();
        }

        [NotifyChildProperty]
        public BigStringEmbedded InitialState { get; set; } = new BigStringEmbedded();

        [NotifyChildProperty]
        public BigStringEmbedded FinalState { get; set; } = new BigStringEmbedded();

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
