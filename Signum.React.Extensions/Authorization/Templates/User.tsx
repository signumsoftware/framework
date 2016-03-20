/// <reference path="../../../../Framework/Signum.React/typings/react/react.d.ts" />

import * as React from 'react'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityComponent, EntityLine, EntityCombo, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'




export default class User extends EntityComponent<UserEntity> {

    handlePasswordChange = (event: React.SyntheticEvent) => {

        var areDifferent = (this.refs["newPass"] as HTMLInputElement).value != (this.refs["newPass2"] as HTMLInputElement).value;

        if (areDifferent) {
            this.props.frame.setError({ "passwordHash": AuthMessage.PasswordsAreDifferent.niceToString() });
        }
        else {
            this.props.frame.setError(null);
            (this.entity as any).newPassword = (this.refs["newPass"] as HTMLInputElement).value;
        }

    }

    renderEntity() {
        const ctx = this.props.ctx;
        const ph = this.props.ctx.subCtx(a => a.passwordHash, { labelColumns: { sm: 4 } });

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true }) } />
                <ValueLine ctx={ctx.subCtx(e => e.userName) } />
                {this.entity.isNew && <div>
                    <FormGroup ctx={ ph } title={AuthMessage.ChangePasswordAspx_NewPassword.niceToString() }>
                        <input type="password" ref="newPass" className="form-control" onChange={this.handlePasswordChange}/>
                    </FormGroup>
                    <FormGroup ctx={ ph } title={AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }>
                        <input type="password" ref="newPass2" className="form-control" onChange={this.handlePasswordChange}/>
                    </FormGroup>
                </div>}
                <EntityCombo ctx={ctx.subCtx(e => e.role) } />
                <ValueLine ctx={ctx.subCtx(e => e.email) } />
                <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo) }/>
                <ValueLine ctx={ctx.subCtx(e => e.passwordNeverExpires, { labelColumns: { sm: 4 } }) } />
                <ValueLine ctx={ctx.subCtx(e => e.passwordSetDate, { labelColumns: { sm: 4 } }) } />
            </div>
        );
    }
}

