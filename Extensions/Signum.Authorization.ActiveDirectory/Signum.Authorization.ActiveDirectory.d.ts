import { MessageKey, Type } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Authorization from '../Signum.Authorization/Signum.Authorization';
import * as Files from '../Signum.Files/Signum.Files';
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler';
export declare namespace ActiveDirectoryAuthorizerMessage {
    const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication: MessageKey;
}
export declare const ActiveDirectoryConfigurationEmbedded: Type<ActiveDirectoryConfigurationEmbedded>;
export interface ActiveDirectoryConfigurationEmbedded extends Entities.EmbeddedEntity {
    Type: "ActiveDirectoryConfigurationEmbedded";
    domainName: string | null;
    directoryRegistry_Username: string | null;
    directoryRegistry_Password: string | null;
    azure_ApplicationID: string | null;
    azure_DirectoryID: string | null;
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
export declare namespace ActiveDirectoryMessage {
    const Id: MessageKey;
    const DisplayName: MessageKey;
    const Mail: MessageKey;
    const GivenName: MessageKey;
    const Surname: MessageKey;
    const JobTitle: MessageKey;
    const OnPremisesImmutableId: MessageKey;
    const CompanyName: MessageKey;
    const AccountEnabled: MessageKey;
    const OnPremisesExtensionAttributes: MessageKey;
    const OnlyActiveUsers: MessageKey;
    const InGroup: MessageKey;
    const Description: MessageKey;
    const SecurityEnabled: MessageKey;
    const Visibility: MessageKey;
    const HasUser: MessageKey;
}
export declare namespace ActiveDirectoryPermission {
    const InviteUsersFromAD: Basics.PermissionSymbol;
}
export declare namespace ActiveDirectoryTask {
    const DeactivateUsers: Scheduler.SimpleTaskSymbol;
}
export declare const ADGroupEntity: Type<ADGroupEntity>;
export interface ADGroupEntity extends Entities.Entity {
    Type: "ADGroup";
    displayName: string;
}
export declare namespace ADGroupOperation {
    const Save: Operations.ExecuteSymbol<ADGroupEntity>;
    const Delete: Operations.DeleteSymbol<ADGroupEntity>;
}
export declare namespace AuthADFileType {
    const CachedProfilePhoto: Files.FileTypeSymbol;
}
export declare const AzureB2CEmbedded: Type<AzureB2CEmbedded>;
export interface AzureB2CEmbedded extends Entities.EmbeddedEntity {
    Type: "AzureB2CEmbedded";
    tenantName: string;
    signInSignUp_UserFlow: string;
}
export declare const CachedProfilePhotoEntity: Type<CachedProfilePhotoEntity>;
export interface CachedProfilePhotoEntity extends Entities.Entity {
    Type: "CachedProfilePhoto";
    user: Entities.Lite<Authorization.UserEntity>;
    size: number;
    photo: Files.FilePathEmbedded | null;
    creationDate: string;
}
export declare namespace CachedProfilePhotoOperation {
    const Save: Operations.ExecuteSymbol<CachedProfilePhotoEntity>;
    const Delete: Operations.DeleteSymbol<CachedProfilePhotoEntity>;
}
export declare const RoleMappingEmbedded: Type<RoleMappingEmbedded>;
export interface RoleMappingEmbedded extends Entities.EmbeddedEntity {
    Type: "RoleMappingEmbedded";
    aDNameOrGuid: string;
    role: Entities.Lite<Authorization.RoleEntity>;
}
export declare namespace UserADMessage {
    const Find0InActiveDirectory: MessageKey;
    const FindInActiveDirectory: MessageKey;
    const NoUserContaining0FoundInActiveDirectory: MessageKey;
    const SelectActiveDirectoryUser: MessageKey;
    const PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport: MessageKey;
    const NameOrEmail: MessageKey;
}
export declare const UserADMixin: Type<UserADMixin>;
export interface UserADMixin extends Entities.MixinEntity {
    Type: "UserADMixin";
    oID: string | null;
    sID: string | null;
}
export declare namespace UserOIDMessage {
    const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet: MessageKey;
}
