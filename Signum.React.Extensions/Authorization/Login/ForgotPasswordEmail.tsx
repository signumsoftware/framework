import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { RouteComponentProps } from 'react-router'
import * as QueryString from 'query-string'


interface ForgotPasswordEmailState { modelState?: ModelState; success?: boolean; message?:string }


export default class ForgotPassword extends React.Component<{}, ForgotPasswordEmailState > {
  constructor(props: {}) {
    super(props);
    this.state = {};
  }

  eMail!: HTMLInputElement;


  handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    this.setState({ modelState: { ...this.validateEmail() } }, () => {

      if (this.state.modelState && Dic.getValues(this.state.modelState).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ForgotPasswordEmailRequest = {
        email: this.eMail.value,
      };

      AuthClient.API.forgotPasswordEmail(request)
        .then(lr => {

          if (lr == null) {
            this.setState({ success: true });
          }
          else {
            this.setState({ success: false, message:lr });
          }
         
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            this.setState({ modelState: e.modelState });
        })
        .done();
    });
  }

  


  handleMailBlur = (event: React.SyntheticEvent<any>) => {
    this.setState({ modelState: { ...this.state.modelState, ...this.validateEmail() } as ModelState });
  }

  validateEmail() {
    return {
      eMail: !this.eMail.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()] :[]         
    }
  }


  error(field: string): string | undefined {
    var ms = this.state.modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }


  render() {

    if (this.state.success === true) {
      return (
        <div className="container">
          <div className="row">
            <div className="col-md-6 offset-md-3">
              <h2 className="sf-entity-title">{AuthMessage.RequestAccepted.niceToString()}</h2>
              <p>{AuthMessage.WeHaveSentYouAnEmailToResetYourPassword.niceToString()}</p>
            </div>
          </div>
        </div>
      );
    }
     

    return (

      <div className="container">
        <div className="row">
          <div className="col-md-6 offset-md-3">
            <form onSubmit={(e) => this.handleSubmit(e)}>
              <h2 className="sf-entity-title">{AuthMessage.IForgotMyPassword.niceToString()}</h2>
              <p>{AuthMessage.GiveUsYourUserEmailToResetYourPassword.niceToString()}</p>

              <div className={classes("form-group", this.error("eMail") && "has-error")}>
                <div>
                  <input type="texbox" className="form-control" id="eMail" ref={r => this.eMail = r!} onBlur={this.handleMailBlur} placeholder={AuthMessage.EnterYourUserEmail.niceToString()} />
                  {this.error("eMail") && <span className="help-block">{this.error("newPassword")}</span>}
                </div>
                <label className="col-form-label col-sm-2" style={this.state.success === false ? { display: "inline" } : { display: "none" }}>{this.state.message}</label>
              </div>

              <button type="submit" className="btn btn-primary" id="changePasswordRequest">{AuthMessage.SendEmail.niceToString()}</button>
            </form>
          </div>
        </div>
      </div>
    );
  }

}
