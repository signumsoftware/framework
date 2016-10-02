import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import * as AuthClient from '../../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import { UserEntity, RoleEntity } from '../../../../Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import { Expression } from './NodeUtils'


export class AuthInfo {
    get user(): UserEntity {
        return AuthClient.currentUser();
    }

    get role(): Lite<RoleEntity> {
        return this.user.role!;
    }

    isFindable(queryKey: string) {
        return Finder.isFindable(queryKey);
    }

    isViewable(type: string) {
        return Navigator.isViewable(type);
    }

    permissionAllowed(permissionKey: string) {
        return AuthClient.isPermissionAuthorized(permissionKey);
    }

    operationAllowed(operationKey: string) {
        return AuthClient.isOperationAuthorized(operationKey);
    }
}
