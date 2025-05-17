//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'
import * as Basics from './Signum.Basics'
import * as Security from './Signum.Security'


//The interfaces add no real members, they are there just to force TS structural typing

export interface ExecuteSymbol<T extends Entities.Entity> extends OperationSymbol { _execute_: T /*TRICK*/ };
export interface DeleteSymbol<T extends Entities.Entity> extends OperationSymbol { _delete_: T /*TRICK*/ };
export interface ConstructSymbol_Simple<T extends Entities.Entity> extends OperationSymbol { _construct_: T /*TRICK*/ };
export interface ConstructSymbol_From<T extends Entities.Entity, F extends Entities.Entity> extends OperationSymbol { _constructFrom_: T, _from_?: F /*TRICK*/ };
export interface ConstructSymbol_FromMany<T extends Entities.Entity, F extends Entities.Entity> extends OperationSymbol { _constructFromMany_: T, _from_?: F /*TRICK*/ };

export const OperationLogEntity: Type<OperationLogEntity> = new Type<OperationLogEntity>("OperationLog");
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

export const OperationSymbol: Type<OperationSymbol> = new Type<OperationSymbol>("Operation");
export interface OperationSymbol extends Basics.Symbol {
  Type: "Operation";
}

export const OperationType: EnumType<OperationType> = new EnumType<OperationType>("OperationType");
export type OperationType =
  "Execute" |
  "Delete" |
  "Constructor" |
  "ConstructorFrom" |
  "ConstructorFromMany";

export const PropertyOperation: EnumType<PropertyOperation> = new EnumType<PropertyOperation>("PropertyOperation");
export type PropertyOperation =
  "Set" |
  "AddElement" |
  "AddNewElement" |
  "ChangeElements" |
  "RemoveElement" |
  "RemoveElementsWhere" |
  "ModifyEntity" |
  "CreateNewEntity";

