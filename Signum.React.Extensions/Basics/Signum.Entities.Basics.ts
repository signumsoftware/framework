//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 
export const CultureInfoEntity_Type = new Type<CultureInfoEntity>("CultureInfo");
export interface CultureInfoEntity extends Entities.Entity {
    name?: string;
    nativeName?: string;
    englishName?: string;
}

export module CultureInfoOperation {
    export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = registerSymbol({ Type: "Operation", key: "CultureInfoOperation.Save" });
}

export const DateSpanEntity_Type = new Type<DateSpanEntity>("DateSpan");
export interface DateSpanEntity extends Entities.EmbeddedEntity {
    years?: number;
    months?: number;
    days?: number;
}

export const TypeConditionSymbol_Type = new Type<TypeConditionSymbol>("TypeCondition");
export interface TypeConditionSymbol extends Entities.Symbol {
}

