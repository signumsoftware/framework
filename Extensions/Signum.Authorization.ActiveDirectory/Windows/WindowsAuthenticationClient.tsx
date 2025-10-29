import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost } from '@framework/Services';
import * as Reflection from '@framework/Reflection';
import LoginPage from '../../Signum.Authorization/Login/LoginPage';
import { AuthClient } from '../../Signum.Authorization/AuthClient';
import { LoginAuthMessage } from '../../Signum.Authorization/Signum.Authorization';
import MessageModal from '@framework/Modals/MessageModal';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export namespace WindowsAuthenticationClient {
  
  /* Install and enable Windows authentication in IIS https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-2.2&tabs=visual-studio */
  
  export function registerWindowsAuthenticator(): void {
  
    if (Reflection.isStarted())
      throw new Error("call WindowsAuthenticationClient.registerWindowsAuthenticator in MainPublic.tsx before AuthClient.autoLogin");
  
    AuthClient.authenticators.push(loginWindowsAuthentication);
    AuthClient.Options.AuthHeader = "Signum_Authorization"; //Authorization is used by IIS with Negotiate prefix
    LoginPage.showLoginForm = "initially_not";
    LoginPage.customLoginButtons = () => <LoginWithWindowsButton />;
  }
  
  
  export function loginWindowsAuthentication(): Promise<AuthClient.AuthenticatedUser | undefined> {
  
    if (AuthClient.Options.disableWindowsAuthentication)
      return Promise.resolve(undefined);
  
    return API.loginWindowsAuthentication(false).then(au => {
      au && console.log("loginWindowsAuthentication");
      return au;
    }).catch(() => undefined);
  }
  
  export namespace API {
  
    export function loginWindowsAuthentication( throwError : boolean): Promise<AuthClient.API.LoginResponse | undefined> {
      return ajaxPost({ url: `/api/auth/loginWindowsAuthentication?throwError=${throwError}`, avoidAuthToken: true }, undefined);
    }
  }
  
  
  export function LoginWithWindowsButton(): React.JSX.Element {
  
    function onClick() {
      return API.loginWindowsAuthentication(true)
        .then(lr => {
          if (lr == null) {
            MessageModal.showError(LoginAuthMessage.LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication.niceToString(), LoginAuthMessage.NoWindowsUserFound.niceToString());
          } else {
            AuthClient.setAuthToken(lr.token, lr.authenticationType);
            AuthClient.setCurrentUser(lr.userEntity);
            AuthClient.Options.onLogin();
          }
        });
    }
  
    return (
      <div className="row mt-2">
        <div className="col-md-6 offset-md-3">
          <button onClick={e => { onClick(); }} className="btn btn-info">
            <FontAwesomeIcon aria-hidden={true} icon={["fab", "windows"]} /> {LoginAuthMessage.LoginWithWindowsUser.niceToString()}
          </button>
        </div>
      </div>
    );
  }
}
