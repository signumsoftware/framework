//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export module MapMessage {
  export const Map: MessageKey = new MessageKey("MapMessage", "Map");
  export const Namespace: MessageKey = new MessageKey("MapMessage", "Namespace");
  export const TableSize: MessageKey = new MessageKey("MapMessage", "TableSize");
  export const Columns: MessageKey = new MessageKey("MapMessage", "Columns");
  export const Rows: MessageKey = new MessageKey("MapMessage", "Rows");
  export const Press0ToExploreEachTable: MessageKey = new MessageKey("MapMessage", "Press0ToExploreEachTable");
  export const Press0ToExploreStatesAndOperations: MessageKey = new MessageKey("MapMessage", "Press0ToExploreStatesAndOperations");
  export const Filter: MessageKey = new MessageKey("MapMessage", "Filter");
  export const Color: MessageKey = new MessageKey("MapMessage", "Color");
  export const State: MessageKey = new MessageKey("MapMessage", "State");
  export const StateColor: MessageKey = new MessageKey("MapMessage", "StateColor");
  export const RowsHistory: MessageKey = new MessageKey("MapMessage", "RowsHistory");
  export const TableSizeHistory: MessageKey = new MessageKey("MapMessage", "TableSizeHistory");
}

export module MapPermission {
  export const ViewMap : Basics.PermissionSymbol = registerSymbol("Permission", "MapPermission.ViewMap");
}

