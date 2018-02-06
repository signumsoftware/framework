import * as React from 'react'
import { Link } from 'react-router-dom'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ModelState } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'

export default class Login extends React.Component<{}, { modelState?: ModelState }> {

    constructor(props: {}) {
        super(props);
        this.state = {};
    }


    handleSubmit(e: React.FormEvent<any>) {

        e.preventDefault();

        const request: AuthClient.API.LoginRequest = {
            userName: this.userName.value,
            password: this.password.value,
            rememberMe: this.rememberMe ? this.rememberMe.checked : undefined,
        };

        AuthClient.API.login(request)
            .then(response => {
                AuthClient.setAuthToken(response.token);
                AuthClient.setCurrentUser(response.userEntity);
                AuthClient.Options.onLogin();
            })
            .catch((e: ValidationError) => {
                if (e.modelState)
                    this.setState({ modelState: e.modelState });
            })
            .done();
    }

    userName: HTMLInputElement;
    password: HTMLInputElement;
    rememberMe: HTMLInputElement;


    error(field: string) {
        return this.state.modelState && this.state.modelState[field];
    }

    render() {

        return (
            <form onSubmit={(e) => this.handleSubmit(e)}>

                <div className="form-horizontal">

                    <div className="form-group">
                        <div className="col-sm-offset-4 col-sm-6">
                            <h2>Login</h2>
                            <p>{AuthMessage.EnterYourUserNameAndPassword.niceToString()}</p>
                        </div>
                    </div>

                    <div className={classes("form-group", this.error("userName") ? "has-error" : undefined)}>
                        <label htmlFor="userName" className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.Username.niceToString()}</label>
                        <div className="col-sm-4">
                            <div className="input-group">
                                <span className="input-group-addon"><i className="glyphicon glyphicon-user"></i></span>
                                <input type="text" className="form-control" id="userName" ref={r => this.userName = r!} placeholder={AuthMessage.Username.niceToString()} />
                            </div>
                            {this.error("userName") && <span className="help-block">{this.error("userName")}</span>}
                        </div>
                    </div>

                    <div className={classes("form-group", this.error("password") ? "has-error" : undefined)}>
                        <label htmlFor="password" className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.Password.niceToString()}</label>
                        <div className="col-sm-4">
                            <div className="input-group">
                                <span className="input-group-addon"><i className="glyphicon glyphicon-lock"></i></span>
                                <input type="password" className="form-control" id="password" ref={r => this.password = r!} placeholder={AuthMessage.Password.niceToString()} />
                            </div>
                            {this.error("password") && <span className="help-block">{this.error("password")}</span>}
                        </div>
                    </div>

                    {AuthClient.userTicket &&
                        <div className="form-group">
                            <div className="col-sm-offset-4 col-sm-6">
                                <div className="checkbox">
                                    <label> <input type="checkbox" ref={r => this.rememberMe = r!} />{AuthMessage.RememberMe.niceToString()}</label>
                                </div>
                            </div>
                        </div>
                    }


                    <div className="form-group">
                        <div className="col-sm-offset-4 col-sm-6">
                            <button className="btn btn-primary" ref="login" id="login" type="submit">{AuthMessage.Login.niceToString()}</button>
                        </div>
                    </div>

                    {AuthClient.resetPassword &&
                        <div className="form-group">
                            <div className="col-sm-offset-4 col-sm-6">
                                <Link to="~/auth/resetPassword">{AuthMessage.IHaveForgottenMyPassword.niceToString()}</Link>
                            </div>
                        </div>
                    }
                </div>
            </form>
        );
    }

}
