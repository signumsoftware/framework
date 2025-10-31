//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Authorization from './Signum.Authorization'


export namespace ActiveDirectoryAuthorizerMessage {
  export const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication: MessageKey = new MessageKey("ActiveDirectoryAuthorizerMessage", "ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication");
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

export const BaseADConfigurationEmbedded: Type<BaseADConfigurationEmbedded> = new Type<BaseADConfigurationEmbedded>("BaseADConfigurationEmbedded");
export interface BaseADConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "BaseADConfigurationEmbedded";
  allowMatchUsersBySimpleUserName: boolean;
  autoCreateUsers: boolean;
  autoUpdateUsers: boolean;
  roleMapping: Entities.MList<RoleMappingEmbedded>;
  defaultRole: Entities.Lite<Authorization.RoleEntity> | null;
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

