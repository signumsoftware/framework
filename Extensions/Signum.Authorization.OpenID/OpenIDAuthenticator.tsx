import * as React from 'react'
import { useLocation } from 'react-router'
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

    AuthClient.authenticators.push(loginWithOpenIDSilent);
  }

  // Points to the dedicated callback route, not the app root
  export function getRedirectUri(): string {
    return window.location.origin + AppContext.toAbsoluteUrl("/openid-callback");
  }

  export async function redirectToIdP(config: OpenIDConfig, returnUrl?: string): Promise<void> {
    const discoveryUrl = `${config.authority.replace(/\/$/, '')}/.well-known/openid-configuration`;
    const discovery = await fetch(discoveryUrl).then(r => r.json()) as { authorization_endpoint: string };

    const state = generateState();
    sessionStorage.setItem("openIDState", state);

    if (returnUrl)
      sessionStorage.setItem("openIDReturnUrl", returnUrl);
    else
      sessionStorage.removeItem("openIDReturnUrl");

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

  // Called by AuthClient.autoLogin on every page load.
  // If the user previously authenticated via OpenID, redirect them to the IdP silently.
  // The IdP session (e.g. Keycloak's 8h SSO session) makes this transparent when still valid.
  export async function loginWithOpenIDSilent(): Promise<AuthClient.API.LoginResponse | undefined> {
    if (location.search.includes("avoidOID"))
      return undefined;

    const config = Options.getOpenIDConfig();
    if (!config)
      return undefined;

    if (!localStorage.getItem("openIDActive"))
      return undefined;

    // Save the current deep-link so OpenIDRedirect can restore it after login
    const returnUrl = window.location.pathname + window.location.search + window.location.hash;
    await redirectToIdP(config, returnUrl);

    // Browser is navigating away — this promise intentionally never resolves
    return new Promise(() => { });
  }

  export function setOpenIDActive(active: boolean): void {
    if (active)
      localStorage.setItem("openIDActive", "1");
    else
      localStorage.removeItem("openIDActive");
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

  // When the router redirected us to /auth/login it put the original URL in location.state.back
  const loc = useLocation();
  const back = loc.state?.back as { pathname: string; search?: string; hash?: string } | undefined;

  function handleClick() {
    if (!config) return;
    const returnUrl = back
      ? back.pathname + (back.search ?? '') + (back.hash ?? '')
      : undefined;
    void OpenIDAuthenticator.redirectToIdP(config, returnUrl);
  }

  return (
    <div className="row mt-4">
      <div className="col-md-6 offset-md-3">
        <button
          type="button"
          className={`btn btn-primary w-100${ctx.loading != null ? " disabled" : ""}`}
          onClick={handleClick}
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
