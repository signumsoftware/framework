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

      const request: AuthClient.API.forgotPasswordEmailRequest = {
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
        <div>
          <h2 className="sf-entity-title">{AuthMessage.RequestAccepted.niceToString()}</h2>
          <p>{AuthMessage.WeHaveSentYouAnEmailToResetYourPassword.niceToString()}</p>
        </div>
      );
    }
     

    return (
      <form onSubmit={(e) => this.handleSubmit(e)}>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <h2 className="sf-entity-title">{AuthMessage.IForgotMyPassword.niceToString()}</h2>
            <p>{AuthMessage.GiveUsYourUserEmailToResetYourPassword.niceToString()}</p>
          </div>
        </div>
        <div>

          <div className={classes("form-group row", this.error("eMail") && "has-error")}>
            <label className="col-form-label col-sm-2">{AuthMessage.EnterYourUserEmail.niceToString()}</label>
            <div className="col-sm-4">
              <input type="texbox" className="form-control" id="eMail" ref={r => this.eMail = r!} onBlur={this.handleMailBlur} />
              {this.error("eMail") && <span className="help-block">{this.error("newPassword")}</span>}
            </div>
            <label className="col-form-label col-sm-2" style={this.state.success === false ? { display: "inline" } : { display: "none" }}>{this.state.message}</label>
          </div>
       
        </div>
        <div className="row">
          <div className="offset-sm-2 col-sm-6">
            <button type="submit" className="btn btn-primary" id="changePasswordRequest">{AuthMessage.SendEmail.niceToString()}</button>
          </div>
        </div>
      </form>
    );
  }

}
