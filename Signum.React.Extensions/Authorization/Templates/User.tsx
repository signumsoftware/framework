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
                    <DoublePassword ctx={ctx.subCtx(a => a.passwordHash, { labelColumns: { sm: 3 } }) }/> :
                    !ctx.readOnly && this.renderButton()
                }
                <EntityCombo ctx={ctx.subCtx(e => e.role) } />
                <ValueLine ctx={ctx.subCtx(e => e.email) } />
                <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo) }/>
                <ValueLine ctx={ctx.subCtx(e => e.passwordNeverExpires, { labelColumns: { sm: 3 } }) } />
                <ValueLine ctx={ctx.subCtx(e => e.passwordSetDate, { labelColumns: { sm: 3 } }) } />
            </div>
        );
    }

    renderButton() {
        return (
            <FormGroup labelText={AuthMessage.NewPassword.niceToString()} ctx={this.props.ctx}>
                <a className="btn btn-default btn-sm" onClick={() => this.setState({ withPassword: true }) }>
                    <i className="fa fa-key"></i> {AuthMessage.ChangePassword.niceToString() }
                </a>
            </FormGroup>
        );
    }
}

class DoublePassword extends React.Component<{ ctx: TypeContext<string> }, void>{

    handlePasswordBlur = (event: React.SyntheticEvent) => {

        var ctx = this.props.ctx;

        if (this.newPass.value && this.newPass2.value && this.newPass.value != this.newPass2.value) {
            ctx.binding.error = AuthMessage.PasswordsAreDifferent.niceToString()
        }
        else {
            ctx.binding.error = null;
            ctx.value = this.newPass.value;
        }

        ctx.frame.onClose

    }

    newPass: HTMLInputElement;
    newPass2: HTMLInputElement;

    render() {
        return (
            <div>
                <FormGroup ctx={ this.props.ctx } labelText={AuthMessage.ChangePasswordAspx_NewPassword.niceToString() }>
                    <input type="password" ref={p => this.newPass = p} className="form-control" onBlur={this.handlePasswordBlur}/>
                </FormGroup>
                <FormGroup ctx={ this.props.ctx } labelText={AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }>
                    <input type="password" ref={p => this.newPass2 = p} className="form-control" onBlur={this.handlePasswordBlur}/>
                </FormGroup>
            </div>
        );
    }
}

