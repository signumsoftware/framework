//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'


export const ADGroupEntity: Type<ADGroupEntity> = new Type<ADGroupEntity>("ADGroup");
export interface ADGroupEntity extends Entities.Entity {
  Type: "ADGroup";
  displayName: string;
}

export namespace ADGroupOperation {
  export const Save : Operations.ExecuteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Save");
  export const Delete : Operations.DeleteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Delete");
}

