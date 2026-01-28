import * as React from 'react'
import { classes, Dic, ifError } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization'
import { AuthClient } from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ChangePasswordPage(): React.JSX.Element {
  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);


  const oldPassword = React.useRef<HTMLInputElement>(null);
  const newPassword = React.useRef<HTMLInputElement>(null);
  const newPassword2 = React.useRef<HTMLInputElement>(null);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    setModelState({ ...validateOldPassword(), ...validateNewPassword(true) }).then(ms => {

      if (ms && Dic.getValues(ms).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ChangePasswordRequest = {
        oldPassword: oldPassword.current!.value,
        newPassword: newPassword.current!.value,
      };

      AuthClient.API.changePassword(request)
        .then(lr => {
          AuthClient.setAuthToken(lr.token, lr.authenticationType);
          AuthClient.setCurrentUser(lr.userEntity);
          AppContext.resetUI();
          AppContext.navigate("/auth/changePasswordSuccess");
        })
        .catch(ifError(ValidationError, e => {
          if (e.modelState)
            setModelState(e.modelState);
        }));

    });
  }

  function error(field: string): string | undefined {
    var ms = modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  function handleOldPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateOldPassword() });
  }

  function handleNewPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateNewPassword(event.currentTarget == newPassword2!.current) });
  }

  function validateOldPassword(): ModelState {

    return {
      ["oldPassword"]: oldPassword.current!.value ? [] : [LoginAuthMessage.PasswordMustHaveAValue.niceToString()]
    };
  }

  function validateNewPassword(isSecond: boolean) {
    return {
      ["newPassword"]:
        !isSecond ? [] :
          !newPassword.current!.value && !newPassword2.current!.value ? [LoginAuthMessage.PasswordMustHaveAValue.niceToString()] :
            newPassword2.current!.value != newPassword.current!.value ? [LoginAuthMessage.PasswordsAreDifferent.niceToString()] :
              []
    }
  }

  return (
    <div className="container sf-reset-password">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <form onSubmit={(e) => handleSubmit(e)} className="w-100">
            <h1 className="sf-entity-title h2">{LoginAuthMessage.ChangePassword.niceToString()}</h1>
            <p>{LoginAuthMessage.EnterActualPasswordAndNewOne.niceToString()}</p>
            <div className={classes("form-group form-group-sm", error("oldPassword") && "has-error")}>
              <label className="col-form-label col-form-label-sm">{LoginAuthMessage.CurrentPassword.niceToString()}</label>
              <div>
                <input type="password" className="form-control form-control-sm" id="currentPassword" ref={oldPassword} onBlur={handleOldPasswordBlur} autoComplete="old-password" />
                {error("oldPassword") && <span className="help-block">{error("oldPassword")}</span>}
              </div>
            </div>
            <div className={classes("form-group form-group-sm", error("newPassword") && "has-error")}>
              <label className="col-form-label col-form-label-sm">{LoginAuthMessage.EnterTheNewPassword.niceToString()}</label>
              <div>
                <input type="password" className="form-control form-control-sm" id="newPassword" ref={newPassword} onBlur={handleNewPasswordBlur} autoComplete="new-password"/>
                {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
              </div>
            </div>
            <div className={classes("form-group form-group-sm", error("newPassword") && "has-error")}>
              <label className="col-form-label col-form-label-sm">{LoginAuthMessage.ConfirmNewPassword.niceToString()}</label>
              <div>
                <input type="password" className="form-control form-control-sm" id="newPassword2" ref={newPassword2} onBlur={handleNewPasswordBlur} autoComplete="new-password" />
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
  
