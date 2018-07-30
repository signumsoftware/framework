import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'



export default class ChangePassword extends React.Component<{}, { modelState?: ModelState; success?: boolean }> {

    constructor(props: {}) {
        super(props);
        this.state = {};
    }
    

    handleSubmit(e: React.FormEvent<any>) {
        
        e.preventDefault();

        this.setState({ modelState: { ...this.validateOldPassword(), ...this.validateNewPassword(true) } }, () => {
            
            if (this.state.modelState && Dic.getValues(this.state.modelState).some(array => array.length > 0))
                return;

            const request: AuthClient.API.ChangePasswordRequest = {
                oldPassword: this.oldPassword.value,
                newPassword: this.newPassword.value,
            };

            AuthClient.API.changePassword(request)
                .then(lr => {
                    AuthClient.setAuthToken(lr.token);
                    AuthClient.setCurrentUser(lr.userEntity);
                    Navigator.resetUI();
                    this.setState({ success: true });
                })
                .catch((e: ValidationError) => {
                    if (e.modelState)
                        this.setState({ modelState: e.modelState });
                })
                .done();
        });
    }

    oldPassword!: HTMLInputElement;
    newPassword!: HTMLInputElement;
    newPassword2!: HTMLInputElement;


    error(field: string): string | undefined {
        var ms = this.state.modelState;

        return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
    }

    handleOldPasswordBlur = (event: React.SyntheticEvent<any>) => {
        this.setState({ modelState: { ...this.state.modelState, ...this.validateOldPassword() } as ModelState });
    }

    handleNewPasswordBlur = (event: React.SyntheticEvent<any>) => {
        this.setState({ modelState: { ...this.state.modelState, ...this.validateNewPassword(event.currentTarget == this.newPassword2) } as ModelState });
    }

    validateOldPassword(): ModelState {

        return {
            ["oldPassword"]: this.oldPassword.value ? [] : [AuthMessage.PasswordMustHaveAValue.niceToString()]
        };
    }

    validateNewPassword(isSecond: boolean) {
        return {
            ["newPassword"]:
                !isSecond ? [] :
                !this.newPassword.value && !this.newPassword2.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()] :
                    this.newPassword2.value != this.newPassword.value ? [AuthMessage.PasswordsAreDifferent.niceToString()] :
                        []
        }
    }


    render() {

        if (this.state.success) {
            return (
                <div>
                    <h2 className="sf-entity-title">{AuthMessage.PasswordChanged.niceToString()}</h2>
                    <p>{ AuthMessage.PasswordHasBeenChangedSuccessfully.niceToString() }</p>
                </div>
            );
        }

        return (
            <form onSubmit={(e) => this.handleSubmit(e) }>
                <div className="row">
                    <div className="offset-sm-2 col-sm-6">
                        <h2 className="sf-entity-title">{AuthMessage.ChangePasswordAspx_ChangePassword.niceToString() }</h2>
                        <p>{ AuthMessage.ChangePasswordAspx_EnterActualPasswordAndNewOne.niceToString() }</p>
                    </div>
                </div>
                <div>
                    <div className={classes("form-group row", this.error("oldPassword") && "has-error")}>
                        <label className="col-form-label col-sm-2">{AuthMessage.ChangePasswordAspx_ActualPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="currentPassword" ref={r => this.oldPassword = r!} onBlur={this.handleOldPasswordBlur} />
                            {this.error("oldPassword") && <span className="help-block">{this.error("oldPassword") }</span>}
                        </div>
                    </div>
                    <div className={classes("form-group row", this.error("newPassword") && "has-error")}>
                        <label className="col-form-label col-sm-2">{AuthMessage.EnterTheNewPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="newPassword" ref={r => this.newPassword = r!} onBlur={this.handleNewPasswordBlur}/>
                            {this.error("newPassword") && <span className="help-block">{this.error("newPassword") }</span>}
                        </div>
                    </div>
                    <div className={classes("form-group row", this.error("newPassword") && "has-error")}>
                        <label className="col-form-label col-sm-2">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="newPassword2" ref={r => this.newPassword2 = r!} onBlur={this.handleNewPasswordBlur}/>
                            {this.error("newPassword") && <span className="help-block">{this.error("newPassword") }</span>}
                        </div>
                    </div>
                </div>
                <div className="row">
                    <div className="offset-sm-2 col-sm-6">
                        <button type="submit" className="btn btn-primary" id="changePassword">{AuthMessage.ChangePassword.niceToString() }</button>
                    </div>
                </div>
            </form>
        );
    }

}
