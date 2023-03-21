//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'


export const BootstrapStyle = new EnumType<BootstrapStyle>("BootstrapStyle");
export type BootstrapStyle =
  "Light" |
  "Dark" |
  "Primary" |
  "Secondary" |
  "Success" |
  "Info" |
  "Warning" |
  "Danger";

export module CollapsableCardMessage {
  export const Collapse = new MessageKey("CollapsableCardMessage", "Collapse");
  export const Expand = new MessageKey("CollapsableCardMessage", "Expand");
}

export const CultureInfoEntity = new Type<CultureInfoEntity>("CultureInfo");
export interface CultureInfoEntity extends Entities.Entity {
  Type: "CultureInfo";
  name: string;
  nativeName: string;
  englishName: string;
}

export module CultureInfoOperation {
  export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Save");
  export const Delete : Entities.DeleteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Delete");
}

export const DateSpanEmbedded = new Type<DateSpanEmbedded>("DateSpanEmbedded");
export interface DateSpanEmbedded extends Entities.EmbeddedEntity {
  Type: "DateSpanEmbedded";
  years: number;
  months: number;
  days: number;
}

export module DisabledMessage {
  export const ParentIsDisabled = new MessageKey("DisabledMessage", "ParentIsDisabled");
}

export const DisabledMixin = new Type<DisabledMixin>("DisabledMixin");
export interface DisabledMixin extends Entities.MixinEntity {
  Type: "DisabledMixin";
  isDisabled: boolean;
}

export module DisableOperation {
  export const Disable : Entities.ExecuteSymbol<Entities.Entity> = registerSymbol("Operation", "DisableOperation.Disable");
  export const Enabled : Entities.ExecuteSymbol<Entities.Entity> = registerSymbol("Operation", "DisableOperation.Enabled");
}

export const TimeSpanEmbedded = new Type<TimeSpanEmbedded>("TimeSpanEmbedded");
export interface TimeSpanEmbedded extends Entities.EmbeddedEntity {
  Type: "TimeSpanEmbedded";
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
}

export const TypeConditionSymbol = new Type<TypeConditionSymbol>("TypeCondition");
export interface TypeConditionSymbol extends Entities.Symbol {
  Type: "TypeCondition";
}


