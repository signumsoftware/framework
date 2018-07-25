import * as React from 'react'
import { RoleEntity, AuthMessage, AuthAdminMessage, MergeStrategy } from '../Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, TypeContext } from '@framework/Lines'

export default class Role extends React.Component<{ ctx: TypeContext<RoleEntity> }> {

    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.name)} />
                <ValueLine ctx={ctx.subCtx(e => e.mergeStrategy) } unitText={this.rolesMessage() } onChange={() => this.forceUpdate() } />
                <EntityList ctx={ctx.subCtx(e => e.roles) } onChange={() => this.forceUpdate() }/>
            </div>
        );
    }

    rolesMessage(): string {
        return AuthAdminMessage.NoRoles.niceToString() + " ⇒ " +
            (this.props.ctx.value.mergeStrategy == "Union" ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).niceToString();
    }
}

