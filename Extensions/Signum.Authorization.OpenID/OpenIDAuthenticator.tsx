import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { ajaxPost } from '@framework/Services'
import { QueryString } from '@framework/QueryString'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import LoginPage, { LoginContext } from '../Signum.Authorization/Login/LoginPage'
import { OpenIDMessage } from './Signum.Authorization.OpenID'

export namespace OpenIDAuthenticator {

  /*
    Add this to Index.cshtml:
    var __openIDConfig = @Json.Serialize(Starter.Configuration.Value.OpenID?.ToOpenIDConfigTS());
  */

  export namespace Options {
    export let getOpenIDConfig = function (): OpenIDConfig | undefined {
      return window.__openIDConfig;
    }
  }

  export function registerOpenIDAuthenticator(buttonContent?: React.ReactNode): void {
    LoginPage.customLoginButtons = ctx => {
      const config = Options.getOpenIDConfig();
      if (!config)
        return null;

      return <OpenIDSignIn ctx={ctx} buttonContent={buttonContent} />;
    };

    LoginPage.showLoginForm = "initially_not";

    AuthClient.authenticators.push(loginWithOpenIDCallback);
  }

  export function getRedirectUri(): string {
    return window.location.origin + AppContext.toAbsoluteUrl("/");
  }

  export async function redirectToIdP(config: OpenIDConfig): Promise<void> {
    const discoveryUrl = `${config.authority.replace(/\/$/, '')}/.well-known/openid-configuration`;
    const discovery = await fetch(discoveryUrl).then(r => r.json()) as { authorization_endpoint: string };

    const state = generateState();
    sessionStorage.setItem("openIDState", state);

    const params = new URLSearchParams({
      response_type: "code",
      client_id: config.clientId,
      redirect_uri: getRedirectUri(),
      scope: config.scopes.join(" "),
      state,
    });

    window.location.href = `${discovery.authorization_endpoint}?${params.toString()}`;
  }

  function generateState(): string {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return Array.from(array, b => b.toString(16).padStart(2, "0")).join("");
  }

  export async function loginWithOpenIDCallback(): Promise<AuthClient.API.LoginResponse | undefined> {
    const qs = QueryString.parse(window.location.search);
    const code = qs["code"] as string | undefined;
    const state = qs["state"] as string | undefined;

    if (!code || !state)
      return undefined;

    const storedState = sessionStorage.getItem("openIDState");
    sessionStorage.removeItem("openIDState");

    if (state !== storedState)
      return undefined;

    // Remove code/state from URL without reloading
    const clean = window.location.origin + window.location.pathname;
    window.history.replaceState({}, document.title, clean);

    return API.loginWithOpenID(code, getRedirectUri(), { throwErrors: true });
  }

  export namespace API {
    export function loginWithOpenID(code: string, redirectUri: string, opts: { throwErrors: boolean }): Promise<AuthClient.API.LoginResponse | undefined> {
      return ajaxPost({
        url: "/api/auth/loginWithOpenID?" + QueryString.stringify(opts),
        avoidAuthToken: true
      }, { code, redirectUri });
    }
  }
}

export function OpenIDSignIn({ ctx, buttonContent }: {
  ctx: LoginContext;
  buttonContent?: React.ReactNode;
}): React.JSX.Element {
  const config = OpenIDAuthenticator.Options.getOpenIDConfig();

  return (
    <div className="row mt-4">
      <div className="col-md-6 offset-md-3">
        <button
          type="button"
          className={`btn btn-primary w-100${ctx.loading != null ? " disabled" : ""}`}
          onClick={() => config && void OpenIDAuthenticator.redirectToIdP(config)}
        >
          {buttonContent ?? OpenIDMessage.SignInWithOpenID.niceToString()}
        </button>
      </div>
    </div>
  );
}

declare global {
  interface Window {
    __openIDConfig?: OpenIDConfig;
  }
}

export interface OpenIDConfig {
  authority: string;
  clientId: string;
  scopes: string[];
}
