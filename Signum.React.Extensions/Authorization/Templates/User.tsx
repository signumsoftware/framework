/// <reference path="../../../../Framework/Signum.React/typings/react/react.d.ts" />

import * as React from 'react'
import { AuthMessage, UserEntity } from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityComponent, EntityLine, EntityCombo, FormGroup } from 'Framework/Signum.React/Scripts/Lines'

export default class User extends EntityComponent<UserEntity> {

    handlePasswordChange = (event: React.SyntheticEvent) => {

    }

    render() {
        var ph = this.subCtx(a=> a.passwordHash);
        return (
            <div>
            <ValueLine ctx={this.subCtx(e => e.state, { readOnly: true }) } />
            <ValueLine ctx={this.subCtx(e => e.userName) } />
                {this.value.isNew && <div>
                    <FormGroup ctx={ph} labelProps={AuthMessage.ChangePasswordAspx_NewPassword}>
                        <input type="password" ref="newPass" className="form-control" onChange={this.handlePasswordChange}/>
                        </FormGroup>
                    <FormGroup ctx={ph} labelProps={AuthMessage.ChangePasswordAspx_ConfirmNewPassword}>
                        <input type="password" ref="newPass2" className="form-control" onChange={this.handlePasswordChange}/>
                        </FormGroup>
                    </div>}
            <EntityLine ctx={this.subCtx(e => e.role) } />
            <ValueLine ctx={this.subCtx(e => e.email) } />
            <EntityCombo ctx={this.subCtx(e => e.cultureInfo) }/>
            <ValueLine ctx={this.subCtx(e => e.passwordNeverExpires, { labelColumns: { sm: 4 } }) } />
            <ValueLine ctx={this.subCtx(e => e.passwordSetDate, { labelColumns: { sm: 4 } }) } />
                </div>);
    }
}

