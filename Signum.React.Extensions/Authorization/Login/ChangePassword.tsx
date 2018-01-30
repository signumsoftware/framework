import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ModelState } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'



export default class ChangePassword extends React.Component<{}, { modelState?: ModelState; success?: boolean }> {

    constructor(props: {}) {
        super(props);
        this.state = {};
    }
    

    handleSubmit(e: React.FormEvent<any>) {

        e.preventDefault();

        const request: AuthClient.API.ChangePasswordRequest = {
            oldPassword: this.oldPassword.value,
            newPassword: this.newPassword.value,
        };

        AuthClient.API.changePassword(request)
            .then(user => {
                AuthClient.setCurrentUser(user);
                Navigator.resetUI();
                this.setState({ success: true });
            })
            .catch((e: ValidationError) => {
                if (e.modelState)
                    this.setState({ modelState: e.modelState });
            })
            .done();
    }

    oldPassword!: HTMLInputElement;
    newPassword!: HTMLInputElement;
    newPassword2!: HTMLInputElement;


    error(field: string) {
        return this.state.modelState && this.state.modelState[field];
    }

    handlePasswordBlur = (event: React.SyntheticEvent<any>) => {
        
        if (this.newPassword.value && this.newPassword2.value && this.newPassword2.value != this.newPassword.value)
            this.setState({ modelState: { ["newPassword"]: [AuthMessage.PasswordsAreDifferent.niceToString()] } as ModelState });
        else
            this.setState({ modelState: undefined });
    }

    isEnabled() {
        return (
            this.oldPassword && this.oldPassword.value &&
            this.newPassword && this.newPassword.value &&
            this.newPassword2 && this.newPassword2.value &&
            this.newPassword.value == this.newPassword2.value
        );

    }

    render() {

        if (this.state.success) {
            return (
                <div>
                    <h2>{AuthMessage.PasswordChanged.niceToString() }</h2>
                    <p>{ AuthMessage.PasswordHasBeenChangedSuccessfully.niceToString() }</p>
                </div>
            );
        }

        return (
            <form onSubmit={(e) => this.handleSubmit(e) }>
                <div className="row">
                    <div className="col-sm-offset-4 col-sm-6">
                        <h2>{AuthMessage.ChangePasswordAspx_ChangePassword.niceToString() }</h2>
                        <p>{ AuthMessage.ChangePasswordAspx_EnterActualPasswordAndNewOne.niceToString() }</p>
                    </div>
                </div>
                <div>
                    <div className="form-group">
                        <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.ChangePasswordAspx_ActualPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="currentPassword" ref={r => this.oldPassword = r!} placeholder={AuthMessage.ChangePasswordAspx_ActualPassword.niceToString() } onBlur={()=>this.forceUpdate()}/>
                            {this.error("oldPassword") && <span className="help-block">{this.error("oldPassword") }</span>}
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.EnterTheNewPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="newPassword" ref={r => this.newPassword = r!} placeholder={AuthMessage.NewPassword.niceToString() } onBlur={this.handlePasswordBlur}/>
                            {this.error("newPassword") && <span className="help-block">{this.error("newPassword") }</span>}
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="newPassword2" ref={r => this.newPassword2 = r!} placeholder={AuthMessage.ConfirmNewPassword.niceToString() } onBlur={this.handlePasswordBlur}/>
                            {this.error("newPassword") && <span className="help-block">{this.error("newPassword") }</span>}
                        </div>
                    </div>
                </div>
                <div className="row">
                    <div className="col-sm-offset-4 col-sm-6">
                        <button type="submit" className="btn btn-light" id="changePassword"  disabled={!this.isEnabled() }>{AuthMessage.ChangePassword.niceToString() }</button>
                    </div>
                </div>
            </form>
        );
    }

}
