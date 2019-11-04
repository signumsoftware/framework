import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { classes } from '@framework/Globals'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import MessageModal from '../../../../Framework/Signum.React/Scripts/Modals/MessageModal'

export default class Login extends React.Component<{}, { modelState?: ModelState }> {

  static customLoginButtons?: () => React.ReactElement<any>;

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
            <div className="col-md-6 offset-md-3">
              <h2 className="sf-entity-title">{AuthClient.currentUser() ? AuthMessage.SwitchUser.niceToString() : AuthMessage.Login.niceToString()}</h2>
              <p>{AuthMessage.EnterYourUserNameAndPassword.niceToString()}</p>
              <hr />
            </div>
          </div>
          <div className="row">
            <div className="col-md-6 offset-md-3">
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
            <div className="col-md-6 offset-md-3">
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
          {AuthClient.Options.userTicket &&
            <div className="row">
            <div className="col-md-6 offset-md-3" style={{ paddingTop: ".35rem" }}>
                <div className="form-check mb-2 mr-sm-2 mb-sm-0">
                  <label>
                    <input ref={r => this.rememberMe = r!} name="remember" type="checkbox" /> {AuthMessage.RememberMe.niceToString()}
                  </label>
                </div>
              </div>
            </div>
          }

          <div className="row" style={{ paddingTop: "1rem" }}>
            <div className="col-md-6 offset-md-3">
              <button type="submit" id="login" className="btn btn-success"><FontAwesomeIcon icon="sign-in-alt" /> {AuthClient.currentUser() ? AuthMessage.SwitchUser.niceToString() : AuthMessage.Login.niceToString()}</button>
              {this.error("login") && <span className="help-block" style={{ color: "red" }}>{this.error("login")}</span>}
              {AuthClient.Options.resetPassword &&
                <span>
                &nbsp;
                &nbsp;
                <Link to="~/auth/forgotPasswordEmail">{AuthMessage.IHaveForgottenMyPassword.niceToString()}</Link>
                </span>
              }
            </div>
          </div>

          {Login.customLoginButtons && Login.customLoginButtons()}
        </form>
      </div>
    );
  }
}


export function LoginWithWindowsButton() {

  function onClick() {
    return AuthClient.API.loginWindowsAuthentication(true)
      .then(r => {
        if (r == null) {
          MessageModal.showError(AuthMessage.LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication.niceToString(), AuthMessage.NoWindowsUserFound.niceToString()).done();
        } else {
          AuthClient.setAuthToken(r.token);
          AuthClient.setCurrentUser(r.userEntity);
          AuthClient.Options.onLogin();
        }
      }).done();
  }

  return (
    <div className="row">
      <div className="col-md-6 offset-md-3 mt-4">
        <button onClick={e => { e.preventDefault(); onClick(); }} className="btn btn-info">
          <FontAwesomeIcon icon={["fab", "windows"]} /> {AuthMessage.LoginWithWindowsUser.niceToString()}
        </button>
      </div>
    </div>
  );
}
