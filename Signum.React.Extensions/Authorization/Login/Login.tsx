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
            <div className="container">
                <form className="form-horizontal" onSubmit={(e) => this.handleSubmit(e)}>
                    <div className="row">
                        <div className="col-md-3"></div>
                        <div className="col-md-6">
                            <h2>Login</h2>
                            <p>{AuthMessage.EnterYourUserNameAndPassword.niceToString()}</p>
                            <hr />
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-3"></div>
                        <div className="col-md-6">
                            <div className={classes("form-group", this.error("userName") && "has-danger")}>
                                <label className="sr-only" htmlFor="userName">{AuthMessage.Username.niceToString()}</label>
                                <div className="input-group mb-2 mr-sm-2 mb-sm-0">
                                    <div className="input-group-addon" style={{ width: "2.6rem" }}><i className="fa fa-user"></i></div>
                                    <input type="text" className="form-control" id="userName" ref={r => this.userName = r!} placeholder={AuthMessage.Username.niceToString()} />
                                </div>
                            </div>
                        </div>
                        <div className="col-md-3">
                            <div className="form-control-feedback">
                                <span className="text-danger align-middle">
                                    <i className="fa fa-close"></i> Example error message
                        </span>
                            </div>
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-3"></div>
                        <div className="col-md-6">
                            <div className="form-group">
                                <label className="sr-only" htmlFor="password">Password</label>
                                <div className="input-group mb-2 mr-sm-2 mb-sm-0">
                                    <div className="input-group-addon" style={{ width: "2.6rem" }}><i className="fa fa-key"></i></div>
                                    <input type="password" name="password" className="form-control" id="password"
                                        placeholder="Password" required />
                                </div>
                            </div>
                        </div>
                        <div className="col-md-3">
                            <div className="form-control-feedback">
                                <span className="text-danger align-middle">
                                </span>
                            </div>
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-3"></div>
                        <div className="col-md-6" style={{ paddingTop: ".35rem" }}>
                            <div className="form-check mb-2 mr-sm-2 mb-sm-0">
                                <label className="form-check-label">
                                    <input className="form-check-input" name="remember"
                                        type="checkbox" />
                                    <span style={{ paddingBottom: ".15rem" }}>Remember me</span>
                                </label>
                            </div>
                        </div>
                    </div>
                    <div className="row" style={{ paddingTop: "1rem" }}>
                        <div className="col-md-3"></div>
                        <div className="col-md-6">
                            <button type="submit" className="btn btn-success"><i className="fa fa-sign-in"></i> Login</button>
                            <a className="btn btn-link" href="/password/reset">Forgot Your Password?</a>
                        </div>
                    </div>
                </form>
            </div>
        );
    }
}
