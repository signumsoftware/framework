//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const CSharpMigrationEntity = new Type<CSharpMigrationEntity>("CSharpMigration");
export interface CSharpMigrationEntity extends Entities.Entity {
  Type: "CSharpMigration";
  uniqueName: string;
  executionDate: string;
}

export const LoadMethodLogEntity = new Type<LoadMethodLogEntity>("LoadMethodLog");
export interface LoadMethodLogEntity extends Entities.Entity {
  Type: "LoadMethodLog";
  methodName: string | null;
  className: string | null;
  description: string | null;
  start: string;
  end: string | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const SqlMigrationEntity = new Type<SqlMigrationEntity>("SqlMigration");
export interface SqlMigrationEntity extends Entities.Entity {
  Type: "SqlMigration";
  versionNumber: string;
  comment: string | null;
}


