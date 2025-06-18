//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Eval from '../Signum.Eval/Signum.Eval'

interface IDynamicApiEvaluator {}

export const DynamicApiEntity: Type<DynamicApiEntity> = new Type<DynamicApiEntity>("DynamicApi");
export interface DynamicApiEntity extends Entities.Entity {
  Type: "DynamicApi";
  name: string;
  eval: DynamicApiEval;
}

export const DynamicApiEval: Type<DynamicApiEval> = new Type<DynamicApiEval>("DynamicApiEval");
export interface DynamicApiEval extends Eval.EvalEmbedded<IDynamicApiEvaluator> {
  Type: "DynamicApiEval";
}

export namespace DynamicApiOperation {
  export const Clone : Operations.ConstructSymbol_From<DynamicApiEntity, DynamicApiEntity> = registerSymbol("Operation", "DynamicApiOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicApiEntity> = registerSymbol("Operation", "DynamicApiOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicApiEntity> = registerSymbol("Operation", "DynamicApiOperation.Delete");
}

