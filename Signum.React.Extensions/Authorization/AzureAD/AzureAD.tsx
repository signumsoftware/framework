import * as React from "react";
import * as Msal from "msal";
import * as AppContext from "@framework/AppContext";
import * as AuthClient from "../AuthClient";
import { LoginContext } from "../Login/LoginPage";
import { LoginAuthMessage } from "../Signum.Entities.Authorization";

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

var msalConfig: Msal.Configuration = {

  auth: {
    clientId: window.__azureApplicationId!, //This is your client ID
    authority: "https://login.microsoftonline.com/" + window.__azureTenantId!, //This is your tenant info
    postLogoutRedirectUri: window.location.origin + AppContext.toAbsoluteUrl("~/"),
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: true
  }
};

var requestObj = {
  scopes: ["user.read"]
};

var myMSALObj = new Msal.UserAgentApplication(msalConfig);

export function signIn(ctx: LoginContext) {
  ctx.setLoading("azureAD");
  myMSALObj.loginPopup(requestObj)
    .then(a => {
      return AuthClient.API.loginWithAzureAD(a.idToken.rawIdToken, true);
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
  var rawIdToken = localStorage.getItem("msal.idtoken");
  if (!rawIdToken)
    return Promise.resolve(undefined);

  return AuthClient.API.loginWithAzureAD(rawIdToken, false);
}

export function signOut() {
  if (myMSALObj.getAccount())
    myMSALObj.logout();
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
