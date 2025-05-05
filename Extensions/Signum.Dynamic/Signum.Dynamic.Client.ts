//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicClientEntity: Type<DynamicClientEntity> = new Type<DynamicClientEntity>("DynamicClient");
export interface DynamicClientEntity extends Entities.Entity {
  Type: "DynamicClient";
  name: string;
  code: string;
}

export module DynamicClientOperation {
  export const Clone : Operations.ConstructSymbol_From<DynamicClientEntity, DynamicClientEntity> = registerSymbol("Operation", "DynamicClientOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicClientEntity> = registerSymbol("Operation", "DynamicClientOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicClientEntity> = registerSymbol("Operation", "DynamicClientOperation.Delete");
}

