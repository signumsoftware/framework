import * as React from "react";
import * as msal from "@azure/msal-browser";
import * as AppContext from "@framework/AppContext";
import * as Reflection from "@framework/Reflection";
import { AuthClient } from "../../Signum.Authorization/AuthClient";
import LoginPage, { LoginContext } from "../../Signum.Authorization/Login/LoginPage";
import { ExternalServiceError, ajaxPost } from "@framework/Services";
import { AuthMessage, LoginAuthMessage, ResetPasswordB2CMessage } from "../../Signum.Authorization/Signum.Authorization";
import { classes } from "@framework/Globals";
import { QueryString } from "@framework/QueryString";
import MessageModal, { MessageModalHandler } from "@framework/Modals/MessageModal";
import { AuthError } from "@azure/msal-browser";
import ErrorModal from "../../../Signum/React/Modals/ErrorModal";
import { JavascriptMessage } from "@framework/Signum.Entities";
import { LinkButton } from "@framework/Basics/LinkButton";

export namespace AzureADClient {

  export function registerAzureADAuthenticator(): void {

    if (Reflection.isStarted())
      throw new Error("call AzureADClient.registerWindowsAuthenticator in MainPublic.tsx before AuthClient.autoLogin");

    LoginPage.customLoginButtons = ctx =>
      <>
        {window.__azureADConfig?.loginWithAzureAD && <MicrosoftSignIn ctx={ctx} />}
        {window.__azureADConfig?.azureB2C && <AzureB2CSignIn ctx={ctx} />}
      </>;
    LoginPage.showLoginForm = "initially_not";
    AuthClient.authenticators.push(loginWithAzureADSilent);

    const config = window.__azureADConfig;

    var msalConfig: msal.Configuration = {
      auth: {
        clientId: config?.applicationId!, //This is your client ID
        //authority: config?.azureB2C ? getAzureB2C_Authority(config.azureB2C.signInSignUp_UserFlow!) : ("https://login.microsoftonline.com/" + config?.tenantId)!, //This is your tenant info
        redirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
        postLogoutRedirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
      },
      cache: {
        cacheLocation: "localStorage",
        storeAuthStateInCookie: true
      }
    };

    if (config?.azureB2C && config?.azureB2C.tenantName)
      msalConfig.auth.knownAuthorities = [
        config.loginWithAzureAD ? `login.microsoftonline.com` : null,
        `${config?.azureB2C.tenantName}.b2clogin.com`
      ].notNull();

    msalClient = new msal.PublicClientApplication(msalConfig);
  }

  /*     Add this to Index.cshtml
        var __azureApplicationId = @Json.Serialize(Starter.Configuration.Value.ActiveDirectory.Azure_ApplicationID);
        var __azureTenantId = @Json.Serialize(Starter.Configuration.Value.ActiveDirectory.Azure_DirectoryID);
  */


  let msalClient: msal.PublicClientApplication;

  export namespace Config {
    export let scopes: string[] = ["user.read"];
    export let scopesB2C: string[] = ["openid", "profile", "email"];
  }

  export type B2C_UserFlows = "signInSignUp_UserFlow" | "signIn_UserFlow" | "signUp_UserFlow" | "resetPassword_UserFlow";

  export function getAuthority(mode?: "B2C" | undefined, b2c_UserFlow?: B2C_UserFlows): string {
    const config = window.__azureADConfig;
    if (mode == undefined)
      return "https://login.microsoftonline.com/" + config!.tenantId;

    const confB2C = config!.azureB2C!;
    const userFlow = b2c_UserFlow ? confB2C[b2c_UserFlow]! : (confB2C.signInSignUp_UserFlow || confB2C.signIn_UserFlow!);

    const tenantName = confB2C.tenantName;
    return `https://${tenantName}.b2clogin.com/${tenantName}.onmicrosoft.com/${userFlow}`;
  }

  export async function signIn(ctx: LoginContext, b2c: boolean, b2c_UserFlow?: B2C_UserFlows): Promise<void> {
    ctx.setLoading(b2c ? "azureB2C" : "azureAD");

    const config = window.__azureADConfig;

    (msalClient as any).browserStorage.setInteractionInProgress(false); //Without this cancelling log-out makes log-in impossible without cleaning cookies and local storage

    try {
      const authResult = await msalClient.loginPopup({
        scopes: b2c ? Config.scopesB2C : Config.scopes,
        authority: getAuthority(b2c ? "B2C" : undefined, b2c_UserFlow),
      });
      setMsalAccount(authResult.account!.username, b2c);

      const loginResponse = await API.loginWithAzureAD(authResult.idToken, authResult.accessToken, { azureB2C: b2c, throwErrors: true })
      if (loginResponse == null)
        throw new Error("User " + authResult.account?.username + " not found in the database");

      AuthClient.setAuthToken(loginResponse!.token, loginResponse!.authenticationType);
      AuthClient.setCurrentUser(loginResponse!.userEntity);
      AuthClient.Options.onLogin();
    } catch (e) {
      ctx.setLoading(undefined);
      if (e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode == "user_cancelled"))
        return;

      if (e instanceof msal.AuthError && e.errorCode == "access_denied" && e.errorMessage.startsWith("AADB2C90091"))
        return;

      if (e instanceof msal.AuthError && e.errorCode == "access_denied" && e.errorMessage.startsWith("AADB2C90118")) {
        resetPasswordB2C(ctx)
        return;
      }

      ErrorModal.showErrorModal(e, () => signOut());
    }
  }

  export async function resetPasswordB2C(ctx: LoginContext): Promise<void> {
    ctx.setLoading("azureAD");

    var promise: Promise<void> | undefined;
    const modalRef = React.createRef<MessageModalHandler>();
    await MessageModal.show({ //The point of this modal is to avoid the browser blocking the popup
      modalRef: modalRef,
      title: ResetPasswordB2CMessage.ResetPasswordRequested.niceToString(),
      message: ResetPasswordB2CMessage.DoYouWantToContinue.niceToString(),
      buttonContent: a => a == "ok" ? ResetPasswordB2CMessage.ResetPassword.niceToString() : undefined,
      onButtonClicked: a => {
        if (a == "ok")
          promise = handleResetPAsswordClick();

        modalRef.current!.handleButtonClicked(a);
      },
      buttons: "ok_cancel"
    })

    await promise;

    ctx.setLoading(undefined);

    async function handleResetPAsswordClick() {
      const config = window.__azureADConfig;

      try {

        (msalClient as any).browserStorage.setInteractionInProgress(false); //Without this cancelling log-out makes log-in impossible without cleaning cookies and local storage

        const resetPasswordResult = await msalClient.loginPopup({
          scopes: Config.scopesB2C,
          authority: getAuthority("B2C", "resetPassword_UserFlow"),
        });

        await MessageModal.show({
          title: LoginAuthMessage.PasswordChanged.niceToString(),
          message: LoginAuthMessage.PasswordHasBeenChangedSuccessfully.niceToString(),
          buttons: "ok",
        });

        ctx.setLoading(undefined);

      } catch (e) {
        ctx.setLoading(undefined);
        if (e instanceof msal.InteractionRequiredAuthError ||
          e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode == "user_cancelled"))
          return Promise.resolve(undefined);

        ErrorModal.showErrorModal(e, () => signOut());
      }
    }
  }

  export function isB2C(): boolean {
    return localStorage.getItem("msalAccountIsBTC") == "true";
  }

  export function loginWithAzureADSilent(): Promise<AuthClient.API.LoginResponse | undefined> {

    if (location.search.contains("avoidAD") || window.__azureADConfig == null)
      return Promise.resolve(undefined);

    var ai = getMsalAccount();

    if (!ai)
      return Promise.resolve(undefined);

    const b2c = isB2C();

    var userRequest: msal.SilentRequest = {
      scopes: b2c ? Config.scopesB2C : Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
      account: ai,
      authority: getAuthority(b2c ? "B2C" : undefined)
    };

    return msalClient.acquireTokenSilent(userRequest)
      .then(res => {
        return API.loginWithAzureAD(res.idToken, res.accessToken, { throwErrors: false, azureB2C: b2c });
      }, e => {
        if (e instanceof msal.InteractionRequiredAuthError ||
          e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode == "user_cancelled"))
          return Promise.resolve(undefined);

        console.log(e);
        return Promise.resolve(undefined);
      });
  }

  export function cleantMsalAccount(): void {
    localStorage.removeItem("msalAccount");
    localStorage.removeItem("msalAccountIsBTC");
  }

  export function setMsalAccount(accountName: string, isB2C: boolean): void {
    localStorage.setItem("msalAccount", accountName);
    if (isB2C)
      localStorage.setItem("msalAccountIsBTC", isB2C.toString());
  }

  export function getMsalAccount(): msal.AccountInfo | null | undefined {
    let account = localStorage.getItem('msalAccount');
    if (!account || !msalClient)
      return null;

    return msalClient.getAccountByUsername(account) ?? undefined;
  }

  export async function getAccessToken(): Promise<string> {

    var ai = getMsalAccount();

    if (!ai)
      throw new Error('User account missing from session. Please sign out and sign in again.');

    var userRequest: msal.SilentRequest = {
      scopes: Config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
      account: ai,
      authority: getAuthority(isB2C() ? "B2C" : undefined),
    };

    const res = await adquireTokenSilentOrPopup(userRequest);
    return res.accessToken;
  }

  async function adquireTokenSilentOrPopup(userRequest: msal.SilentRequest) {
    try {
      return await msalClient.acquireTokenSilent(userRequest);
    } catch (e) {
      if (e instanceof AuthError &&
        (e.errorCode === "consent_required" ||
          e.errorCode === "interaction_required" ||
          e.errorCode === "login_required"))
        return msalClient.acquireTokenPopup(userRequest);

      else
        throw e;
    }
  }

  export async function signOut(): Promise<void> {
    var account = getMsalAccount();
    if (account) {
      const b2c = isB2C();
      await msalClient.logoutPopup({
        authority: getAuthority(b2c ? "B2C" : undefined),
        account: account
      });

      AzureADClient.cleantMsalAccount();
    }
  }

  export function MicrosoftSignIn({ ctx }: { ctx: LoginContext }): React.JSX.Element {
    return (
      <div className="row mt-2">
        <div className="col-md-6 offset-md-3">
          <LinkButton title={LoginAuthMessage.SignInWithMicrosoft.niceToString()} className={ctx.loading != null ? "disabled" : undefined} onClick={e => { signIn(ctx, false); }}>
            <img src={MicrosoftSignIn.iconUrl} alt={LoginAuthMessage.SignInWithMicrosoft.niceToString()} />
          </LinkButton>
        </div>
      </div>
    );
  }

  export function AzureB2CSignIn({ ctx }: { ctx: LoginContext }): React.JSX.Element {
    const config = window.__azureADConfig;
    const hasSignInFlow = Boolean(config?.azureB2C?.signIn_UserFlow);
    const hasSignUpFlow = Boolean(config?.azureB2C?.signUp_UserFlow);

    if (hasSignInFlow && hasSignUpFlow) {
      return (
        <div className="row mt-4">
          <div className="col-md-6 offset-md-3">
            <div className='hstack'>
              <div className=''>
                <button className={classes("btn btn-secondary me-2", ctx.loading != null ? "disabled" : undefined)} onClick={e => { AzureADClient.signIn(ctx, true, 'signIn_UserFlow'); }}>
                  {"Sign in with Azure B2C"}
                </button>
              </div>
              <div className=''>
                <button className={classes("btn btn-primary", ctx.loading != null ? "disabled" : undefined)} onClick={e => { AzureADClient.signIn(ctx, true, 'signUp_UserFlow'); }}>
                  {"Sign up with Azure B2C"}
                </button>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div className="row mt-4">
        <div className="col-md-6 offset-md-3">
          <button className={classes("btn btn-primary", ctx.loading != null ? "disabled" : undefined)} onClick={e => { AzureADClient.signIn(ctx, true); }}>
            {"Login with Azure B2C"}
          </button>
        </div>
      </div>
    );
  }

  export declare namespace MicrosoftSignIn {
    export var iconUrl: string;
  }

  MicrosoftSignIn.iconUrl = AppContext.toAbsoluteUrl("/signin_light.svg");

  export namespace API {
    export function loginWithAzureAD(jwt: string, accessToken: string, opts: { throwErrors: boolean, azureB2C: boolean }): Promise<AuthClient.API.LoginResponse | undefined> {
      return ajaxPost({
        url: "/api/auth/loginWithAzureAD?" + QueryString.stringify(opts), avoidAuthToken: true
      }, { idToken: jwt, accessToken });
    }
  }



}

declare global {
  interface Window {
    __azureADConfig?: AzureADConfig;
  }
}

interface AzureADConfig {
  loginWithAzureAD: boolean;
  applicationId: string;
  tenantId: string;
  azureB2C?: AzureB2CConfig;
}

interface AzureB2CConfig {
  tenantName: string;
  signInSignUp_UserFlow: string;
  signIn_UserFlow?: string;
  signUp_UserFlow?: string;
  resetPassword_UserFlow?: string;
}
