import * as React from 'react'
import { classes, Dic, ifError } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Authorization'
import { AuthClient } from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from "@framework/QueryString"

export default function ChangePasswordPage(): React.JSX.Element {
  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);

  const currentUser = AuthClient.currentUser();
  const pendingUser = AuthClient.pendingPasswordChangeUser;
  const user = currentUser ?? pendingUser;
  const mustChangePassword = pendingUser != null || currentUser?.mustChangePassword === true;
  const [passValidation, setPassValidation] = React.useState<AuthClient.PasswordValidationResult | null>(null);

  const oldPassword = React.useRef<HTMLInputElement>(null);
  const newPassword = React.useRef<HTMLInputElement>(null);
  const newPassword2 = React.useRef<HTMLInputElement>(null);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    const newPwdState = comparePasswords();
    const ms = { ...validateOldPassword(), ...newPwdState };
    setModelState(ms);

    if (ms && Dic.getValues(ms).some(array => array.length > 0))
      return;

    if (passValidation?.level === "error")
      return;

    const request: AuthClient.API.ChangePasswordRequest = {
      oldPassword: oldPassword.current!.value,
      newPassword: newPassword.current!.value,
    };

    AuthClient.API.changePassword(request)
      .then(lr => {
        AuthClient.setAuthToken(lr.token, lr.authenticationType);
        AuthClient.setCurrentUser(lr.userEntity);
        AuthClient.pendingPasswordChangeUser = undefined;
        
        if (mustChangePassword) {
          const back = QueryString.parse(window.location.search).back;
          AuthClient.Options.onLogin(back);
        } else {
          AppContext.resetUI();
          AppContext.navigate("/auth/changePasswordSuccess");
        }
      })
      .catch(ifError(ValidationError, e => {
        if (e.modelState)
          setModelState(e.modelState);
      }));
  }

  function error(field: string): string | undefined {
    var ms = modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  function handleOldPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState(prevState => ({ ...prevState, ...validateOldPassword() }));
  }

  async function handlePasswordChange(e: React.SyntheticEvent<any>) {

    if (newPassword.current!.value && AuthClient.validatePassword && user) {
      const result = await AuthClient.validatePassword(newPassword.current!.value, user);

      setPassValidation(result);

      if (result?.level == "error") {
        setModelState(prevState => ({ ...prevState, ["newPassword"]: [result.message], ["newPassword2"]: [] }));
      } else {
        setModelState(prevState => ({ ...prevState, ["newPassword"]: [], ["newPassword2"]: [] }));
      }
    } else {
      setPassValidation(null);
      setModelState(prevState => ({ ...prevState, ["newPassword"]: [], ["newPassword2"]: [] }));
    }
  }

  function validateOldPassword(): ModelState {
    return {
      ["oldPassword"]: oldPassword.current!.value ? [] : [LoginAuthMessage.PasswordMustHaveAValue.niceToString()]
    };
  }

  function comparePasswords(): ModelState {
    const pwd1 = newPassword.current!.value;
    const pwd2 = newPassword2.current!.value;

    if (!pwd1 && !pwd2) {
      return { ["newPassword"]: [LoginAuthMessage.PasswordMustHaveAValue.niceToString()], ["newPassword2"]: [] };
    }

    if (pwd1 !== pwd2) {
      return { ["newPassword"]: [], ["newPassword2"]: [LoginAuthMessage.PasswordsAreDifferent.niceToString()] };
    }

    return { ["newPassword"]: [], ["newPassword2"]: [] };
  }

  function handlePasswordBlur() {
    const result = comparePasswords();
    setModelState(prevState => ({ ...prevState, ...result }));
  }

  return (
    <div className="container sf-reset-password">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <form onSubmit={(e) => handleSubmit(e)} className="w-100">
            <h1 className="sf-entity-title h2">{LoginAuthMessage.ChangePassword.niceToString()}</h1>
            {mustChangePassword && (
              <div className="alert alert-warning" role="alert">
                <strong>{LoginAuthMessage.PasswordMustBeChanged.niceToString()}</strong>
                <p className="mb-0">{LoginAuthMessage.YouMustChangeYourPasswordBeforeContinuing.niceToString()}</p>
              </div>
            )}
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
                <input type="password" className={classes("form-control form-control-sm", passValidation && "is-invalid")} id="newPassword" ref={newPassword} onChange={handlePasswordChange} onBlur={handlePasswordBlur} autoComplete="new-password" />
                {passValidation && <span className={classes('help-block', passValidation.level == 'error' ? 'text-danger' : 'text-warning')}>{passValidation.message}</span>}
              </div>
            </div>
            <div className={classes("form-group form-group-sm", error("newPassword2") && "has-error")}>
              <label className="col-form-label col-form-label-sm">{LoginAuthMessage.ConfirmNewPassword.niceToString()}</label>
              <div>
                <input type="password" className="form-control form-control-sm" id="newPassword2" ref={newPassword2} onBlur={handlePasswordBlur} autoComplete="new-password" />
                {error("newPassword2") && <span className="help-block">{error("newPassword2")}</span>}
              </div>
            </div>
            <button type="submit" className="btn btn-primary mt-2" id="changePassword">{LoginAuthMessage.ChangePassword.niceToString()}</button>
          </form>
        </div>
      </div>
    </div>
  );
}
