import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { classes } from '@framework/Globals'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
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
      userName: this.userName!.value,
      password: this.password!.value,
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

  componentDidMount() {
    this.userName!.focus();
  }

  userName!: HTMLInputElement;
  password!: HTMLInputElement;
  rememberMe?: HTMLInputElement;


  error(field: string) {
    return this.state.modelState && this.state.modelState[field];
  }

  render() {

    return (
      <div className="container">
        <form onSubmit={(e) => this.handleSubmit(e)}>
          <div className="row">
            <div className="col-md-3"></div>
            <div className="col-md-6">
              <h2 className="sf-entity-title">{AuthMessage.Login.niceToString()}</h2>
              <p>{AuthMessage.EnterYourUserNameAndPassword.niceToString()}</p>
              <hr />
            </div>
          </div>
          <div className="row">
            <div className="col-md-3"></div>
            <div className="col-md-6">
              <div className={classes("form-group", this.error("userName") && "has-error")}>
                <label className="sr-only" htmlFor="userName">{AuthMessage.Username.niceToString()}</label>
                <div className="input-group mb-2 mr-sm-2 mb-sm-0">
                  <div className="input-group-prepend">
                    <div className="input-group-text"><FontAwesomeIcon icon="user" style={{ width: "16px" }} /></div>
                  </div>
                  <input type="text" className="form-control" id="userName" ref={r => this.userName = r!} placeholder={AuthMessage.Username.niceToString()} />
                </div>
                {this.error("userName") && <span className="help-block">{this.error("userName")}</span>}
              </div>
            </div>
          </div>
          <div className="row">
            <div className="col-md-3"></div>
            <div className="col-md-6">
              <div className={classes("form-group", this.error("password") && "has-error")}>
                <label className="sr-only" htmlFor="password">Password</label>
                <div className="input-group mb-2 mr-sm-2 mb-sm-0">
                  <div className="input-group-prepend">
                    <div className="input-group-text"><FontAwesomeIcon icon="key" style={{ width: "16px" }} /></div>
                  </div>
                  <input ref={r => this.password = r!} type="password" name="password" className="form-control" id="password" placeholder="Password" />
                </div>
                {this.error("password") && <span className="help-block">{this.error("password")}</span>}
              </div>
            </div>
          </div>
          {AuthClient.userTicket &&
            <div className="row">
              <div className="col-md-3"></div>
              <div className="col-md-6" style={{ paddingTop: ".35rem" }}>
                <div className="form-check mb-2 mr-sm-2 mb-sm-0">
                  <label>
                    <input ref={r => this.rememberMe = r!} name="remember" type="checkbox" /> {AuthMessage.RememberMe.niceToString()}
                  </label>
                </div>
              </div>
            </div>
          }

          <div className="row" style={{ paddingTop: "1rem" }}>
            <div className="col-md-3"></div>
            <div className="col-md-6">
              <button type="submit" id="login" className="btn btn-success"><FontAwesomeIcon icon="sign-in-alt" /> {AuthMessage.Login.niceToString()}</button>
              {this.error("login") && <span className="help-block" style={{ color: "red" }}>{this.error("login")}</span>}
              {AuthClient.resetPassword &&
                <span>
                  &nbsp;
                  &nbsp;
                                <Link to="~/auth/resetPassword">{AuthMessage.IHaveForgottenMyPassword.niceToString()}</Link>
                </span>
              }
            </div>
          </div>
        </form>
      </div>
    );
  }
}
