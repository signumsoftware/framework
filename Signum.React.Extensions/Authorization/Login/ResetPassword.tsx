import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { RouteComponentProps } from 'react-router'
import * as QueryString from 'query-string'

interface ResetPasswordProps extends RouteComponentProps<{}> { }

interface ResetPasswordState { modelState?: ModelState; success?: boolean }


export default class ResetPassword extends React.Component<ResetPasswordProps, ResetPasswordState> {
  constructor(props: ResetPasswordProps) {
    super(props);
    this.state = {};
  }

  newPassword!: HTMLInputElement;
  newPassword2!: HTMLInputElement;
  code: string = String(QueryString.parse(this.props.location.search).code!);

  handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    this.setState({ modelState: { ...this.validateNewPassword(false) } }, () => {

      if (this.state.modelState && Dic.getValues(this.state.modelState).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ResetPasswordRequest = {
        code: this.code,
        newPassword: this.newPassword.value,
      };

      AuthClient.API.resetPassword(request)
        .then(lr => {
          AuthClient.setAuthToken(lr.token);
          AuthClient.setCurrentUser(lr.userEntity);
         
          this.setState({ success: true });
          //Navigator.resetUI();
          Navigator.history.push("~/auth/ResetPassword?code=OK");
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            this.setState({ modelState: e.modelState });
        })
        .done();
    });
  }




  handleNewPasswordBlur = (event: React.SyntheticEvent<any>) => {
    this.setState({ modelState: { ...this.state.modelState, ...this.validateNewPassword(event.currentTarget == this.newPassword2) } as ModelState });
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


  error(field: string): string | undefined {
    var ms = this.state.modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }


  render() {

    if (this.state.success || this.code=="OK") {
      return (

        <div>
          <h2 className="sf-entity-title">{AuthMessage.PasswordChanged.niceToString()}</h2>
          <p>{AuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
        </div>
      );
    }

    return (
      <form onSubmit={(e) => this.handleSubmit(e)}>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <h2 className="sf-entity-title">{AuthMessage.ChangePasswordAspx_ChangePassword.niceToString()}</h2>
            <p>{AuthMessage.ChangePasswordAspx_NewPassword.niceToString()}</p>
          </div>
        </div>
        <div>

          <div className={classes("form-group row", this.error("newPassword") && "has-error")}>
            <label className="col-form-label col-sm-2">{AuthMessage.EnterTheNewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="newPassword" ref={r => this.newPassword = r!} onBlur={this.handleNewPasswordBlur} />
              {this.error("newPassword") && <span className="help-block">{this.error("newPassword")}</span>}
            </div>
          </div>
          <div className={classes("form-group row", this.error("newPassword") && "has-error")}>
            <label className="col-form-label col-sm-2">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString()}</label>
            <div className="col-sm-4">
              <input type="password" className="form-control" id="newPassword2" ref={r => this.newPassword2 = r!} onBlur={this.handleNewPasswordBlur} />
              {this.error("newPassword") && <span className="help-block">{this.error("newPassword")}</span>}
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
