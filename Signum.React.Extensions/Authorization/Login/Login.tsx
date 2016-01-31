import * as React from 'react'
import { Grid, Row, Col, Input, Button } from 'react-bootstrap'
import { Link } from 'react-router'
import { Calendar } from 'react-widgets'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ModelState, ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'



export default class Login extends React.Component<{}, { modelState?: ModelState }> {

    constructor(props) {
        super(props);
        this.state = {};
    }


    handleLogin(e: React.FormEvent) {

        e.preventDefault();

        const rememberMe = this.refs["rememberMe"] as Input;

        const request: AuthClient.Api.LoginRequest = {
            userName: (this.refs["userName"] as Input).getValue(),
            password: (this.refs["password"] as Input).getValue(),
            rememberMe: rememberMe ? rememberMe.getChecked() : null,
        };

        AuthClient.Api.login(request)
            .then(response => {
                AuthClient.setCurrentUser(response.userEntity);
                AuthClient.onLogin();
            })
            .catch((e: ValidationError) => {
                if (e.modelState)
                    this.setState({ modelState: e.modelState });
            });
    }

    render() {



        return (
            <form onSubmit={(e) => this.handleLogin(e) }>
                <Row>
                    <Col smOffset={4} sm={6}>
                        <h2>Login</h2>
                        <p>{ AuthMessage.IntroduceYourUserNameAndPassword.niceToString() }</p>
                    </Col>
                </Row>
                <div className="form-horizontal">

                    <Input ref="userName" type="text" label={AuthMessage.Username.niceToString() } placeholder={AuthMessage.Username.niceToString() }
                        labelClassName="col-sm-offset-2 col-sm-2"
                        wrapperClassName="col-sm-4"
                        help={this.error("userName") }
                        bsStyle={this.error("userName") ? "error" : null}
                        hasFeedback/>

                    <Input ref="password" type="password" label={AuthMessage.Password.niceToString() } placeholder={AuthMessage.Password.niceToString() }
                        labelClassName="col-sm-offset-2 col-sm-2"
                        wrapperClassName="col-sm-4"
                        help={this.error("password") }
                        bsStyle={this.error("password") ? "error" : null}
                        hasFeedback/>

                    {AuthClient.userTicket &&
                        <Input ref="rememberMe" type="checkbox" label={AuthMessage.RememberMe.niceToString() } wrapperClassName="col-sm-offset-4 col-sm-6"/>}
                </div>
                <Row>
                    <Col smOffset={4} sm={6}>
                        <Button ref="login" bsStyle="primary" type="submit">{AuthMessage.Login.niceToString() }</Button>

                        { AuthClient.resetPassword &&
                            <div>
                                <Link to="auth/resetPassword">{AuthMessage.IHaveForgottenMyPassword.niceToString() }</Link>
                            </div>
                        }
                    </Col>
                </Row>
            </form>
        );
    }

    error(field: string) {
        return this.state.modelState && this.state.modelState[field];
    }
}
