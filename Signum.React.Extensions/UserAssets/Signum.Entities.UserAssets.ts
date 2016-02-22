//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Authorization from '../Authorization/Signum.Entities.Authorization' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 


export enum EntityAction {
    Identical = "Identical" as any,
    Different = "Different" as any,
    New = "New" as any,
}
export const EntityAction_Type = new EnumType<EntityAction>("EntityAction", EntityAction);

export interface IUserAssetEntity extends Entities.IEntity {
    guid?: string;
}

export const QueryTokenEntity_Type = new Type<QueryTokenEntity>("QueryTokenEntity");
export interface QueryTokenEntity extends Entities.EmbeddedEntity {
    tokenString?: string;
}

export module UserAssetMessage {
    export const ExportToXml = new MessageKey("UserAssetMessage", "ExportToXml");
    export const ImportUserAssets = new MessageKey("UserAssetMessage", "ImportUserAssets");
    export const ImportPreview = new MessageKey("UserAssetMessage", "ImportPreview");
    export const SelectTheEntitiesToOverride = new MessageKey("UserAssetMessage", "SelectTheEntitiesToOverride");
    export const SucessfullyImported = new MessageKey("UserAssetMessage", "SucessfullyImported");
}

export module UserAssetPermission {
    export const UserAssetsToXML : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "UserAssetPermission.UserAssetsToXML" });
}

export const UserAssetPreviewLine_Type = new Type<UserAssetPreviewLine>("UserAssetPreviewLine");
export interface UserAssetPreviewLine extends Entities.EmbeddedEntity {
    type?: Basics.TypeEntity;
    text?: string;
    action?: EntityAction;
    overrideEntity?: boolean;
    guid?: string;
}

export const UserAssetPreviewModel_Type = new Type<UserAssetPreviewModel>("UserAssetPreviewModel");
export interface UserAssetPreviewModel extends Entities.ModelEntity {
    lines?: Entities.MList<UserAssetPreviewLine>;
}

