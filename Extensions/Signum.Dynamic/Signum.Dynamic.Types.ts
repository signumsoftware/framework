//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Eval from '../Signum.Eval/Signum.Eval'

interface IDynamicTypeConditionEvaluator {}

export const DynamicBaseType = new EnumType<DynamicBaseType>("DynamicBaseType");
export type DynamicBaseType =
  "Entity" |
  "MixinEntity" |
  "EmbeddedEntity" |
  "ModelEntity";

export const DynamicTypeConditionEntity = new Type<DynamicTypeConditionEntity>("DynamicTypeCondition");
export interface DynamicTypeConditionEntity extends Entities.Entity {
  Type: "DynamicTypeCondition";
  symbolName: DynamicTypeConditionSymbolEntity;
  entityType: Basics.TypeEntity;
  eval: DynamicTypeConditionEval;
}

export const DynamicTypeConditionEval = new Type<DynamicTypeConditionEval>("DynamicTypeConditionEval");
export interface DynamicTypeConditionEval extends Eval.EvalEmbedded<IDynamicTypeConditionEvaluator> {
  Type: "DynamicTypeConditionEval";
}

export module DynamicTypeConditionOperation {
  export const Clone : Operations.ConstructSymbol_From<DynamicTypeConditionEntity, DynamicTypeConditionEntity> = registerSymbol("Operation", "DynamicTypeConditionOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicTypeConditionEntity> = registerSymbol("Operation", "DynamicTypeConditionOperation.Save");
}

export const DynamicTypeConditionSymbolEntity = new Type<DynamicTypeConditionSymbolEntity>("DynamicTypeConditionSymbol");
export interface DynamicTypeConditionSymbolEntity extends Entities.Entity {
  Type: "DynamicTypeConditionSymbol";
  name: string;
}

export module DynamicTypeConditionSymbolOperation {
  export const Save : Operations.ExecuteSymbol<DynamicTypeConditionSymbolEntity> = registerSymbol("Operation", "DynamicTypeConditionSymbolOperation.Save");
}

export const DynamicTypeEntity = new Type<DynamicTypeEntity>("DynamicType");
export interface DynamicTypeEntity extends Entities.Entity {
  Type: "DynamicType";
  baseType: DynamicBaseType;
  typeName: string;
  typeDefinition: string;
}

export module DynamicTypeMessage {
  export const TypeSaved = new MessageKey("DynamicTypeMessage", "TypeSaved");
  export const DynamicType0SucessfullySavedGoToDynamicPanelNow = new MessageKey("DynamicTypeMessage", "DynamicType0SucessfullySavedGoToDynamicPanelNow");
  export const ServerRestartedWithErrorsInDynamicCodeFixErrorsAndRestartAgain = new MessageKey("DynamicTypeMessage", "ServerRestartedWithErrorsInDynamicCodeFixErrorsAndRestartAgain");
  export const RemoveSaveOperation = new MessageKey("DynamicTypeMessage", "RemoveSaveOperation");
  export const TheEntityShouldBeSynchronizedToApplyMixins = new MessageKey("DynamicTypeMessage", "TheEntityShouldBeSynchronizedToApplyMixins");
}

export module DynamicTypeOperation {
  export const Create : Operations.ConstructSymbol_Simple<DynamicTypeEntity> = registerSymbol("Operation", "DynamicTypeOperation.Create");
  export const Clone : Operations.ConstructSymbol_From<DynamicTypeEntity, DynamicTypeEntity> = registerSymbol("Operation", "DynamicTypeOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicTypeEntity> = registerSymbol("Operation", "DynamicTypeOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicTypeEntity> = registerSymbol("Operation", "DynamicTypeOperation.Delete");
}

