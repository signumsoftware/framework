import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { LoginAuthMessage } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { RouteComponentProps } from 'react-router'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'

export default function ResetPassword(p: RouteComponentProps<{}>) {

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);

  const [success, setSuccess] = React.useState<boolean>(false);

  const newPassword = React.useRef<HTMLInputElement>(null);
  const newPassword2 = React.useRef<HTMLInputElement>(null);
  const code = String(QueryString.parse(p.location.search).code!);

  function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    setModelState({ ...validateNewPassword(false) }).then(ms => {

      if (ms && Dic.getValues(ms).some(array => array.length > 0))
        return;

      const request: AuthClient.API.ResetPasswordRequest = {
        code: code,
        newPassword: newPassword.current!.value,
      };

      AuthClient.API.resetPassword(request)
        .then(lr => {
          AuthClient.setAuthToken(lr.token, lr.authenticationType);
          AuthClient.setCurrentUser(lr.userEntity);

          setSuccess(true);
          //Navigator.resetUI();
          AppContext.history.push("~/auth/ResetPassword?code=OK");
        })
        .catch((e: ValidationError) => {
          if (e.modelState)
            setModelState(e.modelState).done();
        })
        .done();
    });
  }




  function handleNewPasswordBlur(event: React.SyntheticEvent<any>) {
    setModelState({ ...modelState, ...validateNewPassword(event.currentTarget == newPassword2.current) }).done();
  }

  function validateNewPassword(isSecond: boolean) {
    return {
      ["newPassword"]:
        !isSecond ? [] :
          !newPassword.current!.value && !newPassword2.current!.value ? [LoginAuthMessage .PasswordMustHaveAValue.niceToString()] :
            newPassword2.current!.value != newPassword.current!.value ? [LoginAuthMessage .PasswordsAreDifferent.niceToString()] :
              []
    };
  }

  function error(field: string): string | undefined {
    var ms = modelState;
    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  if (success || code == "OK") {
    return (
      <div>
        <h2 className="sf-entity-title">{LoginAuthMessage .PasswordChanged.niceToString()}</h2>
        <p>{LoginAuthMessage .PasswordHasBeenChangedSuccessfully.niceToString()}</p>
      </div>
    );
  }

  return (
    <form onSubmit={(e) => handleSubmit(e)}>
      <div className="row">
        <div className="offset-sm-2 col-sm-6">
          <h2 className="sf-entity-title">{LoginAuthMessage.ChangePassword.niceToString()}</h2>
          <p>{LoginAuthMessage .NewPassword.niceToString()}</p>
        </div>
      </div>
      <div>

        <div className={classes("form-group row", error("newPassword") && "has-error")}>
          <label className="col-form-label col-sm-2">{LoginAuthMessage .EnterTheNewPassword.niceToString()}</label>
          <div className="col-sm-4">
            <input type="password" className="form-control" id="newPassword" ref={newPassword} onBlur={handleNewPasswordBlur} />
            {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
          </div>
        </div>
        <div className={classes("form-group row", error("newPassword") && "has-error")}>
          <label className="col-form-label col-sm-2">{LoginAuthMessage .ConfirmNewPassword.niceToString()}</label>
          <div className="col-sm-4">
            <input type="password" className="form-control" id="newPassword2" ref={newPassword2} onBlur={handleNewPasswordBlur} />
            {error("newPassword") && <span className="help-block">{error("newPassword")}</span>}
          </div>
        </div>

      </div>
      <div className="row">
        <div className="offset-sm-2 col-sm-6">
          <button type="submit" className="btn btn-primary" id="changePassword">{LoginAuthMessage.ChangePassword.niceToString()}</button>
        </div>
      </div>
    </form>
  );
}
