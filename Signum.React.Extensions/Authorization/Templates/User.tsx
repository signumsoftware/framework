import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import { Binding } from '@framework/Reflection'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, FormGroup, TypeContext } from '@framework/Lines'


export default class User extends React.Component<{ ctx: TypeContext<UserEntity> }, { withPassword: boolean }> {

    constructor(props: any) {
        super(props);
        this.state = { withPassword: false };
    }

    render() {
        const ctx = this.props.ctx.subCtx({ labelColumns: { sm: 3 } });
        const entity = this.props.ctx.value;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.state, { readOnly: true }) } />
                <ValueLine ctx={ctx.subCtx(e => e.userName) } />
                { entity.isNew || this.state.withPassword ?
                    <DoublePassword ctx={new TypeContext<string>(ctx, undefined, undefined as any, Binding.create(ctx.value, v => v.newPassword)) }/> :
                    !ctx.readOnly && this.renderButton(ctx)
                }
                <EntityLine ctx={ctx.subCtx(e => e.role) } />
                <ValueLine ctx={ctx.subCtx(e => e.email) } />
                <EntityCombo ctx={ctx.subCtx(e => e.cultureInfo) }/>
                <ValueLine ctx={ctx.subCtx(e => e.passwordNeverExpires) } />
                <ValueLine ctx={ctx.subCtx(e => e.passwordSetDate) } />
            </div>
        );
    }

    renderButton(ctx: TypeContext<UserEntity>) {
        return (
            <FormGroup labelText={AuthMessage.NewPassword.niceToString()} ctx={ctx}>
                <a className="btn btn-light btn-sm" onClick={() => this.setState({ withPassword: true }) }>
                    <FontAwesomeIcon icon="key" /> {AuthMessage.ChangePassword.niceToString() }
                </a>
            </FormGroup>
        );
    }
}

class DoublePassword extends React.Component<{ ctx: TypeContext<string> }>{

    handlePasswordBlur = (event: React.SyntheticEvent<any>) => {

        const ctx = this.props.ctx;

        if (this.newPass.value && this.newPass2.value && this.newPass.value != this.newPass2.value) {
            ctx.error = AuthMessage.PasswordsAreDifferent.niceToString()
        }
        else {
            ctx.error = undefined;
            ctx.value = this.newPass.value;
        }

        ctx.frame!.revalidate();
    }

    newPass!: HTMLInputElement;
    newPass2!: HTMLInputElement;

    render() {
        return (
            <div>
                <FormGroup ctx={this.props.ctx} labelText={AuthMessage.ChangePasswordAspx_NewPassword.niceToString()}>
                    <input type="password" ref={p => this.newPass = p!} className={this.props.ctx.formControlClass} onBlur={this.handlePasswordBlur} />
                </FormGroup>
                <FormGroup ctx={ this.props.ctx } labelText={AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }>
                    <input type="password" ref={p => this.newPass2 = p!} className={this.props.ctx.formControlClass} onBlur={this.handlePasswordBlur}/>
                </FormGroup>
            </div>
        );
    }
}

