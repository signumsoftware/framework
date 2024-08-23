//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'



export const CSharpMigrationEntity: Type<CSharpMigrationEntity> = new Type<CSharpMigrationEntity>("CSharpMigration");
export interface CSharpMigrationEntity extends Entities.Entity {
  Type: "CSharpMigration";
  uniqueName: string;
  executionDate: string /*DateTime*/;
}

export const LoadMethodLogEntity: Type<LoadMethodLogEntity> = new Type<LoadMethodLogEntity>("LoadMethodLog");
export interface LoadMethodLogEntity extends Entities.Entity {
  Type: "LoadMethodLog";
  methodName: string | null;
  className: string | null;
  description: string | null;
  start: string /*DateTime*/;
  end: string /*DateTime*/ | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const SqlMigrationEntity: Type<SqlMigrationEntity> = new Type<SqlMigrationEntity>("SqlMigration");
export interface SqlMigrationEntity extends Entities.Entity {
  Type: "SqlMigration";
  versionNumber: string;
  comment: string | null;
}

