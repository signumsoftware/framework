import * as React from 'react'
import { Link } from 'react-router'
import { Calendar } from 'react-widgets'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ModelState } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'



export default class Login extends React.Component<{}, { modelState?: ModelState }> {

    constructor(props) {
        super(props);
        this.state = {};
    }


    handleLogin(e: React.FormEvent) {

        e.preventDefault();

        const rememberMe = this.refs["rememberMe"] as HTMLInputElement;

        const request: AuthClient.Api.LoginRequest = {
            userName: (this.refs["userName"] as HTMLInputElement).value,
            password: (this.refs["password"] as HTMLInputElement).value,
            rememberMe: rememberMe ? rememberMe.checked : null,
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

    render() {



        return (
            <form onSubmit={(e) => this.handleLogin(e) }>
                <div className="row">
                    <div className="col-sm-offset-4 col-sm-6">
                        <h2>Login</h2>
                        <p>{ AuthMessage.IntroduceYourUserNameAndPassword.niceToString() }</p>
                    </div>
                </div>
                <div className="form-horizontal">

                    <div className={classes("form-group", this.error("userName") ? "has-error" : null) }>
                        <label htmlFor="userName" className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.Username.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="text" className="form-control" id="userName" ref="userName" placeholder={AuthMessage.Username.niceToString() }/>
                            {this.error("userName") && <span className="help-block">{this.error("userName") }</span>}
                        </div>
                    </div>

                    <div className={classes("form-group", this.error("password") ? "has-error" : null) }>
                        <label htmlFor="password" className="col-sm-offset-2 col-sm-2 control-label">{AuthMessage.Password.niceToString() }</label>
                        <div className="col-sm-4">
                            <input type="password" className="form-control" id="password" ref="password" placeholder={AuthMessage.Password.niceToString() }/>
                            {this.error("password") && <span className="help-block">{this.error("password") }</span>}
                        </div>
                    </div>


                    {AuthClient.userTicket &&
                        <div className="row">
                            <div className="col-sm-offset-4 col-sm-6">
                                <div className="checkbox">
                                    <label> <input type="checkbox" ref="rememberMe"/>{AuthMessage.RememberMe.niceToString() }</label>
                                </div>
                            </div>
                        </div>
                    }
                </div>
                <br/>
                <div className="row">
                    <div className="col-sm-offset-4 col-sm-6">
                        <button className="btn btn-primary" ref="login" type="submit">{AuthMessage.Login.niceToString() }</button>

                        { AuthClient.resetPassword &&
                            <div>
                                <Link to="auth/resetPassword">{AuthMessage.IHaveForgottenMyPassword.niceToString() }</Link>
                            </div>
                        }
                    </div>
                </div>
            </form>
        );
    }

    error(field: string) {
        return this.state.modelState && this.state.modelState[field];
    }
}
