using Signum.Entities.Basics;

namespace Signum.Entities.DiffLog;

public class DiffLogMixin : MixinEntity
{
    protected DiffLogMixin(ModifiableEntity mainEntity, MixinEntity next)
        : base(mainEntity, next)
    {
        this.BindParent();
    }

    [BindParent]
    public BigStringEmbedded InitialState { get; set; } = new BigStringEmbedded();

    [BindParent]
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
