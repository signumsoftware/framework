//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const CultureInfoEntity = new Type<CultureInfoEntity>("CultureInfo");
export interface CultureInfoEntity extends Entities.Entity {
  Type: "CultureInfo";
  name: string;
  nativeName: string;
  englishName: string;
  hidden: boolean;
}

export module CultureInfoOperation {
  export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Save");
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


