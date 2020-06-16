import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { AuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { useStateWithPromise } from '@framework/Hooks'

export default function ChangePassword() {
  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);

  const [success, setSuccess] = React.useState(false);

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
          Navigator.resetUI();
          setSuccess(true);
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            setModelState(e.modelState).done();
        })
        .done();

    }).done();
  }

  function error(field: string): string | undefined {
    var ms = modelState;

    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  function handleOldPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateOldPassword() }).done();
  }

  function handleNewPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateNewPassword(event.currentTarget == newPassword2!.current) }).done();
  }

  function validateOldPassword(): ModelState {

    return {
      ["oldPassword"]: oldPassword.current!.value ? [] : [AuthMessage.PasswordMustHaveAValue.niceToString()]
    };
  }

  function validateNewPassword(isSecond: boolean) {
    return {
      ["newPassword"]:
        !isSecond ? [] :
          !newPassword.current!.value && !newPassword2.current!.value ? [AuthMessage.PasswordMustHaveAValue.niceToString()] :
            newPassword2.current!.value != newPassword.current!.value ? [AuthMessage.PasswordsAreDifferent.niceToString()] :
              []
    }
  }

  if (success) {
    return (
      <div>
        <h2 className="sf-entity-title">{AuthMessage.PasswordChanged.niceToString()}</h2>
        <p>{AuthMessage.PasswordHasBeenChangedSuccessfully.niceToString()}</p>
      </div>
    );
  }

  return (
    <form onSubmit={(e) => handleSubmit(e)} className="w-100">
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <h2 className="sf-entity-title">{AuthMessage.ChangePasswordAspx_ChangePassword.niceToString()}</h2>
          <p>{AuthMessage.ChangePasswordAspx_EnterActualPasswordAndNewOne.niceToString()}</p>
        </div>
      </div>
      <div className="row">
      <div className="col-md-6 offset-md-3">
        <div className={classes("form-group", error("oldPassword") && "has-error")}>
          <label className="col-form-label">{AuthMessage.ChangePasswordAspx_ActualPassword.niceToString()}</label>
          <div>
            <input type="password" className="form-control" id="currentPassword" ref={oldPassword} onBlur={handleOldPasswordBlur} />
            {error("oldPassword") && <span className="help-block">{error("oldPassword")}</span>}
          </div>
        </div>
        <div className={classes("form-group", error("newPassword") && "has-error")}>
          <label className="col-form-label">{AuthMessage.EnterTheNewPassword.niceToString()}</label>
          <div>
            <input type="password" className="form-control" id="newPassword" ref={newPassword} onBlur={handleNewPasswordBlur} />
            {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
          </div>
        </div>
        <div className={classes("form-group", error("newPassword") && "has-error")}>
          <label className="col-form-label">{AuthMessage.ChangePasswordAspx_ConfirmNewPassword.niceToString()}</label>
          <div>
            <input type="password" className="form-control" id="newPassword2" ref={newPassword2} onBlur={handleNewPasswordBlur} />
            {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
          </div>
        </div>
      </div>
      </div>
      <div className="row">
        <div className="col-md-6 offset-md-3">
          <button type="submit" className="btn btn-primary" id="changePassword">{AuthMessage.ChangePassword.niceToString()}</button>
        </div>
      </div>
    </form>
  );
}
  
