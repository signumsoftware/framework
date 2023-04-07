//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'


export module DiffLogMessage {
  export const PreviousLog = new MessageKey("DiffLogMessage", "PreviousLog");
  export const NextLog = new MessageKey("DiffLogMessage", "NextLog");
  export const CurrentEntity = new MessageKey("DiffLogMessage", "CurrentEntity");
  export const NavigatesToThePreviousOperationLog = new MessageKey("DiffLogMessage", "NavigatesToThePreviousOperationLog");
  export const DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState");
  export const StateWhenTheOperationStarted = new MessageKey("DiffLogMessage", "StateWhenTheOperationStarted");
  export const DifferenceBetweenInitialStateAndFinalState = new MessageKey("DiffLogMessage", "DifferenceBetweenInitialStateAndFinalState");
  export const StateWhenTheOperationFinished = new MessageKey("DiffLogMessage", "StateWhenTheOperationFinished");
  export const DifferenceBetweenFinalStateAndTheInitialStateOfNextLog = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheInitialStateOfNextLog");
  export const NavigatesToTheNextOperationLog = new MessageKey("DiffLogMessage", "NavigatesToTheNextOperationLog");
  export const DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity");
  export const NavigatesToTheCurrentEntity = new MessageKey("DiffLogMessage", "NavigatesToTheCurrentEntity");
}

export const DiffLogMixin = new Type<DiffLogMixin>("DiffLogMixin");
export interface DiffLogMixin extends Entities.MixinEntity {
  Type: "DiffLogMixin";
  initialState: Entities.BigStringEmbedded;
  finalState: Entities.BigStringEmbedded;
  cleaned: boolean;
}

