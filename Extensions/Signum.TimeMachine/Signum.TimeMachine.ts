//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export module TimeMachineMessage {
  export const TimeMachine = new MessageKey("TimeMachineMessage", "TimeMachine");
  export const EntityDeleted = new MessageKey("TimeMachineMessage", "EntityDeleted");
  export const CompareVersions = new MessageKey("TimeMachineMessage", "CompareVersions");
  export const AllVersions = new MessageKey("TimeMachineMessage", "AllVersions");
  export const SelectedVersions = new MessageKey("TimeMachineMessage", "SelectedVersions");
  export const UIDifferences = new MessageKey("TimeMachineMessage", "UIDifferences");
  export const DataDifferences = new MessageKey("TimeMachineMessage", "DataDifferences");
  export const UISnapshot = new MessageKey("TimeMachineMessage", "UISnapshot");
  export const DataSnapshot = new MessageKey("TimeMachineMessage", "DataSnapshot");
  export const ShowDiffs = new MessageKey("TimeMachineMessage", "ShowDiffs");
  export const YouCanNotSelectMoreThanTwoVersionToCompare = new MessageKey("TimeMachineMessage", "YouCanNotSelectMoreThanTwoVersionToCompare");
  export const BetweenThisTimeRange = new MessageKey("TimeMachineMessage", "BetweenThisTimeRange");
  export const ThisVersionWasCreated = new MessageKey("TimeMachineMessage", "ThisVersionWasCreated");
  export const ThisVersionWasDeleted = new MessageKey("TimeMachineMessage", "ThisVersionWasDeleted");
  export const ThisVersionWasCreatedAndDeleted = new MessageKey("TimeMachineMessage", "ThisVersionWasCreatedAndDeleted");
  export const ThisVersionDidNotChange = new MessageKey("TimeMachineMessage", "ThisVersionDidNotChange");
}

export module TimeMachinePermission {
  export const ShowTimeMachine : Basics.PermissionSymbol = registerSymbol("Permission", "TimeMachinePermission.ShowTimeMachine");
}

