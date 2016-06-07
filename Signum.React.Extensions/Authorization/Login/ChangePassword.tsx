import * as React from 'react'
import { Link } from 'react-router'
import { Calendar } from 'react-widgets'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ModelState } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'



export default class ChangePassword extends React.Component<{}, { modelState?: ModelState }> {

    constructor(props) {
        super(props);
        this.state = {};
    }


    handleSubmit(e: React.FormEvent) {

        e.preventDefault();
        
        const request: AuthClient.Api.LoginRequest = {
            userName: this.userName.value,
            password: this.password.value,
            rememberMe: this.rememberMe ? this.rememberMe.checked : null,
        };

        AuthClient.Api.login(request)
            .then(response => {
                AuthClient.setCurrentUser(response.userEntity);
                AuthClient.onLogin();
            })
            .catch((e: ValidationError) => {
                if (e.modelState)
                    this.setState({ modelState: e.modelState });
            })
            .done();
    }

    currentPass: HTMLInputElement;
    newPass: HTMLInputElement;
    newPass2: HTMLInputElement;


    error(field: string) {
        return this.state.modelState && this.state.modelState[field];
    }

    render() {

        return (
            <form onSubmit={(e) => this.handleSubmit(e) }>
                <div className="col-sm-offset-4">
                    <h2>Login</h2>
                    <p>
                        {AuthMessage.ChangePasswordAspx_ChangePassword.niceToString() }
                    </p>
                    
                </div>

                @using (Html.BeginForm())
                {
                    <div className="form-horizontal">
                        <div className="form-group">
                            <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.ChangePasswordAspx_ActualPassword.niceToString() }</label>
                            <div className="col-sm-4">
                                <input type="password" className="form-control" id="currentPassword" ref={r => this.currentPass = r} placeholder={AuthMessage.ChangePasswordAspx_ActualPassword.niceToString() }/>
                                {this.error("password") && <span className="help-block">{this.error("password") }</span>}
                            </div>
                        </div>
                        <div className="form-group">
                            <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.ChangePasswordAspx_NewPassword.niceToString() }</label>
                            <div className="col-sm-4">
                                @Html.Password(UserMapping.NewPasswordKey, null, new { @class = "form-control", placeholder = AuthMessage.ChangePasswordAspx_NewPassword.NiceToString() })
                            </div>
                        </div>
                        <div className="form-group">
                            <label className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString() }</label>
                            <div className="col-sm-4">
                                @Html.Password(UserMapping.NewPasswordBisKey, null, new { @class = "form-control", placeholder = AuthMessage.ChangePasswordAspx_ConfirmNewPassword.NiceToString() })
                            </div>
                        </div>
                    </div>
                    <div className="form-group">
                        <div className="col-sm-offset-4 col-sm-6">
                            <button type="submit" className="btn btn-default" id="login">{AuthMessage,ChangePassword.niceToString() }</button>
                        </div>
                    </div>   
}
            </form>
        );
    }

}
