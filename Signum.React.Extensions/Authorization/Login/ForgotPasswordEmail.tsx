import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { RouteComponentProps } from 'react-router'
import * as QueryString from 'query-string'
import { useStateWithPromise } from '../../../../Framework/Signum.React/Scripts/Hooks'


interface ForgotPasswordEmailState { modelState?: ModelState; success?: boolean; message?: string }


export default function ForgotPassword() {

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);
  const [success, setSuccess] = React.useState(false);
  const [message, setMessage] = React.useState<string | undefined>(undefined);

  const eMail = React.useRef<HTMLInputElement>(null);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    setModelState({ ...validateEmail() }).then(ms => {

      if (ms && Dic.getValues(ms).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ForgotPasswordEmailRequest = {
        email: eMail.current!.value,
      };

      AuthClient.API.forgotPasswordEmail(request)
        .then(msg => {

          setSuccess(msg == undefined);
          setMessage(msg);
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            setModelState(e.modelState).done();
        })
        .done();
    }).done();
  }

  function handleMailBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateEmail() }).done();
  }

  function validateEmail() {
    return {
      eMail: !eMail.current!.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()] : []
    }
  }

  function error(field: string): string | undefined {
    var ms = modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }


  if (success === true) {
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
          <form onSubmit={(e) => handleSubmit(e)}>
            <h2 className="sf-entity-title">{AuthMessage.IForgotMyPassword.niceToString()}</h2>
            <p>{AuthMessage.GiveUsYourUserEmailToResetYourPassword.niceToString()}</p>

            <div className={classes("form-group", error("eMail") && "has-error")}>
              <div>
                <input type="texbox" className="form-control" id="eMail" ref={eMail} onBlur={handleMailBlur} placeholder={AuthMessage.EnterYourUserEmail.niceToString()} />
                {error("eMail") && <span className="help-block">{error("newPassword")}</span>}
              </div>
              <label className="col-form-label col-sm-2" style={success === false ? { display: "inline" } : { display: "none" }}>{message}</label>
            </div>

            <button type="submit" className="btn btn-primary" id="changePasswordRequest">{AuthMessage.SendEmail.niceToString()}</button>
          </form>
        </div>
      </div>
    </div>
  );
}
