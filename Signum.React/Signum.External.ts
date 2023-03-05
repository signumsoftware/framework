//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export module CollectionMessage {
  export const And = new MessageKey("CollectionMessage", "And");
  export const Or = new MessageKey("CollectionMessage", "Or");
  export const No0Found = new MessageKey("CollectionMessage", "No0Found");
  export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
}

export const DayOfWeek = new EnumType<DayOfWeek>("DayOfWeek");
export type DayOfWeek =
  "Sunday" |
  "Monday" |
  "Tuesday" |
  "Wednesday" |
  "Thursday" |
  "Friday" |
  "Saturday";

