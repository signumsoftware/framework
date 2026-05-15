import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { OpenIDAuthenticator } from './OpenIDAuthenticator'

// Dedicated callback page for the OpenID Connect authorization code flow.
// Keycloak / Dex redirects here after authentication with ?code=...&state=...
// Registered as the route /openid-callback in OpenIDClient.startPublic().
export default function OpenIDCallback(): React.JSX.Element {

  React.useEffect(() => {
    handleCallback();
  }, []);

  async function handleCallback(): Promise<void> {
    console.log("openid-callback");
    const qs = QueryString.parse(window.location.search);
    const code = qs["code"] as string | undefined;
    const state = qs["state"] as string | undefined;

    const storedState = sessionStorage.getItem("openIDState");
    sessionStorage.removeItem("openIDState");

    const returnUrl = sessionStorage.getItem("openIDReturnUrl") ?? undefined;
    sessionStorage.removeItem("openIDReturnUrl");

    if (!code || !state || state !== storedState) {
      AppContext.navigate("/");
      return;
    }

    try {
      const loginResponse = await OpenIDAuthenticator.API.loginWithOpenID(
        code,
        OpenIDAuthenticator.getRedirectUri(),
        { throwErrors: true }
      );

      OpenIDAuthenticator.setOpenIDActive(true);
      AuthClient.setAuthToken(loginResponse!.token, loginResponse!.authenticationType);
      AuthClient.setCurrentUser(loginResponse!.userEntity);
      AuthClient.Options.onLogin(returnUrl);

    } catch (e) {
      OpenIDAuthenticator.setOpenIDActive(false);
      AppContext.navigate("/");
      throw e; 
    }
  }

  return (
    <div className="d-flex justify-content-center align-items-center" style={{ height: "100vh" }}>
      <div className="spinner-border text-primary" role="status">
        <span className="visually-hidden">Signing in…</span>
      </div>
    </div>
  );
}
