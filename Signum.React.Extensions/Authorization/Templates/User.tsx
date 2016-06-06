/// <reference path="../../../../Framework/Signum.React/typings/react/react.d.ts" />

import * as React from 'react'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, FormGroup, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'




export default class User extends React.Component<{ ctx: TypeContext<UserEntity> }, void> {

    handlePasswordChange = (event: React.SyntheticEvent) => {

        var pass2Value = (this.refs["newPass2"] as HTMLInputElement).value;
        var areDifferent = pass2Value != "" && (this.refs["newPass"] as HTMLInputElement).value != pass2Value ;

        if (areDifferent) {
            this.props.ctx.frame.setError({ "passwordHash": AuthMessage.PasswordsAreDifferent.niceToString() });
        }
        else {
            this.props.ctx.frame.setError(null);
            (this.props.ctx.value as any).newPassword = (this.refs["newPass"] as HTMLInputElement).value;
        }

    }

    render() {
        const ctx = this.props.ctx;
        const ph = this.props.ctx.subCtx(a => a.passwordHash, { labelColumns: { sm: 4 } });
        var entity = this.props.ctx.value;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true }) } />
                <ValueLine ctx={ctx.subCtx(e => e.userName) } />
                { entity.isNew && <div>
                    <FormGroup ctx={ ph } labelText={AuthMessage.ChangePasswordAspx_NewPassword.niceToString() }>
                        <input type="password" ref="newPass" className="form-control" onBlur={this.handlePasswordChange}/>
                    </FormGroup>
                    <FormGroup ctx={ ph } labelText={AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }>
                        <input type="password" ref="newPass2" className="form-control" onBlur={this.handlePasswordChange}/>
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

