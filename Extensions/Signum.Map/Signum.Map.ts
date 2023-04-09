//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export module MapMessage {
  export const Map = new MessageKey("MapMessage", "Map");
  export const Namespace = new MessageKey("MapMessage", "Namespace");
  export const TableSize = new MessageKey("MapMessage", "TableSize");
  export const Columns = new MessageKey("MapMessage", "Columns");
  export const Rows = new MessageKey("MapMessage", "Rows");
  export const Press0ToExploreEachTable = new MessageKey("MapMessage", "Press0ToExploreEachTable");
  export const Press0ToExploreStatesAndOperations = new MessageKey("MapMessage", "Press0ToExploreStatesAndOperations");
  export const Filter = new MessageKey("MapMessage", "Filter");
  export const Color = new MessageKey("MapMessage", "Color");
  export const State = new MessageKey("MapMessage", "State");
  export const StateColor = new MessageKey("MapMessage", "StateColor");
  export const RowsHistory = new MessageKey("MapMessage", "RowsHistory");
  export const TableSizeHistory = new MessageKey("MapMessage", "TableSizeHistory");
}

export module MapPermission {
  export const ViewMap : Basics.PermissionSymbol = registerSymbol("Permission", "MapPermission.ViewMap");
}

