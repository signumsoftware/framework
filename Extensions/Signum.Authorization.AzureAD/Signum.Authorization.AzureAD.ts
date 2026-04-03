//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as BaseAD from '../Signum.Authorization/Signum.Authorization.BaseAD'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'
import * as Files from '../Signum.Files/Signum.Files'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export namespace AuthADFileType {
  export const CachedProfilePhoto : Files.FileTypeSymbol = registerSymbol("FileType", "AuthADFileType.CachedProfilePhoto");
}

export const AzureADConfigurationEmbedded: Type<AzureADConfigurationEmbedded> = new Type<AzureADConfigurationEmbedded>("AzureADConfigurationEmbedded");
export interface AzureADConfigurationEmbedded extends BaseAD.BaseADConfigurationEmbedded {
  enabled: boolean;
  type: AzureADType;
  applicationID: string /*Guid*/;
  directoryID: string /*Guid*/;
  tenantName: string | null;
  signInSignUp_UserFlow: string | null;
  signIn_UserFlow: string | null;
  signUp_UserFlow: string | null;
  editProfile_UserFlow: string | null;
  resetPassword_UserFlow: string | null;
  clientSecret: string | null;
  useDelegatedPermission: boolean;
}

export namespace AzureADQuery {
  export const ActiveDirectoryUsers: QueryKey = new QueryKey("AzureADQuery", "ActiveDirectoryUsers");
  export const ActiveDirectoryGroups: QueryKey = new QueryKey("AzureADQuery", "ActiveDirectoryGroups");
}

export namespace AzureADTask {
  export const DeactivateUsers : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "AzureADTask.DeactivateUsers");
}

export const AzureADType: EnumType<AzureADType> = new EnumType<AzureADType>("AzureADType");
export type AzureADType =
  "AzureAD" |
  "B2C" |
  "ExternalID";

export const CachedProfilePhotoEntity: Type<CachedProfilePhotoEntity> = new Type<CachedProfilePhotoEntity>("CachedProfilePhoto");
export interface CachedProfilePhotoEntity extends Entities.Entity {
  Type: "CachedProfilePhoto";
  user: Entities.Lite<Authorization.UserEntity>;
  size: number;
  photo: Files.FilePathEmbedded | null;
  invalidationDate: string /*DateTime*/;
  creationDate: string /*DateTime*/;
}

export namespace CachedProfilePhotoOperation {
  export const Save : Operations.ExecuteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Save");
  export const Delete : Operations.DeleteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Delete");
}

export const OnPremisesExtensionAttributesModel: Type<OnPremisesExtensionAttributesModel> = new Type<OnPremisesExtensionAttributesModel>("OnPremisesExtensionAttributesModel");
export interface OnPremisesExtensionAttributesModel extends Entities.ModelEntity {
  Type: "OnPremisesExtensionAttributesModel";
  extensionAttribute1: string | null;
  extensionAttribute2: string | null;
  extensionAttribute3: string | null;
  extensionAttribute4: string | null;
  extensionAttribute5: string | null;
  extensionAttribute6: string | null;
  extensionAttribute7: string | null;
  extensionAttribute8: string | null;
  extensionAttribute9: string | null;
  extensionAttribute10: string | null;
  extensionAttribute11: string | null;
  extensionAttribute12: string | null;
  extensionAttribute13: string | null;
  extensionAttribute14: string | null;
  extensionAttribute15: string | null;
}

export const UserAzureADMixin: Type<UserAzureADMixin> = new Type<UserAzureADMixin>("UserAzureADMixin");
export interface UserAzureADMixin extends Entities.MixinEntity {
  Type: "UserAzureADMixin";
  oID: string /*Guid*/ | null;
}

export namespace UserOIDMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet: MessageKey = new MessageKey("UserOIDMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
}

