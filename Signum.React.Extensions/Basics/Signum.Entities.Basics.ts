//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const CultureInfoEntity = new Type<CultureInfoEntity>("CultureInfo");
export interface CultureInfoEntity extends Entities.Entity {
    Type: "CultureInfo";
    name: string;
    nativeName: string | null;
    englishName: string | null;
}

export module CultureInfoOperation {
    export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Save");
}

export const DateSpanEntity = new Type<DateSpanEntity>("DateSpanEntity");
export interface DateSpanEntity extends Entities.EmbeddedEntity {
    Type: "DateSpanEntity";
    years?: number;
    months?: number;
    days?: number;
}

export const TimeSpanEntity = new Type<TimeSpanEntity>("TimeSpanEntity");
export interface TimeSpanEntity extends Entities.EmbeddedEntity {
    Type: "TimeSpanEntity";
    days?: number;
    hours?: number;
    minutes?: number;
    seconds?: number;
}

export const TypeConditionSymbol = new Type<TypeConditionSymbol>("TypeCondition");
export interface TypeConditionSymbol extends Entities.Symbol {
    Type: "TypeCondition";
}


