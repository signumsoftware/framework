import * as React from "react";
import * as msal from "@azure/msal-browser";
import * as AppContext from "@framework/AppContext";
import * as AuthClient from "../AuthClient";
import { LoginContext } from "../Login/LoginPage";

/*     Add this to Index.cshtml
       var __azureApplicationId = @Json.Serialize(Starter.Configuration.Value.ActiveDirectory.Azure_ApplicationID);
       var __azureTenantId = @Json.Serialize(Starter.Configuration.Value.ActiveDirectory.Azure_DirectoryID);
 * */

declare global {
  interface Window {
    __azureApplicationId: string | null;
    __azureTenantId: string | null;
  }
}

var msalConfig: msal.Configuration = {
  auth: {
    clientId: window.__azureApplicationId!, //This is your client ID
    authority: "https://login.microsoftonline.com/" + window.__azureTenantId!, //This is your tenant info
    redirectUri: window.location.origin + AppContext.toAbsoluteUrl("~/"),
    postLogoutRedirectUri: window.location.origin + AppContext.toAbsoluteUrl("~/"),
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: true
  }
};

var msalClient = new msal.PublicClientApplication(msalConfig);

export namespace Config {
  export let scopes = ["user.read"];
}


export function signIn(ctx: LoginContext) {
  ctx.setLoading("azureAD");

  var userRequest: msal.PopupRequest = {
    scopes: Config.scopes,
  };

  msalClient.loginPopup(userRequest)
    .then(a => {
      localStorage.setItem('msalAccount', a.account!.username);
      return AuthClient.API.loginWithAzureAD(a.idToken, true);
    }).then(r => {
      AuthClient.setAuthToken(r!.token, r!.authenticationType);
      AuthClient.setCurrentUser(r!.userEntity);
      AuthClient.Options.onLogin()
    })
    .catch(e => {
      ctx.setLoading(undefined);

      if (e && e.name == "ClientAuthError" && e.errorCode == "user_cancelled")
        return;

      throw e;
    })
    .done();
}

export function loginWithAzureAD(): Promise<AuthClient.API.LoginResponse | undefined> {

  if (location.search.contains("avoidAD"))
    return Promise.resolve(undefined);

  var ai = getAccountInfo();

  if (!ai)
    return Promise.resolve(undefined);

  var userRequest: msal.SilentRequest = {
    scopes: Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
    account: ai,
  };

  return msalClient.acquireTokenSilent(userRequest).then(res => {
    const rawIdToken = res.idToken;

    return AuthClient.API.loginWithAzureAD(rawIdToken, false);
  }, e => {
    if (e instanceof msal.InteractionRequiredAuthError || e instanceof msal.BrowserAuthError && e.errorCode == "user_login_error")
      return Promise.resolve(undefined);

    console.log(e);
    return Promise.resolve(undefined);
  });
}

export function getAccountInfo() {
  let account = localStorage.getItem('msalAccount');
  if (!account)
    return null;

  return msalClient.getAccountByUsername(account) ?? undefined;
}

export function getAccessToken() {

  var ai = getAccountInfo();

  if (!ai)
    throw new Error('User account missing from session. Please sign out and sign in again.');

  var userRequest: msal.SilentRequest = {
    scopes: Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
    account: ai,
  };

  return msalClient.acquireTokenSilent(userRequest).then(res => res.accessToken);
}

export function signOut() {
  var account = getAccountInfo();
  if (account) {
    msalClient.logout({
      account: account
    }).done();
  }
}

export function MicrosoftSignIn({ ctx }: { ctx: LoginContext }) {
  return (
    <div className="row mt-2">
      <div className="col-md-6 offset-md-3">
        <a href="#" className={ctx.loading != null ? "disabled" : undefined} onClick={e => { e.preventDefault(); signIn(ctx); }}>
          <img src={MicrosoftSignIn.iconUrl} />
        </a>
      </div>
    </div>
  );
}

MicrosoftSignIn.iconUrl = AppContext.toAbsoluteUrl("~/signin_light.png");
