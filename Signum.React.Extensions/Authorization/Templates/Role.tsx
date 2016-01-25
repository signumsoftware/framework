/// <reference path="../../../../Framework/Signum.React/typings/react/react.d.ts" />

import * as React from 'react'
import { AuthMessage, RoleEntity, AuthAdminMessage, MergeStrategy } from '../Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityComponent, EntityLine, EntityCombo, EntityList } from '../../../../Framework/Signum.React/Scripts/Lines'

export default class Role extends EntityComponent<RoleEntity> {
    
    render() {
        return (<div>
            <ValueLine ctx={this.subCtx(e => e.name) } />
            <ValueLine ctx={this.subCtx(e => e.mergeStrategy) } unitText={this.rolesMessage() } />
            <EntityList ctx={this.subCtx(e => e.roles) }/>
            </div>);
    }

    rolesMessage(): string {
        return AuthAdminMessage.NoRoles.niceToString() + "-> " +
            (this.value.mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).niceToString();
    }
}

