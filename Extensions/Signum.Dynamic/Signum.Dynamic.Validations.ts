//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Eval from '../Signum.Eval/Signum.Eval'

interface IDynamicValidationEvaluator {}

export const DynamicValidationEntity: Type<DynamicValidationEntity> = new Type<DynamicValidationEntity>("DynamicValidation");
export interface DynamicValidationEntity extends Entities.Entity {
  Type: "DynamicValidation";
  name: string;
  entityType: Basics.TypeEntity;
  subEntity: Basics.PropertyRouteEntity | null;
  eval: DynamicValidationEval;
}

export const DynamicValidationEval: Type<DynamicValidationEval> = new Type<DynamicValidationEval>("DynamicValidationEval");
export interface DynamicValidationEval extends Eval.EvalEmbedded<IDynamicValidationEvaluator> {
  Type: "DynamicValidationEval";
}

export module DynamicValidationMessage {
  export const PropertyIs: MessageKey = new MessageKey("DynamicValidationMessage", "PropertyIs");
}

export module DynamicValidationOperation {
  export const Clone : Operations.ConstructSymbol_From<DynamicValidationEntity, DynamicValidationEntity> = registerSymbol("Operation", "DynamicValidationOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicValidationEntity> = registerSymbol("Operation", "DynamicValidationOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicValidationEntity> = registerSymbol("Operation", "DynamicValidationOperation.Delete");
}

