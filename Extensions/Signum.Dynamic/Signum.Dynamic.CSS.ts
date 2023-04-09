//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicCSSOverrideEntity = new Type<DynamicCSSOverrideEntity>("DynamicCSSOverride");
export interface DynamicCSSOverrideEntity extends Entities.Entity {
  Type: "DynamicCSSOverride";
  name: string;
  script: string;
}

export module DynamicCSSOverrideOperation {
  export const Save : Operations.ExecuteSymbol<DynamicCSSOverrideEntity> = registerSymbol("Operation", "DynamicCSSOverrideOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicCSSOverrideEntity> = registerSymbol("Operation", "DynamicCSSOverrideOperation.Delete");
}

