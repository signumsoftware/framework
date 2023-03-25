//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Basics from '../../Signum.React/Signum.Basics'
import * as Operations from '../../Signum.React/Signum.Operations'


export const DynamicMixinConnectionEntity = new Type<DynamicMixinConnectionEntity>("DynamicMixinConnection");
export interface DynamicMixinConnectionEntity extends Entities.Entity {
  Type: "DynamicMixinConnection";
  entityType: Entities.Lite<Basics.TypeEntity>;
  mixinName: string;
}

export module DynamicMixinConnectionOperation {
  export const Save : Operations.ExecuteSymbol<DynamicMixinConnectionEntity> = registerSymbol("Operation", "DynamicMixinConnectionOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicMixinConnectionEntity> = registerSymbol("Operation", "DynamicMixinConnectionOperation.Delete");
}

