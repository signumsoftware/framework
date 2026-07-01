import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { OpenIDAuthenticator } from './OpenIDAuthenticator'
import ErrorModal from "@framework/Modals/ErrorModal"
// Dedicated callback page for the OpenID Connect authorization code flow.
// Keycloak / Dex redirects here after authentication with ?code=...&state=...
// Registered as the route /openid-callback in OpenIDClient.startPublic().
export default function OpenIDCallback(): React.JSX.Element {

  const [isError, setIsError] = React.useState();
   
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

    try {

      if (!code) {
        throw new Error("No 'code' in query string");
      }

      if (!state || state !== storedState) {
        throw new Error("Invalid 'state' in query string");
      }

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
      throw e; 
    }
  }

  return (
    <div className="d-flex justify-content-center align-items-center" style={{ height: "100vh" }}>
      {isError ? <div className="spinner-border text-primary" role="status">
        <span className="visually-hidden">Signing in…</span>
      </div> :
        <div className="text-danger" role="status">
          <span>Error</span>
        </div>
      }
    </div>
  );
}
