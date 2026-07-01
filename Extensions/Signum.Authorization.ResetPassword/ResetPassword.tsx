import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { ModelState } from '@framework/Signum.Entities'
import { ValidationError } from '@framework/Services'
import { ResetPasswordClient } from './ResetPasswordClient'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { useLocation, useParams } from 'react-router'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { LoginAuthMessage } from '../Signum.Authorization/Signum.Authorization'
import { ResetPasswordAuthMessage } from './Signum.Authorization.ResetPassword'
 
export default function ResetPassword(): React.JSX.Element {
  const location = useLocation();

  const [modelState, setModelState] = useStateWithPromise<ModelState | undefined>(undefined);

  const [success, setSuccess] = React.useState<boolean>(false);
  const [successRequestNewLink, setSuccessRquestNewLink] = React.useState<boolean>(false);
  const [showRequestNewLink, setShowRequestNewLink] = React.useState<boolean>(false);

  const newPassword = React.useRef<HTMLInputElement>(null);
  const newPassword2 = React.useRef<HTMLInputElement>(null);
  const code = String(QueryString.parse(location.search).code!);

  async function handleSubmit(e: React.FormEvent<any>) {

    e.preventDefault();

    if (validateNewPassword()) {

      try {
        const request: ResetPasswordClient.API.ResetPasswordRequest = {
          code: code,
          newPassword: newPassword.current!.value,
        };

        var lr = await ResetPasswordClient.API.resetPassword(request);

        AuthClient.setAuthToken(lr.token, lr.authenticationType);
        AuthClient.setCurrentUser(lr.userEntity);
        AuthClient.Options.onLogin("/auth/ResetPassword?code=OK");

        setSuccess(true);
        //Navigator.resetUI();
        //AppContext.navigate("/auth/ResetPassword?code=OK");
      } catch (ex) {
        if (ex instanceof ValidationError) {
          if (ex.modelState)
            setModelState(ex.modelState);
        }
        else
          setShowRequestNewLink(true);

        throw ex;
      }
    }
  }

  function validateNewPassword(): boolean {
    if (!newPassword.current!.value) {
      setModelState({ "newPassword": [LoginAuthMessage.PasswordMustHaveAValue.niceToString()] });
      return false;
    }
    if (!newPassword2.current!.value) {
      setModelState({ "newPassword2": [LoginAuthMessage.PasswordMustHaveAValue.niceToString()] });
      return false;
    }
    else if (newPassword2.current!.value != null && newPassword2.current!.value != newPassword.current!.value) {
      setModelState({
        "newPassword": [LoginAuthMessage.PasswordsAreDifferent.niceToString()],
        "newPassword2": [LoginAuthMessage.PasswordsAreDifferent.niceToString()]
      });
      return false;
    } 
    setModelState({});
    return true;
  }

  function error(field: string): string | undefined {
    var ms = modelState;
    return ms && ms[field] && ms[field].length > 0 ? ms[field][0] : undefined;
  }

  if (successRequestNewLink) {
    return (
      <div className="container sf-request-new-link">
        <div className="row">
          <div className="col-md-6 offset-md-3">
            <h2 className="sf-entity-title">{ResetPasswordAuthMessage.RequestNewLink.niceToString()}</h2>
            <p>{ResetPasswordAuthMessage.NewLinkToResetPasswordHasBeenSentSuccessfully.niceToString()}</p>
          </div>
        </div>
      </div>
    );
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
                <input type="password" className="form-control" id="newPassword" ref={newPassword} onBlur={validateNewPassword} placeholder={LoginAuthMessage.EnterTheNewPassword.niceToString()} />
                {error("newPassword") && <span className="help-block text-danger">{error("newPassword")}</span>}
              </div>
              <div className={classes("form-group mb-3", error("newPassword") && "has-error")}>
                <input type="password" className="form-control" id="newPassword2" ref={newPassword2} onBlur={validateNewPassword} placeholder={LoginAuthMessage.ConfirmNewPassword.niceToString()} />
                {error("newPassword") && <span className="help-block text-danger">{error("newPassword2")}</span>}
              </div>

            </div>
            <div className="d-flex">
              <button type="submit" className="btn btn-primary" id="changePassword">{LoginAuthMessage.ChangePassword.niceToString()}</button>
              {showRequestNewLink &&
                <button className="btn btn-secondary ms-auto" id="requestNewLink" onClick={handleRequestNewLinkClick}>{ResetPasswordAuthMessage.RequestNewLink.niceToString()}</button>}
            </div>
          </form>
        </div>
      </div>
    </div>
  );

  function handleRequestNewLinkClick(e: React.MouseEvent<any>) {
    e.preventDefault();
    ResetPasswordClient.API.requestNewLink(code)
      .then(() => setSuccessRquestNewLink(true));
  }
}
