//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'

import { QueryToken } from '@framework/FindOptions'

export interface QueryTokenEmbedded {
    token?: QueryToken;
    parseException?: string;
}

export const EntityAction = new EnumType<EntityAction>("EntityAction");
export type EntityAction =
  "Identical" |
  "Different" |
  "New";

export interface IUserAssetEntity extends Entities.Entity {
  guid: string /*Guid*/;
}

export const LiteConflictEmbedded = new Type<LiteConflictEmbedded>("LiteConflictEmbedded");
export interface LiteConflictEmbedded extends Entities.EmbeddedEntity {
  Type: "LiteConflictEmbedded";
  propertyRoute: string;
  from: Entities.Lite<Entities.Entity>;
  to: Entities.Lite<Entities.Entity> | null;
}

export const QueryTokenEmbedded = new Type<QueryTokenEmbedded>("QueryTokenEmbedded");
export interface QueryTokenEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryTokenEmbedded";
  tokenString: string;
}

export module UserAssetMessage {
  export const ExportToXml = new MessageKey("UserAssetMessage", "ExportToXml");
  export const ImportUserAssets = new MessageKey("UserAssetMessage", "ImportUserAssets");
  export const ImportPreview = new MessageKey("UserAssetMessage", "ImportPreview");
  export const SelectTheXmlFileWithTheUserAssetsThatYouWantToImport = new MessageKey("UserAssetMessage", "SelectTheXmlFileWithTheUserAssetsThatYouWantToImport");
  export const SelectTheEntitiesToOverride = new MessageKey("UserAssetMessage", "SelectTheEntitiesToOverride");
  export const SucessfullyImported = new MessageKey("UserAssetMessage", "SucessfullyImported");
  export const SwitchToValue = new MessageKey("UserAssetMessage", "SwitchToValue");
  export const SwitchToExpression = new MessageKey("UserAssetMessage", "SwitchToExpression");
  export const LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase = new MessageKey("UserAssetMessage", "LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase");
  export const SameSelectionForAllConflictsOf0 = new MessageKey("UserAssetMessage", "SameSelectionForAllConflictsOf0");
}

export module UserAssetPermission {
  export const UserAssetsToXML : Authorization.PermissionSymbol = registerSymbol("Permission", "UserAssetPermission.UserAssetsToXML");
}

export const UserAssetPreviewLineEmbedded = new Type<UserAssetPreviewLineEmbedded>("UserAssetPreviewLineEmbedded");
export interface UserAssetPreviewLineEmbedded extends Entities.EmbeddedEntity {
  Type: "UserAssetPreviewLineEmbedded";
  type: Basics.TypeEntity | null;
  text: string;
  action: EntityAction;
  overrideEntity: boolean;
  guid: string /*Guid*/;
  customResolution: Entities.ModelEntity | null;
  liteConflicts: Entities.MList<LiteConflictEmbedded>;
}

export const UserAssetPreviewModel = new Type<UserAssetPreviewModel>("UserAssetPreviewModel");
export interface UserAssetPreviewModel extends Entities.ModelEntity {
  Type: "UserAssetPreviewModel";
  lines: Entities.MList<UserAssetPreviewLineEmbedded>;
}


