/// <reference path="../../../../Framework/Signum.React/typings/react/react.d.ts" />

import * as React from 'react'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, FormGroup, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'




export default class User extends React.Component<{ ctx: TypeContext<UserEntity> }, { withPassword: boolean }> {

    constructor(props) {
        super(props);
        this.state = { withPassword: false };
    }

    render() {
        const ctx = this.props.ctx;
        var entity = this.props.ctx.value;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true }) } />
                <ValueLine ctx={ctx.subCtx(e => e.userName) } />
                { entity.isNew || this.state.withPassword ?
                    <DoublePassword ctx={ctx.subCtx(a => a.passwordHash, { labelColumns: { sm: 4 } }) }/> :
                    !ctx.readOnly  &&
                    <a className="btn btn-default btn-sm" onClick={() => this.setState({ withPassword: true }) }>
                        <i className="fa fa-key"></i> {AuthMessage.ChangePassword.niceToString() }
                    </a>
                }
                <EntityCombo ctx={ctx.subCtx(e => e.role) } />
                <ValueLine ctx={ctx.subCtx(e => e.email) } />
                <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo) }/>
                <ValueLine ctx={ctx.subCtx(e => e.passwordNeverExpires, { labelColumns: { sm: 4 } }) } />
                <ValueLine ctx={ctx.subCtx(e => e.passwordSetDate, { labelColumns: { sm: 4 } }) } />
            </div>
        );
    }
}

class DoublePassword extends React.Component<{ ctx: TypeContext<string> }, void>{

    handlePasswordChange = (event: React.SyntheticEvent) => {

        var areDifferent = this.newPass.value != this.newPass2.value;

        if (this.newPass.value && areDifferent) {
            this.props.ctx.frame.setError({ "passwordHash": AuthMessage.PasswordsAreDifferent.niceToString() });
        }
        else {
            this.props.ctx.frame.setError(null);
            (this.props.ctx.value as any).newPassword = (this.refs["newPass"] as HTMLInputElement).value;
        }

    }

    newPass: HTMLInputElement;
    newPass2: HTMLInputElement;

    render() {
        return (
            <div>
                <FormGroup ctx={ this.props.ctx } labelText={AuthMessage.ChangePasswordAspx_NewPassword.niceToString() }>
                        <input type="password" ref={p=>this.newPass = p} className="form-control" onBlur={this.handlePasswordChange}/>
                    </FormGroup>
                <FormGroup ctx={ this.props.ctx } labelText={AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }>
                        <input type="password" ref={p=>this.newPass2 = p} className="form-control" onBlur={this.handlePasswordChange}/>
                    </FormGroup>
            </div>
        );
    }

}

