//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'
import * as Files from '../Signum.Files/Signum.Files'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export namespace ActiveDirectoryAuthorizerMessage {
  export const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication: MessageKey = new MessageKey("ActiveDirectoryAuthorizerMessage", "ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication");
}

export const ActiveDirectoryConfigurationEmbedded: Type<ActiveDirectoryConfigurationEmbedded> = new Type<ActiveDirectoryConfigurationEmbedded>("ActiveDirectoryConfigurationEmbedded");
export interface ActiveDirectoryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ActiveDirectoryConfigurationEmbedded";
  windowsAD: WindowsActiveDirectoryEmbedded | null;
  azureAD: AzureActiveDirectoryEmbedded | null;
  allowMatchUsersBySimpleUserName: boolean;
  autoCreateUsers: boolean;
  autoUpdateUsers: boolean;
  roleMapping: Entities.MList<RoleMappingEmbedded>;
  defaultRole: Entities.Lite<Authorization.RoleEntity> | null;
}

export namespace ActiveDirectoryMessage {
  export const Id: MessageKey = new MessageKey("ActiveDirectoryMessage", "Id");
  export const DisplayName: MessageKey = new MessageKey("ActiveDirectoryMessage", "DisplayName");
  export const Mail: MessageKey = new MessageKey("ActiveDirectoryMessage", "Mail");
  export const GivenName: MessageKey = new MessageKey("ActiveDirectoryMessage", "GivenName");
  export const Surname: MessageKey = new MessageKey("ActiveDirectoryMessage", "Surname");
  export const JobTitle: MessageKey = new MessageKey("ActiveDirectoryMessage", "JobTitle");
  export const OnPremisesImmutableId: MessageKey = new MessageKey("ActiveDirectoryMessage", "OnPremisesImmutableId");
  export const CompanyName: MessageKey = new MessageKey("ActiveDirectoryMessage", "CompanyName");
  export const AccountEnabled: MessageKey = new MessageKey("ActiveDirectoryMessage", "AccountEnabled");
  export const OnPremisesExtensionAttributes: MessageKey = new MessageKey("ActiveDirectoryMessage", "OnPremisesExtensionAttributes");
  export const OnlyActiveUsers: MessageKey = new MessageKey("ActiveDirectoryMessage", "OnlyActiveUsers");
  export const InGroup: MessageKey = new MessageKey("ActiveDirectoryMessage", "InGroup");
  export const Description: MessageKey = new MessageKey("ActiveDirectoryMessage", "Description");
  export const SecurityEnabled: MessageKey = new MessageKey("ActiveDirectoryMessage", "SecurityEnabled");
  export const Visibility: MessageKey = new MessageKey("ActiveDirectoryMessage", "Visibility");
  export const HasUser: MessageKey = new MessageKey("ActiveDirectoryMessage", "HasUser");
}

export namespace ActiveDirectoryPermission {
  export const InviteUsersFromAD : Basics.PermissionSymbol = registerSymbol("Permission", "ActiveDirectoryPermission.InviteUsersFromAD");
}

export namespace ActiveDirectoryTask {
  export const DeactivateUsers : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "ActiveDirectoryTask.DeactivateUsers");
}

export const ADGroupEntity: Type<ADGroupEntity> = new Type<ADGroupEntity>("ADGroup");
export interface ADGroupEntity extends Entities.Entity {
  Type: "ADGroup";
  displayName: string;
}

export namespace ADGroupOperation {
  export const Save : Operations.ExecuteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Save");
  export const Delete : Operations.DeleteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Delete");
}

export namespace AuthADFileType {
  export const CachedProfilePhoto : Files.FileTypeSymbol = registerSymbol("FileType", "AuthADFileType.CachedProfilePhoto");
}

export const AzureActiveDirectoryEmbedded: Type<AzureActiveDirectoryEmbedded> = new Type<AzureActiveDirectoryEmbedded>("AzureActiveDirectoryEmbedded");
export interface AzureActiveDirectoryEmbedded extends Entities.EmbeddedEntity {
  Type: "AzureActiveDirectoryEmbedded";
  loginWithAzureAD: boolean;
  applicationID: string /*Guid*/;
  directoryID: string /*Guid*/;
  azureB2C: AzureB2CEmbedded | null;
  clientSecret: string | null;
  useDelegatedPermission: boolean;
}

export const AzureB2CEmbedded: Type<AzureB2CEmbedded> = new Type<AzureB2CEmbedded>("AzureB2CEmbedded");
export interface AzureB2CEmbedded extends Entities.EmbeddedEntity {
  Type: "AzureB2CEmbedded";
  loginWithAzureB2C: boolean;
  tenantName: string;
  signInSignUp_UserFlow: string;
  resetPassword_UserFlow: string | null;
}

export const CachedProfilePhotoEntity: Type<CachedProfilePhotoEntity> = new Type<CachedProfilePhotoEntity>("CachedProfilePhoto");
export interface CachedProfilePhotoEntity extends Entities.Entity {
  Type: "CachedProfilePhoto";
  user: Entities.Lite<Authorization.UserEntity>;
  size: number;
  photo: Files.FilePathEmbedded | null;
  creationDate: string /*DateTime*/;
}

export namespace CachedProfilePhotoOperation {
  export const Save : Operations.ExecuteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Save");
  export const Delete : Operations.DeleteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Delete");
}

export const RoleMappingEmbedded: Type<RoleMappingEmbedded> = new Type<RoleMappingEmbedded>("RoleMappingEmbedded");
export interface RoleMappingEmbedded extends Entities.EmbeddedEntity {
  Type: "RoleMappingEmbedded";
  aDNameOrGuid: string;
  role: Entities.Lite<Authorization.RoleEntity>;
}

export namespace UserADMessage {
  export const Find0InActiveDirectory: MessageKey = new MessageKey("UserADMessage", "Find0InActiveDirectory");
  export const FindInActiveDirectory: MessageKey = new MessageKey("UserADMessage", "FindInActiveDirectory");
  export const NoUserContaining0FoundInActiveDirectory: MessageKey = new MessageKey("UserADMessage", "NoUserContaining0FoundInActiveDirectory");
  export const SelectActiveDirectoryUser: MessageKey = new MessageKey("UserADMessage", "SelectActiveDirectoryUser");
  export const PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport: MessageKey = new MessageKey("UserADMessage", "PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport");
  export const NameOrEmail: MessageKey = new MessageKey("UserADMessage", "NameOrEmail");
}

export const UserADMixin: Type<UserADMixin> = new Type<UserADMixin>("UserADMixin");
export interface UserADMixin extends Entities.MixinEntity {
  Type: "UserADMixin";
  oID: string /*Guid*/ | null;
  sID: string | null;
}

export namespace UserOIDMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet: MessageKey = new MessageKey("UserOIDMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
}

export const WindowsActiveDirectoryEmbedded: Type<WindowsActiveDirectoryEmbedded> = new Type<WindowsActiveDirectoryEmbedded>("WindowsActiveDirectoryEmbedded");
export interface WindowsActiveDirectoryEmbedded extends Entities.EmbeddedEntity {
  Type: "WindowsActiveDirectoryEmbedded";
  loginWithWindowsAuthenticator: boolean;
  loginWithActiveDirectoryRegistry: boolean;
  domainName: string | null;
  directoryRegistry_Username: string | null;
  directoryRegistry_Password: string | null;
}

