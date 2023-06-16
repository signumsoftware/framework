import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import * as ResetPasswordClient from './ResetPasswordClient'
import * as AuthClient from '../Signum.Authorization/AuthClient'
import { useLocation, useParams } from 'react-router'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { LoginAuthMessage } from '../Signum.Authorization/Signum.Authorization'

export default function ResetPassword() {
  const location = useLocation();

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);

  const [success, setSuccess] = React.useState<boolean>(false);

  const newPassword = React.useRef<HTMLInputElement>(null);
  const newPassword2 = React.useRef<HTMLInputElement>(null);
  const code = String(QueryString.parse(location.search).code!);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    setModelState({ ...validateNewPassword(true) }).then(ms => {

      if (ms && Dic.getValues(ms).some(array => array.length > 0))
        return;

      const request: ResetPasswordClient.API.ResetPasswordRequest = {
        code: code,
        newPassword: newPassword.current!.value,
      };

      ResetPasswordClient.API.resetPassword(request)
        .then(lr => {
          AuthClient.setAuthToken(lr.token, lr.authenticationType);
          AuthClient.setCurrentUser(lr.userEntity);

          setSuccess(true);
          //Navigator.resetUI();
          AppContext.navigate("/auth/ResetPassword?code=OK");
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            setModelState(e.modelState);
        });
    });
  }

  function handleNewPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateNewPassword(event.currentTarget == newPassword2.current) });
  }

  function validateNewPassword(isSecond: boolean) {
    return {
      ["newPassword"]:
        !isSecond ? [] :
          !newPassword.current!.value && !newPassword2.current!.value ? [LoginAuthMessage.PasswordMustHaveAValue.niceToString()] :
            newPassword2.current!.value != newPassword.current!.value ? [LoginAuthMessage.PasswordsAreDifferent.niceToString()] :
              []
    };
  }

  function error(field: string): string | undefined {
    var ms = modelState;
    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  if (success || code == "OK") {
    return (
      <div className="container sf-reset-password">
        <div className="row">
          <div className="col-md-6 offset-md-3">
            <h2 className="sf-entity-title">{LoginAuthMessage.PasswordChanged.niceToString()}</h2>
            <p>{LoginAuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container sf-reset-password">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <form onSubmit={(e) => handleSubmit(e)}>
            <h2 className="sf-entity-title">{LoginAuthMessage.ChangePassword.niceToString()}</h2>
            <p>{LoginAuthMessage.NewPassword.niceToString()}</p>
            <div>

              <div className={classes("form-group mb-3", error("newPassword") && "has-error")}>
                <input type="password" className="form-control" id="newPassword" ref={newPassword} onBlur={handleNewPasswordBlur} placeholder={LoginAuthMessage.EnterTheNewPassword.niceToString()} />
                {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
              </div>
              <div className={classes("form-group mb-3", error("newPassword") && "has-error")}>
                <input type="password" className="form-control" id="newPassword2" ref={newPassword2} onBlur={handleNewPasswordBlur} placeholder={LoginAuthMessage.ConfirmNewPassword.niceToString()} />
                {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
              </div>

            </div>
            <button type="submit" className="btn btn-primary" id="changePassword">{LoginAuthMessage.ChangePassword.niceToString()}</button>
          </form>
        </div>
      </div>
    </div>
  );
}
