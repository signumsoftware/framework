//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicRenameEntity = new Type<DynamicRenameEntity>("DynamicRename");
export interface DynamicRenameEntity extends Entities.Entity {
  Type: "DynamicRename";
  creationDate: string /*DateTime*/;
  replacementKey: string;
  oldName: string;
  newName: string;
}

export const DynamicSqlMigrationEntity = new Type<DynamicSqlMigrationEntity>("DynamicSqlMigration");
export interface DynamicSqlMigrationEntity extends Entities.Entity {
  Type: "DynamicSqlMigration";
  creationDate: string /*DateTime*/;
  createdBy: Entities.Lite<Security.IUserEntity>;
  executionDate: string /*DateTime*/ | null;
  executedBy: Entities.Lite<Security.IUserEntity> | null;
  comment: string;
  script: string;
}

export module DynamicSqlMigrationMessage {
  export const TheMigrationIsAlreadyExecuted = new MessageKey("DynamicSqlMigrationMessage", "TheMigrationIsAlreadyExecuted");
  export const PreventingGenerationNewScriptBecauseOfErrorsInDynamicCodeFixErrorsAndRestartServer = new MessageKey("DynamicSqlMigrationMessage", "PreventingGenerationNewScriptBecauseOfErrorsInDynamicCodeFixErrorsAndRestartServer");
}

export module DynamicSqlMigrationOperation {
  export const Create : Operations.ConstructSymbol_Simple<DynamicSqlMigrationEntity> = registerSymbol("Operation", "DynamicSqlMigrationOperation.Create");
  export const Save : Operations.ExecuteSymbol<DynamicSqlMigrationEntity> = registerSymbol("Operation", "DynamicSqlMigrationOperation.Save");
  export const Execute : Operations.ExecuteSymbol<DynamicSqlMigrationEntity> = registerSymbol("Operation", "DynamicSqlMigrationOperation.Execute");
  export const Delete : Operations.DeleteSymbol<DynamicSqlMigrationEntity> = registerSymbol("Operation", "DynamicSqlMigrationOperation.Delete");
}

