//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


import { QueryToken } from '../../../Framework/Signum.React/Scripts/FindOptions' 

export interface QueryTokenEntity {
    token?: QueryToken;
    parseException?: string;
}



export const EntityAction = new EnumType<EntityAction>("EntityAction");
export type EntityAction =
    "Identical" |
    "Different" |
    "New";

export interface IUserAssetEntity extends Entities.Entity {
    guid?: string;
}

export const QueryTokenEntity = new Type<QueryTokenEntity>("QueryTokenEntity");
export interface QueryTokenEntity extends Entities.EmbeddedEntity {
    Type: "QueryTokenEntity";
    tokenString: string;
}

export module UserAssetMessage {
    export const ExportToXml = new MessageKey("UserAssetMessage", "ExportToXml");
    export const ImportUserAssets = new MessageKey("UserAssetMessage", "ImportUserAssets");
    export const ImportPreview = new MessageKey("UserAssetMessage", "ImportPreview");
    export const SelectTheXmlFileWithTheUserAssetsThatYouWantToImport = new MessageKey("UserAssetMessage", "SelectTheXmlFileWithTheUserAssetsThatYouWantToImport");
    export const SelectTheEntitiesToOverride = new MessageKey("UserAssetMessage", "SelectTheEntitiesToOverride");
    export const SucessfullyImported = new MessageKey("UserAssetMessage", "SucessfullyImported");
}

export module UserAssetPermission {
    export const UserAssetsToXML : Authorization.PermissionSymbol = registerSymbol("Permission", "UserAssetPermission.UserAssetsToXML");
}

export const UserAssetPreviewLine = new Type<UserAssetPreviewLine>("UserAssetPreviewLine");
export interface UserAssetPreviewLine extends Entities.EmbeddedEntity {
    Type: "UserAssetPreviewLine";
    type?: Basics.TypeEntity | null;
    text?: string | null;
    action?: EntityAction;
    overrideEntity?: boolean;
    guid?: string;
}

export const UserAssetPreviewModel = new Type<UserAssetPreviewModel>("UserAssetPreviewModel");
export interface UserAssetPreviewModel extends Entities.ModelEntity {
    Type: "UserAssetPreviewModel";
    lines: Entities.MList<UserAssetPreviewLine>;
}


