//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'
import * as Basics from './Signum.Basics'
import * as Security from './Signum.Security'


export const OperationLogEntity = new Type<OperationLogEntity>("OperationLog");
export interface OperationLogEntity extends Entities.Entity {
  Type: "OperationLog";
  target: Entities.Lite<Entities.Entity> | null;
  origin: Entities.Lite<Entities.Entity> | null;
  operation: OperationSymbol;
  user: Entities.Lite<Security.IUserEntity>;
  start: string /*DateTime*/;
  end: string /*DateTime*/ | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const OperationSymbol = new Type<OperationSymbol>("Operation");
export interface OperationSymbol extends Basics.Symbol {
  Type: "Operation";
}

export const OperationType = new EnumType<OperationType>("OperationType");
export type OperationType =
  "Execute" |
  "Delete" |
  "Constructor" |
  "ConstructorFrom" |
  "ConstructorFromMany";

export const PropertyOperation = new EnumType<PropertyOperation>("PropertyOperation");
export type PropertyOperation =
  "Set" |
  "AddElement" |
  "AddNewElement" |
  "ChangeElements" |
  "RemoveElement" |
  "RemoveElementsWhere" |
  "ModifyEntity" |
  "CreateNewEntity";

