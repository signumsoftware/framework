import * as React from "react";
import * as msal from "@azure/msal-browser";
import * as AppContext from "@framework/AppContext";
import * as Reflection from "@framework/Reflection";
import { ExternalServiceError, ajaxPost } from "@framework/Services";
import { classes } from "@framework/Globals";
import { QueryString } from "@framework/QueryString";
import MessageModal, { MessageModalHandler } from "@framework/Modals/MessageModal";
import { AuthError } from "@azure/msal-browser";
import { JavascriptMessage } from "@framework/Signum.Entities";
import { LinkButton } from "@framework/Basics/LinkButton";
import LoginPage, { LoginContext } from "../Signum.Authorization/Login/LoginPage";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import ErrorModal from "@framework/Modals/ErrorModal";
import { LoginAuthMessage, ResetPasswordB2CMessage } from "../Signum.Authorization/Signum.Authorization";
import { AzureADType } from "./Signum.Authorization.AzureAD";

export namespace AzureADAuthenticator {


  /*     Add this to Index.cshtml
        var __azureADConfig = @Json.Serialize(Starter.Configuration.Value.ActiveDirectory?.ToAzureADConfigTS());
  */
  //override if needed
  export namespace Options {
    export let getAzureADConfig = function (adVariant: string): AzureADConfig | undefined {
      return window.__azureADConfig;
    }
  }
  

  export function registerAzureADAuthenticator(): void {

    if (Reflection.isStarted())
      throw new Error("call AzureADClient.registerAzureADAuthenticator in MainPublic.tsx before AuthClient.autoLogin");

    LoginPage.customLoginButtons = ctx => {

      const config = Options.getAzureADConfig("default");
      if (!config)
        return null;

      if (config.type == "AzureAD")
        return <MicrosoftSignIn ctx={ctx} />;

      if (config.type == "B2C")
        return <AzureB2CSignIn ctx={ctx} />;

      if (config.type == "ExternalID")
        return <AzureB2CSignIn ctx={ctx} />;

      return null;
    };

    LoginPage.showLoginForm = "initially_not";

    var config = getCurrentADConfig();

    currentMsalClient = config ? getMsalClient(config) : null;

    AuthClient.authenticators.push(loginWithAzureADSilent);

  }

  let currentMsalClient: msal.PublicClientApplication | null = null;
  function getMsalClient(config: AzureADConfig): msal.PublicClientApplication {


    var msalConfig: msal.Configuration = {
      auth: {
        clientId: config?.applicationId!, //This is your client ID
        redirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
        postLogoutRedirectUri: window.location.origin + AppContext.toAbsoluteUrl("/"),
      },
      cache: {
        cacheLocation: "localStorage",
        storeAuthStateInCookie: true
      }
    };

    if (config.type == "B2C")
      msalConfig.auth.knownAuthorities = [`${config.tenantName}.b2clogin.com`];
    else if (config.type == "ExternalID")
      msalConfig.auth.knownAuthorities = [config.tenantName];
  
    return new msal.PublicClientApplication(msalConfig);
  }

  export type B2C_UserFlows = "signInSignUp_UserFlow" | "signIn_UserFlow" | "signUp_UserFlow" | "resetPassword_UserFlow" | "editProfile_UserFlow";

  export function getAuthority(config: AzureADConfig, b2c_UserFlow?: B2C_UserFlows): string {
    if (config.type == "AzureAD")
      return "https://login.microsoftonline.com/" + config!.tenantId;

    if (config.type == "ExternalID")
      return config.signInSignUp_UserFlow!; // Is a Url

    if (config.type == "B2C") {
      const userFlow = b2c_UserFlow ? config[b2c_UserFlow]! : (config.signInSignUp_UserFlow || config.signIn_UserFlow!);

      return `https://${config.tenantName}.b2clogin.com/${config.tenantName}.onmicrosoft.com/${userFlow}`;
    }

    throw new Error("Unexpected AzureAD type");
  }

  export async function signIn(ctx: LoginContext, adVariant: string, b2c_UserFlow?: B2C_UserFlows, e?: React.MouseEvent): Promise<void> {

    e?.preventDefault();

    ctx.setLoading(adVariant);

    const config = Options.getAzureADConfig(adVariant)!;

    let newClient = getMsalClient(config);

    (newClient as any).browserStorage.setInteractionInProgress(false); //Without this cancelling log-out makes log-in impossible without cleaning cookies and local storage

    try {
      const authResult = await newClient.loginPopup({
        scopes: config.scopes,
        prompt: e?.shiftKey || e?.altKey ?  "select_account" : undefined,
        authority: getAuthority(config, b2c_UserFlow),
      });
      setMsalAccount(authResult.account!.username, adVariant);

      const loginResponse = await API.loginWithAzureAD(authResult.idToken, authResult.accessToken, { adVariant: adVariant, throwErrors: true })
      if (loginResponse == null)
        throw new Error("User " + authResult.account?.username + " not found in the database");

      currentMsalClient = newClient;
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
        resetPasswordB2C(ctx, adVariant)
        return;
      }

      ErrorModal.showErrorModal(e, () => signOut());
    }
  }

  export async function resetPasswordB2C(ctx: LoginContext, adVariant: string): Promise<void> {
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
          promise = handleResetPAsswordClick(adVariant);

        modalRef.current!.handleButtonClicked(a);
      },
      buttons: "ok_cancel"
    })

    await promise;

    ctx.setLoading(undefined);

    async function handleResetPAsswordClick(adVariant: string): Promise<void> {
      const config = Options.getAzureADConfig(adVariant)!;
      let newClient = getMsalClient(config);

      try {

        (newClient as any).browserStorage.setInteractionInProgress(false); //Without this cancelling log-out makes log-in impossible without cleaning cookies and local storage

        const resetPasswordResult = await newClient.loginPopup({
          scopes: config?.scopes,
          authority: getAuthority(config, "resetPassword_UserFlow"),
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

  export async function loginWithAzureADSilent(): Promise<AuthClient.API.LoginResponse | undefined> {

    if (location.search.contains("avoidAD"))
      return Promise.resolve(undefined);

   
    let account = localStorage.getItem('msalAccount');
    
    if (!account)
      return undefined;
    
    var adVariant = getCurrentADVariant() ?? "default";
    var config = getCurrentADConfig()!;
    var newClient = getMsalClient(config);

    var ai = newClient.getAccountByUsername(account) ?? undefined;

    var userRequest: msal.SilentRequest = {
      scopes: config.scopes, 
      account: ai,
      authority: getAuthority(config)
    };

    try {

      var tokenResponse = await newClient.acquireTokenSilent(userRequest);
      currentMsalClient = newClient;
      var loginResp = await API.loginWithAzureAD(tokenResponse.idToken, tokenResponse.accessToken, { throwErrors: false, adVariant: adVariant});
      return loginResp;
    } catch (e) {
      if (e instanceof msal.InteractionRequiredAuthError ||
        e instanceof msal.BrowserAuthError && (e.errorCode == "user_login_error" || e.errorCode == "user_cancelled"))
        return undefined;

      console.log(e);
      return undefined;
    }
  }

  export function cleantMsalAccount(): void {

    localStorage.removeItem("msalAccount");
    localStorage.removeItem("msalAdVariant");
  }

  export function setMsalAccount(accountName: string, adVariant: string): void {
    localStorage.setItem("msalAccount", accountName);
    localStorage.setItem("msalAdVariant", adVariant);
  }

  export function getCurrentMsalAccount(): msal.AccountInfo | null | undefined {
    let account = localStorage.getItem('msalAccount');
    if (!account || !currentMsalClient)
      return null;

    return currentMsalClient.getAccountByUsername(account) ?? undefined;
  }

  export function getCurrentADVariant(): string | null {
    var result = localStorage.getItem("msalAdVariant");
    return result;
  }

  export function getCurrentADConfig(): AzureADConfig | undefined {
    let adVariant = getCurrentADVariant();
    let config = Options.getAzureADConfig(adVariant ?? "default");
    return config;
  }

  export async function getAccessToken(): Promise<string> {

    const ai = getCurrentMsalAccount();

    if (!ai)
      throw new Error('User account missing from session. Please sign out and sign in again.');

    var config = getCurrentADConfig()!;

    var userRequest: msal.SilentRequest = {
      scopes: config.scopes, // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/1246
      account: ai,
      authority: getAuthority(config, undefined),
    };

    const res = await adquireTokenSilentOrPopup(userRequest);
    return res.accessToken;
  }

  async function adquireTokenSilentOrPopup(userRequest: msal.SilentRequest) {
    try {
      return await currentMsalClient!.acquireTokenSilent(userRequest);
    } catch (e) {
      if (e instanceof AuthError &&
        (e.errorCode === "consent_required" ||
          e.errorCode === "interaction_required" ||
          e.errorCode === "login_required"))
        return currentMsalClient!.acquireTokenPopup(userRequest);

      else
        throw e;
    }
  }

  export async function signOut(): Promise<void> {
    var account = getCurrentMsalAccount();
    var config = getCurrentADConfig();
    if (account && config && currentMsalClient) {
      await currentMsalClient!.logoutPopup({
        authority: getAuthority(config),
        account: account
      });
      currentMsalClient.setActiveAccount(null);
      currentMsalClient = null;
      AzureADAuthenticator.cleantMsalAccount();
    }
  }

  export namespace API {
    export function loginWithAzureAD(jwt: string, accessToken: string, opts: { throwErrors: boolean, adVariant: string | null }): Promise<AuthClient.API.LoginResponse | undefined> {
      return ajaxPost({
        url: "/api/auth/loginWithAzureAD?" + QueryString.stringify(opts), avoidAuthToken: true
      }, { idToken: jwt, accessToken });
    }
  }
}

export declare namespace MicrosoftSignIn {
  export var iconUrl: string;
}

MicrosoftSignIn.iconUrl = AppContext.toAbsoluteUrl("/signin_light.svg");

export function MicrosoftSignIn({ ctx, adVariant = "default" }: { ctx: LoginContext, adVariant?: string }): React.JSX.Element {
  return (
    <div className="row mt-2">
      <div className="col-md-6 offset-md-3">
        <LinkButton title={LoginAuthMessage.SignInWithMicrosoft.niceToString()} className={ctx.loading != null ? "disabled" : undefined}
          onClick={e => { AzureADAuthenticator.signIn(ctx, adVariant, undefined, e); }}>
          <img src={MicrosoftSignIn.iconUrl} alt={LoginAuthMessage.SignInWithMicrosoft.niceToString()} />
        </LinkButton>
      </div>
    </div>
  );
}

export function AzureB2CSignIn({ ctx, adVariant = "default" }: { ctx: LoginContext, adVariant?: string }): React.JSX.Element {
  const config = AzureADAuthenticator.Options.getAzureADConfig(adVariant);
  const hasSignInFlow = Boolean(config?.signIn_UserFlow);
  const hasSignUpFlow = Boolean(config?.signUp_UserFlow);

  if (hasSignInFlow && hasSignUpFlow) {
    return (
      <div className="row mt-4">
        <div className="col-md-6 offset-md-3">
          <div className='hstack'>
            <div className=''>
              <button type="button" className={classes("btn btn-secondary me-2", ctx.loading != null ? "disabled" : undefined)}
                onClick={e => { AzureADAuthenticator.signIn(ctx, adVariant, 'signIn_UserFlow', e); }}>
                {LoginAuthMessage.SignInWithAzureB2C.niceToString()}
              </button>
            </div>
            <div className=''>
              <button type="button" className={classes("btn btn-primary", ctx.loading != null ? "disabled" : undefined)}
                onClick={e => { AzureADAuthenticator.signIn(ctx, adVariant, 'signUp_UserFlow', e); }}>
                {LoginAuthMessage.SignUpWithAzureB2C.niceToString()}
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
        <button type="button" className={classes("btn btn-primary", ctx.loading != null ? "disabled" : undefined)} onClick={e => { AzureADAuthenticator.signIn(ctx, adVariant ?? null, undefined, e); }}>
          {LoginAuthMessage.LoginWithAzureB2C.niceToString()}
        </button>
      </div>
    </div>
  );
}


declare global {
  interface Window {
    __azureADConfig?: AzureADConfig; //Default case
  }
}

export interface AzureADConfig {
  type: AzureADType
  applicationId: string;
  tenantId: string;
  tenantName: string;
  signInSignUp_UserFlow: string;
  signIn_UserFlow?: string;
  signUp_UserFlow?: string;
  editProfile_UserFlow?: string;
  resetPassword_UserFlow?: string;
  scopes: string[];
}
