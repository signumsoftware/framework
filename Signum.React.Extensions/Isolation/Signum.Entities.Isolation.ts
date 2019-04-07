//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const IsolationEntity = new Type<IsolationEntity>("Isolation");
export interface IsolationEntity extends Entities.Entity {
  Type: "Isolation";
  name: string;
}

export module IsolationMessage {
  export const Entity0HasIsolation1ButCurrentIsolationIs2 = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButCurrentIsolationIs2");
  export const SelectAnIsolation = new MessageKey("IsolationMessage", "SelectAnIsolation");
  export const Entity0HasIsolation1ButEntity2HasIsolation3 = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButEntity2HasIsolation3");
}

export const IsolationMixin = new Type<IsolationMixin>("IsolationMixin");
export interface IsolationMixin extends Entities.MixinEntity {
  Type: "IsolationMixin";
  isolation: Entities.Lite<IsolationEntity> | null;
}

export module IsolationOperation {
  export const Save : Entities.ExecuteSymbol<IsolationEntity> = registerSymbol("Operation", "IsolationOperation.Save");
}


