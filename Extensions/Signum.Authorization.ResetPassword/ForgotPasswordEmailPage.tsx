import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization/Signum.Authorization'
import * as ResetPasswordClient from './ResetPasswordClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ForgotPasswordEmailPage() {

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);
  const [success, setSuccess] = React.useState(false);
  const [message, setMessage] = React.useState<string | undefined>(undefined);

  const eMail = React.useRef<HTMLInputElement>(null);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    setModelState({ ...validateEmail() }).then(ms => {

      if (ms && Dic.getValues(ms).some(array => array.length > 0))
        return;

      const request: ResetPasswordClient.API.ForgotPasswordEmailRequest = {
        email: eMail.current!.value,
      };

      ResetPasswordClient.API.forgotPasswordEmail(request)
        .then(msg => {

          setSuccess(msg == undefined);
          setMessage(msg);
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            setModelState(e.modelState);
        });
    });
  }

  function handleMailBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateEmail() });
  }

  function validateEmail() {
    return {
      eMail: !eMail.current!.value ? [LoginAuthMessage.PasswordMustHaveAValue.niceToString()] : []
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
            <h2 className="sf-entity-title">{LoginAuthMessage.RequestAccepted.niceToString()}</h2>
            <p>{LoginAuthMessage.WeHaveSentYouAnEmailToResetYourPassword.niceToString()}</p>
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
            <h2 className="sf-entity-title">{LoginAuthMessage.IForgotMyPassword.niceToString()}</h2>
            <p>{LoginAuthMessage.GiveUsYourUserEmailToResetYourPassword.niceToString()}</p>

            <div className={classes("form-group", error("eMail") && "has-error")}>
              <div>
                <input type="texbox" className="form-control" id="eMail" ref={eMail} onBlur={handleMailBlur} placeholder={LoginAuthMessage.EnterYourUserEmail.niceToString()} />
                {error("eMail") && <span className="help-block">{error("newPassword")}</span>}
              </div>
              <label className="col-form-label col-sm-2" style={success === false ? { display: "inline" } : { display: "none" }}>{message}</label>
            </div>

            <button type="submit" className="btn btn-primary" id="changePasswordRequest">{LoginAuthMessage.SendEmail.niceToString()}</button>
          </form>
        </div>
      </div>
    </div>
  );
}
