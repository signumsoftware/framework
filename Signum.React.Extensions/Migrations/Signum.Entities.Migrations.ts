//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

export const CSharpMigrationEntity_Type = new Type<CSharpMigrationEntity>("CSharpMigration");
export interface CSharpMigrationEntity extends Entities.Entity {
    uniqueName: string;
    executionDate: string;
}

export const SqlMigrationEntity_Type = new Type<SqlMigrationEntity>("SqlMigration");
export interface SqlMigrationEntity extends Entities.Entity {
    versionNumber: string;
}

