//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export const EntityAction: EnumType<EntityAction> = new EnumType<EntityAction>("EntityAction");
export type EntityAction =
  "Identical" |
  "Different" |
  "New";

export interface IUserAssetEntity extends Entities.Entity {
  guid: string /*Guid*/;
}

export const LiteConflictEmbedded: Type<LiteConflictEmbedded> = new Type<LiteConflictEmbedded>("LiteConflictEmbedded");
export interface LiteConflictEmbedded extends Entities.EmbeddedEntity {
  Type: "LiteConflictEmbedded";
  propertyRoute: string;
  from: Entities.Lite<Entities.Entity>;
  to: Entities.Lite<Entities.Entity> | null;
}

export namespace UserAssetMessage {
  export const ExportToXml: MessageKey = new MessageKey("UserAssetMessage", "ExportToXml");
  export const ImportUserAssets: MessageKey = new MessageKey("UserAssetMessage", "ImportUserAssets");
  export const ImportPreview: MessageKey = new MessageKey("UserAssetMessage", "ImportPreview");
  export const SelectTheXmlFileWithTheUserAssetsThatYouWantToImport: MessageKey = new MessageKey("UserAssetMessage", "SelectTheXmlFileWithTheUserAssetsThatYouWantToImport");
  export const SelectTheEntitiesToOverride: MessageKey = new MessageKey("UserAssetMessage", "SelectTheEntitiesToOverride");
  export const SucessfullyImported: MessageKey = new MessageKey("UserAssetMessage", "SucessfullyImported");
  export const LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase: MessageKey = new MessageKey("UserAssetMessage", "LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase");
  export const SameSelectionForAllConflictsOf0: MessageKey = new MessageKey("UserAssetMessage", "SameSelectionForAllConflictsOf0");
  export const _0IsNotFilterable: MessageKey = new MessageKey("UserAssetMessage", "_0IsNotFilterable");
  export const TheFilterOperation0isNotCompatibleWith1: MessageKey = new MessageKey("UserAssetMessage", "TheFilterOperation0isNotCompatibleWith1");
  export const UserAssetLines: MessageKey = new MessageKey("UserAssetMessage", "UserAssetLines");
  export const Import: MessageKey = new MessageKey("UserAssetMessage", "Import");
  export const AssumeIs: MessageKey = new MessageKey("UserAssetMessage", "AssumeIs");
  export const UsedBy: MessageKey = new MessageKey("UserAssetMessage", "UsedBy");
  export const Advanced: MessageKey = new MessageKey("UserAssetMessage", "Advanced");
}

export namespace UserAssetPermission {
  export const UserAssetsToXML : Basics.PermissionSymbol = registerSymbol("Permission", "UserAssetPermission.UserAssetsToXML");
}

export const UserAssetPreviewLineEmbedded: Type<UserAssetPreviewLineEmbedded> = new Type<UserAssetPreviewLineEmbedded>("UserAssetPreviewLineEmbedded");
export interface UserAssetPreviewLineEmbedded extends Entities.EmbeddedEntity {
  Type: "UserAssetPreviewLineEmbedded";
  type: Basics.TypeEntity | null;
  text: string;
  entityType: Basics.TypeEntity | null;
  action: EntityAction;
  overrideEntity: boolean;
  guid: string /*Guid*/;
  customResolution: Entities.ModelEntity | null;
  liteConflicts: Entities.MList<LiteConflictEmbedded>;
}

export const UserAssetPreviewModel: Type<UserAssetPreviewModel> = new Type<UserAssetPreviewModel>("UserAssetPreviewModel");
export interface UserAssetPreviewModel extends Entities.ModelEntity {
  Type: "UserAssetPreviewModel";
  lines: Entities.MList<UserAssetPreviewLineEmbedded>;
}

