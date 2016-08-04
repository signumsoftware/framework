//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const CSharpMigrationEntity = new Type<CSharpMigrationEntity>("CSharpMigration");
export interface CSharpMigrationEntity extends Entities.Entity {
    Type: "CSharpMigration";
    uniqueName?: string | null;
    executionDate?: string;
}

export const SqlMigrationEntity = new Type<SqlMigrationEntity>("SqlMigration");
export interface SqlMigrationEntity extends Entities.Entity {
    Type: "SqlMigration";
    versionNumber?: string | null;
}


