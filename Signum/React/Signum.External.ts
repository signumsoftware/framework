//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export module CollectionMessage {
  export const And: MessageKey = new MessageKey("CollectionMessage", "And");
  export const Or: MessageKey = new MessageKey("CollectionMessage", "Or");
  export const No0Found: MessageKey = new MessageKey("CollectionMessage", "No0Found");
  export const MoreThanOne0Found: MessageKey = new MessageKey("CollectionMessage", "MoreThanOne0Found");
}

export const DayOfWeek: EnumType<DayOfWeek> = new EnumType<DayOfWeek>("DayOfWeek");
export type DayOfWeek =
  "Sunday" |
  "Monday" |
  "Tuesday" |
  "Wednesday" |
  "Thursday" |
  "Friday" |
  "Saturday";

