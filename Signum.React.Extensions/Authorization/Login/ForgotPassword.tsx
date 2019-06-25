import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'

export default class ForgotPassword extends React.Component<{}, { modelState?: ModelState; success?: boolean }> {
  constructor(props: {}) {
    super(props);
    this.state = {};
  }


  handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    this.setState({ modelState: { ...this.validatePassword() } }, () => {

      if (this.state.modelState && Dic.getValues(this.state.modelState).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ForgotPasswordRequest = {
        code: this.code.value,
        password: this.password.value,
        repeatPassword: this.repeatPassword.value,
      };

      AuthClient.API.forgotPassword(request)
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

  code!: HTMLInputElement;
  password!: HTMLInputElement;
  repeatPassword!: HTMLInputElement;


  error(field: string): string | undefined {
    var ms = this.state.modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  handlePasswordBlur = (event: React.SyntheticEvent<any>) => {
    this.setState({ modelState: { ...this.state.modelState, ...this.validatePassword() } as ModelState });
  }



  validatePassword(): ModelState {

    return {
      ["password"]: !this.password.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()]:
       this.password.value == this.repeatPassword.value ? [] : [AuthMessage.PasswordsAreDifferent.niceToString()],
    };
  }




  render() {

    if (this.state.success) {
      return (
        <div>
          <h2 className="sf-entity-title">{AuthMessage.PasswordChanged.niceToString()}</h2>
          <p>{AuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
        </div>
      );
    }

    return (
      <form onSubmit={(e) => this.handleSubmit(e)}>
        <input type="password" className="form-control" id="code" ref={r => this.password = r!} onBlur={this.handlePasswordBlur} />
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <h2 className="sf-entity-title">{AuthMessage.ChangePasswordAspx_ChangePassword.niceToString()}</h2>
            <p>{AuthMessage.ChangePasswordAspx_NewPassword.niceToString()}</p>
          </div>
        </div>
        <div>
          <div className={classes("form-group row", this.error("password") && "has-error")}>
            <label className="col-form-label col-sm-2">{AuthMessage.ChangePasswordAspx_NewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="password" ref={r => this.password = r!} onBlur={this.handlePasswordBlur} />
              {this.error("oldPassword") && <span className="help-block">{this.error("oldPassword")}</span>}
            </div>
          </div>
          <div className={classes("form-group row", this.error("repeatPassword") && "has-error")}>
            <label className="col-form-label col-sm-2">{AuthMessage.EnterTheNewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="repeatPassword" ref={r => this.repeatPassword = r!} onBlur={this.handlePasswordBlur} />
              {this.error("repeatPassword") && <span className="help-block">{this.error("repeatPassword")}</span>}
            </div>
          </div>
        
        </div>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <button type="submit" className="btn btn-primary" id="changePassword">{AuthMessage.ChangePassword.niceToString()}</button>
          </div>
        </div>
      </form>
    );
  }

}
