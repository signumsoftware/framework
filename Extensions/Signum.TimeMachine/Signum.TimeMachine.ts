//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export module TimeMachineMessage {
  export const TimeMachine: MessageKey = new MessageKey("TimeMachineMessage", "TimeMachine");
  export const EntityDeleted: MessageKey = new MessageKey("TimeMachineMessage", "EntityDeleted");
  export const CompareVersions: MessageKey = new MessageKey("TimeMachineMessage", "CompareVersions");
  export const AllVersions: MessageKey = new MessageKey("TimeMachineMessage", "AllVersions");
  export const SelectedVersions: MessageKey = new MessageKey("TimeMachineMessage", "SelectedVersions");
  export const UIDifferences: MessageKey = new MessageKey("TimeMachineMessage", "UIDifferences");
  export const DataDifferences: MessageKey = new MessageKey("TimeMachineMessage", "DataDifferences");
  export const UISnapshot: MessageKey = new MessageKey("TimeMachineMessage", "UISnapshot");
  export const DataSnapshot: MessageKey = new MessageKey("TimeMachineMessage", "DataSnapshot");
  export const ShowDiffs: MessageKey = new MessageKey("TimeMachineMessage", "ShowDiffs");
  export const YouCanNotSelectMoreThanTwoVersionToCompare: MessageKey = new MessageKey("TimeMachineMessage", "YouCanNotSelectMoreThanTwoVersionToCompare");
  export const BetweenThisTimeRange: MessageKey = new MessageKey("TimeMachineMessage", "BetweenThisTimeRange");
  export const ThisVersionWasCreated: MessageKey = new MessageKey("TimeMachineMessage", "ThisVersionWasCreated");
  export const ThisVersionWasDeleted: MessageKey = new MessageKey("TimeMachineMessage", "ThisVersionWasDeleted");
  export const ThisVersionWasCreatedAndDeleted: MessageKey = new MessageKey("TimeMachineMessage", "ThisVersionWasCreatedAndDeleted");
  export const ThisVersionDidNotChange: MessageKey = new MessageKey("TimeMachineMessage", "ThisVersionDidNotChange");
}

export module TimeMachinePermission {
  export const ShowTimeMachine : Basics.PermissionSymbol = registerSymbol("Permission", "TimeMachinePermission.ShowTimeMachine");
}

