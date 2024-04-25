import * as React from "react";
import * as msal from "@azure/msal-browser";
import * as AppContext from "@framework/AppContext";
import * as Reflection from "@framework/Reflection";
import { AuthClient } from "../../Signum.Authorization/AuthClient";
import LoginPage, { LoginContext } from "../../Signum.Authorization/Login/LoginPage";
import { ExternalServiceError, ajaxPost } from "@framework/Services";
import { LoginAuthMessage } from "../../Signum.Authorization/Signum.Authorization";

export namespace AzureADClient {
  
  export function registerAzureADAuthenticator() {
  
    if (Reflection.isStarted())
      throw new Error("call AzureADClient.registerWindowsAuthenticator in MainPublic.tsx before AuthClient.autoLogin");
  
    LoginPage.customLoginButtons = ctx =>
      <>
        <MicrosoftSignIn ctx={ctx} />
      </>;
    LoginPage.showLoginForm = "initially_not";
    AuthClient.authenticators.push(loginWithAzureAD);

    var msalConfig: msal.Configuration = {
      auth: {
        clientId: window.__azureApplicationId!, //This is your client ID
        authority: window.__azureB2CAuthority ?? ("https://login.microsoftonline.com/" + window.__azureTenantId)!, //This is your tenant info
        redirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
        knownAuthorities: window.__azureB2CAuthorityDomain ? [window.__azureB2CAuthorityDomain] : undefined,
        postLogoutRedirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
      },
      cache: {
        cacheLocation: "localStorage",
        storeAuthStateInCookie: true
      }
    };

    msalClient = new msal.PublicClientApplication(msalConfig);
  }
  
  
  /*     Add this to Index.cshtml
         var __azureApplicationId = @Json.Serialize(TenantLogic.GetCurrentTenant()!.ActiveDirectoryConfiguration.Azure_ApplicationID);
         var __azureTenantId = @Json.Serialize(TenantLogic.GetCurrentTenant()!.ActiveDirectoryConfiguration.Azure_DirectoryID);
  */
  

  
 
  
  let msalClient: msal.PublicClientApplication;
  
  export namespace Config {
    export let scopes = ["user.read"];
  }
  
  
  export function signIn(ctx: LoginContext) {
    ctx.setLoading("azureAD");
  
    var userRequest: msal.PopupRequest = {
      scopes: Config.scopes,
    };
  
    (msalClient as any).browserStorage.setInteractionInProgress(false); //Without this cancelling log-out makes log-in impossible without cleaning cookies and local storage
    msalClient.loginPopup(userRequest)
      .then(a => {
        return API.loginWithAzureAD(a.idToken, a.accessToken, true)
          .then(r => {
            if (r == null)
              throw new Error("User " + a.account?.username + " not found in the database");
  
            AuthClient.setAuthToken(r!.token, r!.authenticationType);
            AuthClient.setCurrentUser(r!.userEntity);
            AuthClient.Options.onLogin();
            setMsalAccount(a.account!.username);
          })
      })
      .catch(e => {
        debugger;
        ctx.setLoading(undefined);
        if (e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode == "user_cancelled"))
          return;
  
        //if (e instanceof msal.AuthError)
        //  throw new ExternalServiceError("MSAL", e, e.name + ": " + e.errorCode, e.errorMessage, e.subError + "\n" + e.stack);
  
        throw e;
      });
  }
  
  export function loginWithAzureAD(): Promise<AuthClient.API.LoginResponse | undefined> {
  
    if (location.search.contains("avoidAD"))
      return Promise.resolve(undefined);
  
    var ai = getMsalAccount();
  
    if (!ai)
      return Promise.resolve(undefined);
  
    var userRequest: msal.SilentRequest = {
      scopes: Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
      account: ai,
    };
  
    return msalClient.acquireTokenSilent(userRequest)
      .then(res => {
        return API.loginWithAzureAD(res.idToken, res.accessToken, false);
      }, e => {
        if (e instanceof msal.InteractionRequiredAuthError ||
          e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode =="user_cancelled"))
          return Promise.resolve(undefined);
  
        console.log(e);
        return Promise.resolve(undefined);
      });
  }
  
  export function setMsalAccount(accountName: string | null) {
    if (accountName == null)
      localStorage.removeItem("msalAccount");
    else
      localStorage.setItem("msalAccount", accountName);
  }
  
  export function getMsalAccount() {
    let account = localStorage.getItem('msalAccount');
    if (!account)
      return null;
  
    return msalClient.getAccountByUsername(account) ?? undefined;
  }
  
  export function getAccessToken(): Promise<string>{
  
    var ai = getMsalAccount();
  
    if (!ai)
      throw new Error('User account missing from session. Please sign out and sign in again.');
  
    var userRequest: msal.SilentRequest = {
      scopes: Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
      account: ai,
    };
  
    return adquireTokenSilentOrPopup(userRequest)
      .then(res => res.accessToken);
  }
  
  function adquireTokenSilentOrPopup(userRequest: msal.SilentRequest) {
      return msalClient.acquireTokenSilent(userRequest)
          .catch(e => {
              if (e.errorCode === "consent_required"
                  || e.errorCode === "interaction_required"
                  || e.errorCode === "login_required")
                  return msalClient.acquireTokenPopup(userRequest);
  
              else
                  throw e;
          });
  }
  
  export function signOut() {
    var account = getMsalAccount();
    if (account) {
      msalClient.logout({
        account: account
      });
    }
  }
  
  export function MicrosoftSignIn({ ctx }: { ctx: LoginContext }) {
    return (
      <div className="row mt-2">
        <div className="col-md-6 offset-md-3">
          <a href="#" className={ctx.loading != null ? "disabled" : undefined} onClick={e => { e.preventDefault(); signIn(ctx); }}>
            <img src={MicrosoftSignIn.iconUrl} alt={LoginAuthMessage.SignInWithMicrosoft.niceToString()} title={LoginAuthMessage.SignInWithMicrosoft.niceToString()} />
          </a>
        </div>
      </div>
    );
  }
  
  MicrosoftSignIn.iconUrl = AppContext.toAbsoluteUrl("/signin_light.svg");
  
  export module API {

    export function loginWithAzureAD(jwt: string, accessToken: string, throwErrors: boolean): Promise<AuthClient.API.LoginResponse | undefined> {
      return ajaxPost({ url: "/api/auth/loginWithAzureAD?throwErrors=" + throwErrors, avoidAuthToken: true }, { idToken: jwt, accessToken });
    }
  }
}

declare global {
  interface Window {
    __azureApplicationId: string | null;
    __azureTenantId: string | null;

    __azureB2CAuthority: string | null;
    __azureB2CAuthorityDomain: string | null;
    __azureB2CTenantName: string | null;
  }
}
