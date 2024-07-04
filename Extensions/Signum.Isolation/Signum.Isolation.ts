//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'


export const IsolationEntity: Type<IsolationEntity> = new Type<IsolationEntity>("Isolation");
export interface IsolationEntity extends Entities.Entity {
  Type: "Isolation";
  name: string;
}

export module IsolationMessage {
  export const Entity0HasIsolation1ButCurrentIsolationIs2: MessageKey = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButCurrentIsolationIs2");
  export const SelectAnIsolation: MessageKey = new MessageKey("IsolationMessage", "SelectAnIsolation");
  export const Entity0HasIsolation1ButEntity2HasIsolation3: MessageKey = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButEntity2HasIsolation3");
  export const GlobalMode: MessageKey = new MessageKey("IsolationMessage", "GlobalMode");
  export const GlobalEntity: MessageKey = new MessageKey("IsolationMessage", "GlobalEntity");
}

export const IsolationMixin: Type<IsolationMixin> = new Type<IsolationMixin>("IsolationMixin");
export interface IsolationMixin extends Entities.MixinEntity {
  Type: "IsolationMixin";
  isolation: Entities.Lite<IsolationEntity> | null;
}

export module IsolationOperation {
  export const Save : Operations.ExecuteSymbol<IsolationEntity> = registerSymbol("Operation", "IsolationOperation.Save");
}

export const IsolationStrategy: EnumType<IsolationStrategy> = new EnumType<IsolationStrategy>("IsolationStrategy");
export type IsolationStrategy =
  "Isolated" |
  "Optional" |
  "None";

