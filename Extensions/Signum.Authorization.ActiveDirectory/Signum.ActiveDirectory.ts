//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Operations from '../../Signum.React/Signum.Operations'
import * as Authorization from '../Signum.Authorization.React/Signum.Authorization'
import * as Scheduler from '../Signum.Scheduler.React/Signum.Scheduler'


export module ActiveDirectoryAuthorizerMessage {
  export const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication = new MessageKey("ActiveDirectoryAuthorizerMessage", "ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication");
}

export const ActiveDirectoryConfigurationEmbedded = new Type<ActiveDirectoryConfigurationEmbedded>("ActiveDirectoryConfigurationEmbedded");
export interface ActiveDirectoryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ActiveDirectoryConfigurationEmbedded";
  domainName: string | null;
  domainServer: string | null;
  directoryRegistry_Username: string | null;
  directoryRegistry_Password: string | null;
  azure_ApplicationID: string /*Guid*/ | null;
  azure_DirectoryID: string /*Guid*/ | null;
  azure_ClientSecret: string | null;
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

export const OnPremisesExtensionAttributesModel = new Type<OnPremisesExtensionAttributesModel>("OnPremisesExtensionAttributesModel");
export interface OnPremisesExtensionAttributesModel extends Entities.ModelEntity {
  Type: "OnPremisesExtensionAttributesModel";
  extensionAttribute1: string;
  extensionAttribute2: string;
  extensionAttribute3: string;
  extensionAttribute4: string;
  extensionAttribute5: string;
  extensionAttribute6: string;
  extensionAttribute7: string;
  extensionAttribute8: string;
  extensionAttribute9: string;
  extensionAttribute10: string;
  extensionAttribute11: string;
  extensionAttribute12: string;
  extensionAttribute13: string;
  extensionAttribute14: string;
  extensionAttribute15: string;
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

export module UserADQuery {
  export const ActiveDirectoryUsers = new QueryKey("UserADQuery", "ActiveDirectoryUsers");
  export const ActiveDirectoryGroups = new QueryKey("UserADQuery", "ActiveDirectoryGroups");
}

export module UserOIDMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet = new MessageKey("UserOIDMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
}

