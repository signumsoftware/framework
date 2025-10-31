import { MessageKey, QueryKey, Type } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as ADGroups from '../Signum.Authorization/Signum.Authorization.ADGroups';
import * as Authorization from '../Signum.Authorization/Signum.Authorization';
import * as Files from '../Signum.Files/Signum.Files';
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler';
export declare namespace AuthADFileType {
    const CachedProfilePhoto: Files.FileTypeSymbol;
}
export declare const AzureADConfigurationEmbedded: Type<AzureADConfigurationEmbedded>;
export interface AzureADConfigurationEmbedded extends ADGroups.BaseADConfigurationEmbedded {
    loginWithAzureAD: boolean;
    applicationID: string;
    directoryID: string;
    azureB2C: AzureB2CEmbedded | null;
    clientSecret: string | null;
    useDelegatedPermission: boolean;
}
export declare namespace AzureADQuery {
    const ActiveDirectoryUsers: QueryKey;
    const ActiveDirectoryGroups: QueryKey;
}
export declare namespace AzureADTask {
    const DeactivateUsers: Scheduler.SimpleTaskSymbol;
}
export declare const AzureB2CEmbedded: Type<AzureB2CEmbedded>;
export interface AzureB2CEmbedded extends Entities.EmbeddedEntity {
    Type: "AzureB2CEmbedded";
    loginWithAzureB2C: boolean;
    tenantName: string;
    signInSignUp_UserFlow: string | null;
    signIn_UserFlow: string | null;
    signUp_UserFlow: string | null;
    resetPassword_UserFlow: string | null;
}
export declare const CachedProfilePhotoEntity: Type<CachedProfilePhotoEntity>;
export interface CachedProfilePhotoEntity extends Entities.Entity {
    Type: "CachedProfilePhoto";
    user: Entities.Lite<Authorization.UserEntity>;
    size: number;
    photo: Files.FilePathEmbedded | null;
    invalidationDate: string;
    creationDate: string;
}
export declare namespace CachedProfilePhotoOperation {
    const Save: Operations.ExecuteSymbol<CachedProfilePhotoEntity>;
    const Delete: Operations.DeleteSymbol<CachedProfilePhotoEntity>;
}
export declare const OnPremisesExtensionAttributesModel: Type<OnPremisesExtensionAttributesModel>;
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
export declare const UserAzureADMixin: Type<UserAzureADMixin>;
export interface UserAzureADMixin extends Entities.MixinEntity {
    Type: "UserAzureADMixin";
    oID: string | null;
}
export declare namespace UserOIDMessage {
    const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet: MessageKey;
}
//# sourceMappingURL=Signum.Authorization.AzureAD.d.ts.map