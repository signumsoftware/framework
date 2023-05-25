//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicExpressionEntity = new Type<DynamicExpressionEntity>("DynamicExpression");
export interface DynamicExpressionEntity extends Entities.Entity {
  Type: "DynamicExpression";
  name: string;
  fromType: string;
  returnType: string;
  body: string;
  format: string | null;
  unit: string | null;
  translation: DynamicExpressionTranslation;
}

export module DynamicExpressionOperation {
  export const Clone : Operations.ConstructSymbol_From<DynamicExpressionEntity, DynamicExpressionEntity> = registerSymbol("Operation", "DynamicExpressionOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicExpressionEntity> = registerSymbol("Operation", "DynamicExpressionOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicExpressionEntity> = registerSymbol("Operation", "DynamicExpressionOperation.Delete");
}

export const DynamicExpressionTranslation = new EnumType<DynamicExpressionTranslation>("DynamicExpressionTranslation");
export type DynamicExpressionTranslation =
  "TranslateExpressionName" |
  "ReuseTranslationOfReturnType" |
  "NoTranslation";

