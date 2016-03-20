import * as React from 'react'
import { RoleEntity, AuthMessage, AuthAdminMessage, MergeStrategy } from '../Signum.Entities.Authorization'
import { EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList } from '../../../../Framework/Signum.React/Scripts/Lines'

export default class Role extends EntityComponent<RoleEntity> {

    renderEntity() {
        return (
            <div>
                <ValueLine ctx={this.subCtx(e => e.name)} />
                <ValueLine ctx={this.subCtx(e => e.mergeStrategy) } unitText={this.rolesMessage() } onChange={() => this.forceUpdate() } />
                <EntityList ctx={this.subCtx(e => e.roles) } onChange={() => this.forceUpdate() }/>
            </div>
        );
    }

    rolesMessage(): string {
        return AuthAdminMessage.NoRoles.niceToString() + " ⇒ " +
            (this.entity.mergeStrategy == "Union" ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).niceToString();
    }
}

