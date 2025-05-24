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


export module ActiveDirectoryAuthorizerMessage {
  export const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication = new MessageKey("ActiveDirectoryAuthorizerMessage", "ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication");
}

export const ActiveDirectoryConfigurationEmbedded = new Type<ActiveDirectoryConfigurationEmbedded>("ActiveDirectoryConfigurationEmbedded");
export interface ActiveDirectoryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ActiveDirectoryConfigurationEmbedded";
  domainName: string | null;
  directoryRegistry_Username: string | null;
  directoryRegistry_Password: string | null;
  azure_ApplicationID: string /*Guid*/ | null;
  azure_DirectoryID: string /*Guid*/ | null;
  azureB2C: AzureB2CEmbedded | null;
  azure_ClientSecret: string | null;
  useDelegatedPermission: boolean;
  loginWithWindowsAuthenticator: boolean;
  loginWithActiveDirectoryRegistry: boolean;
  loginWithAzureAD: boolean;
  allowMatchUsersBySimpleUserName: boolean;
  autoCreateUsers: boolean;
  autoUpdateUsers: boolean;
  roleMapping: Entities.MList<RoleMappingEmbedded>;
  defaultRole: Entities.Lite<Authorization.RoleEntity> | null;
}

export module ActiveDirectoryMessage {
  export const Id = new MessageKey("ActiveDirectoryMessage", "Id");
  export const DisplayName = new MessageKey("ActiveDirectoryMessage", "DisplayName");
  export const Mail = new MessageKey("ActiveDirectoryMessage", "Mail");
  export const GivenName = new MessageKey("ActiveDirectoryMessage", "GivenName");
  export const Surname = new MessageKey("ActiveDirectoryMessage", "Surname");
  export const JobTitle = new MessageKey("ActiveDirectoryMessage", "JobTitle");
  export const OnPremisesImmutableId = new MessageKey("ActiveDirectoryMessage", "OnPremisesImmutableId");
  export const CompanyName = new MessageKey("ActiveDirectoryMessage", "CompanyName");
  export const AccountEnabled = new MessageKey("ActiveDirectoryMessage", "AccountEnabled");
  export const OnPremisesExtensionAttributes = new MessageKey("ActiveDirectoryMessage", "OnPremisesExtensionAttributes");
  export const OnlyActiveUsers = new MessageKey("ActiveDirectoryMessage", "OnlyActiveUsers");
  export const InGroup = new MessageKey("ActiveDirectoryMessage", "InGroup");
  export const Description = new MessageKey("ActiveDirectoryMessage", "Description");
  export const SecurityEnabled = new MessageKey("ActiveDirectoryMessage", "SecurityEnabled");
  export const Visibility = new MessageKey("ActiveDirectoryMessage", "Visibility");
  export const HasUser = new MessageKey("ActiveDirectoryMessage", "HasUser");
}

export module ActiveDirectoryPermission {
  export const InviteUsersFromAD : Basics.PermissionSymbol = registerSymbol("Permission", "ActiveDirectoryPermission.InviteUsersFromAD");
}

export module ActiveDirectoryTask {
  export const DeactivateUsers : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "ActiveDirectoryTask.DeactivateUsers");
}

export const ADGroupEntity = new Type<ADGroupEntity>("ADGroup");
export interface ADGroupEntity extends Entities.Entity {
  Type: "ADGroup";
  displayName: string;
}

export module ADGroupOperation {
  export const Save : Operations.ExecuteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Save");
  export const Delete : Operations.DeleteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Delete");
}

export module AuthADFileType {
  export const CachedProfilePhoto : Files.FileTypeSymbol = registerSymbol("FileType", "AuthADFileType.CachedProfilePhoto");
}

export const AzureB2CEmbedded = new Type<AzureB2CEmbedded>("AzureB2CEmbedded");
export interface AzureB2CEmbedded extends Entities.EmbeddedEntity {
  Type: "AzureB2CEmbedded";
  tenantName: string;
  signInSignUp_UserFlow: string;
}

export const CachedProfilePhotoEntity = new Type<CachedProfilePhotoEntity>("CachedProfilePhoto");
export interface CachedProfilePhotoEntity extends Entities.Entity {
  Type: "CachedProfilePhoto";
  user: Entities.Lite<Authorization.UserEntity>;
  size: number;
  photo: Files.FilePathEmbedded | null;
  creationDate: string /*DateTime*/;
}

export module CachedProfilePhotoOperation {
  export const Save : Operations.ExecuteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Save");
  export const Delete : Operations.DeleteSymbol<CachedProfilePhotoEntity> = registerSymbol("Operation", "CachedProfilePhotoOperation.Delete");
}

export const RoleMappingEmbedded = new Type<RoleMappingEmbedded>("RoleMappingEmbedded");
export interface RoleMappingEmbedded extends Entities.EmbeddedEntity {
  Type: "RoleMappingEmbedded";
  aDNameOrGuid: string;
  role: Entities.Lite<Authorization.RoleEntity>;
}

export module UserADMessage {
  export const Find0InActiveDirectory = new MessageKey("UserADMessage", "Find0InActiveDirectory");
  export const FindInActiveDirectory = new MessageKey("UserADMessage", "FindInActiveDirectory");
  export const NoUserContaining0FoundInActiveDirectory = new MessageKey("UserADMessage", "NoUserContaining0FoundInActiveDirectory");
  export const SelectActiveDirectoryUser = new MessageKey("UserADMessage", "SelectActiveDirectoryUser");
  export const PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport = new MessageKey("UserADMessage", "PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport");
  export const NameOrEmail = new MessageKey("UserADMessage", "NameOrEmail");
}

export const UserADMixin = new Type<UserADMixin>("UserADMixin");
export interface UserADMixin extends Entities.MixinEntity {
  Type: "UserADMixin";
  oID: string /*Guid*/ | null;
  sID: string | null;
}

export module UserOIDMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet = new MessageKey("UserOIDMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
}

