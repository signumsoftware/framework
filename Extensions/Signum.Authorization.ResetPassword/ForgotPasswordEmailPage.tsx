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

  async function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    if (validateEmail()) {

      const request: ResetPasswordClient.API.ForgotPasswordEmailRequest = {
        email: eMail.current!.value,
      };

      try {
        var msg = await ResetPasswordClient.API.forgotPasswordEmail(request);

        setSuccess(msg == undefined);
        setMessage(msg);

      } catch (e) {
        if (e instanceof ValidationError) {
          setModelState(e.modelState);
        }
        throw e;
      }
    }
  }

  function validateEmail() {
    if (eMail.current?.value) {
      setModelState({
        eMail: [LoginAuthMessage.PasswordMustHaveAValue.niceToString()]
      });

      return true;
    }

    setModelState({  });
    return false;
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

            <div className={classes("form-group mb-3", error("eMail") && "has-error")}>
                <input type="texbox" className="form-control" id="eMail" ref={eMail} onBlur={validateEmail} placeholder={LoginAuthMessage.EnterYourUserEmail.niceToString()} />
              {error("eMail") && <span className="help-block text-danger">{error("newPassword")}</span>}
              {message && < div className="form-text text-danger">{message}</div>}
            </div>

            <button type="submit" className="btn btn-primary" id="changePasswordRequest">{LoginAuthMessage.SendEmail.niceToString()}</button>
          </form>
        </div>
      </div>
    </div>
  );
}
