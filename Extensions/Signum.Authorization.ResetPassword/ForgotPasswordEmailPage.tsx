import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization/Signum.Authorization'
import { ResetPasswordClient } from './ResetPasswordClient'
import { useStateWithPromise } from '@framework/Hooks'
import { AutoFocus } from '@framework/Components/AutoFocus'

export default function ForgotPasswordEmailPage(): React.JSX.Element {

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);
  const [success, setSuccess] = React.useState(false);
  const [message, setMessage] = React.useState<string | undefined>(undefined);
  const [title, setTitle] = React.useState<string | undefined>(undefined);

  const eMail = React.useRef<HTMLInputElement>(null);

  async function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    if (validateEmail()) {

      const request: ResetPasswordClient.API.ForgotPasswordEmailRequest = {
        email: eMail.current!.value,
      };

      try {
        var response = await ResetPasswordClient.API.forgotPasswordEmail(request);

        setSuccess(response.success);
        setMessage(response.message);
        setTitle(response.title);

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
      setModelState({});
      return true;
    }

    setModelState({
      eMail: [LoginAuthMessage.EnterYourUserEmail.niceToString()]
    });
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
          <div className="col-md-6 offset-md-3 forgot-password-success">
            {title &&
              <>
                <h1 className="sf-entity-title h2">{title}</h1>
                <p>{message}</p>
              </>}
            {!title && <h2 className="sf-entity-title">{message}</h2>}
          </div>
        </div>
      </div>
    );
  }


  return (
    <AutoFocus>
      <div className="container">
        <div className="row">
          <div className="col-md-6 offset-md-3">
            <form onSubmit={(e) => handleSubmit(e)}>
              <h1 className="sf-entity-title h2">{LoginAuthMessage.IForgotMyPassword.niceToString()}</h1>
              <p>{LoginAuthMessage.GiveUsYourUserEmailToResetYourPassword.niceToString()}</p>

              <div className={classes("form-group mb-3", error("eMail") && "has-error")}>
                <input type="texbox" className="form-control" id="eMail" ref={eMail} onBlur={validateEmail} placeholder={LoginAuthMessage.EnterYourUserEmail.niceToString()} />
                {error("eMail") && <span className="help-block text-danger">{error("eMail")}</span>}
                {message && <div className="form-text text-danger">{message}</div>}
              </div>

              <button type="submit" className="btn btn-primary" id="changePasswordRequest">{LoginAuthMessage.SendEmail.niceToString()}</button>
            </form>
          </div>
        </div>
      </div>
    </AutoFocus>
  );
}
