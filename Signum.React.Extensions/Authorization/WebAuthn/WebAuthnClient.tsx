import * as React from "react";
import { NavDropdown } from "react-bootstrap";
import * as Services from '@framework/Services';
import * as AuthClient from '../AuthClient';
import { ImportRoute } from "@framework/AsyncImport";
import { LoginContext } from "../Login/Login";
import * as AppContext from "@framework/AppContext";
import { UserEntity, PermissionSymbol, LoginAuthMessage } from "../Signum.Entities.Authorization";
import { ajaxPost, ajaxGet, ServiceError } from "@framework/Services";
import { getToString, is, JavascriptMessage, Lite, toLite } from "@framework/Signum.Entities";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ifError } from "@framework/Globals";
import { Cookies } from "@framework/Cookies";
import { tryGetTypeInfo } from "@framework/Reflection";
import MessageModal from "@framework/Modals/MessageModal";

let applicationName: string;

export namespace Settings {
   
}

export function start(options: { routes: JSX.Element[], applicationName: string }) {
  applicationName = options.applicationName;
}

export function webAuthnDisplayName() {

  if (typeof (PublicKeyCredential) == "undefined")
    return undefined;

  if (navigator.platform.contains("Win"))
    return "Windows Hello / PIN";

  if (navigator.platform.contains("iPhone") ||
    navigator.platform.contains("iPad") ||
    navigator.platform.contains("iPod") || 
    navigator.platform.contains("Mac"))
    return "Apple Face ID / Touch ID";

  if (navigator.platform.contains("Android") || 
    navigator.platform.contains("Linux"))
    return "Android Fingerprint";

  return "WebAuthn / FIDO2 device";
}

export function WebAuthnRegisterMenuItem() {
  var displayName = webAuthnDisplayName();

  if (displayName == null)
    return null;

  return (
    <NavDropdown.Item onClick={() => register()}>
      <FontAwesomeIcon icon="fingerprint" fixedWidth className="mr-2" /> {LoginAuthMessage.Register0.niceToString(displayName)}
    </NavDropdown.Item>
  );
}

export function register() {
  var user = AuthClient.currentUser();
  API.makeCredentialOptions()
    .then(response => {

      const options = response.credentialCreateOptions;
      // Turn the challenge back into the accepted format of padded base64
      options.challenge = toArrayBuffer(options.challenge, "challenge");
      // Turn ID into a UInt8Array Buffer for some reason
      options.user.id = toArrayBuffer(options.user.id, "user.id");

      options.excludeCredentials?.forEach((c) => {
        c.id = toArrayBuffer(c.id, "excludeCredentials/id");
        return c;
      });

      if (options.authenticatorSelection?.authenticatorAttachment === null)
        options.authenticatorSelection.authenticatorAttachment = undefined;

      debugger;
      return navigator.credentials.create({
        publicKey: options
      }).then(credential => {
        debugger;
        if (credential == null)
          return;

        const newCredential = credential as PublicKeyCredential; 

        const attestationObject = new Uint8Array((newCredential.response as any).attestationObject);
        const clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
        const rawId = new Uint8Array(newCredential.rawId);

        return API.makeCredential({
          createOptionsId: response.createOptionsId,
          attestationRawResponse: {
            id: newCredential.id,
            rawId: toBase64Url(rawId),
            type: newCredential.type,
            extensions: newCredential.getClientExtensionResults(),
            response: {
              attestationObject: toBase64Url(attestationObject),
              clientDataJson: toBase64Url(clientDataJSON)
            }
          }
        })
          .then(() => MessageModal.show({
            title: "Success",
            message: <>
              <p>
                {LoginAuthMessage._0HasBeenSucessfullyAssociatedWithUser1InThisDevice.niceToString().formatHtml(
                  <strong>{webAuthnDisplayName()}</strong>,
                  <strong>{getToString(user)}</strong>)
                }
              </p>
              <p>{LoginAuthMessage.TryToLogInWithIt.niceToString()}</p>
            </>,
            buttons: "ok"
          }));
      });
    }).done();
}

export function WebAuthnLoginButton({ ctx }: { ctx: LoginContext }) {
  var displayName = webAuthnDisplayName();

  if (displayName == null)
    return null;

  return (

    <div className="row" style={{ paddingTop: "1rem" }}>
      <div className="col-md-6 offset-md-3">
        <button id="loginWebAuthn" className="btn btn-info" disabled={ctx.loading != null} onClick={() => login(ctx)}>
          {ctx.loading == "webauthn" ? <FontAwesomeIcon icon="cog" fixedWidth style={{ fontSize: "larger" }} spin /> : <FontAwesomeIcon icon="fingerprint" />}
      &nbsp;
      {ctx.loading == "webauthn" ? JavascriptMessage.loading.niceToString() : LoginAuthMessage.LoginWith0.niceToString(displayName)}
        </button>
      </div>
    </div>
  );
}

function fromBase64UrlToUint8Array(base64Url: string): Uint8Array {
  var base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/")
  return Uint8Array.from(atob(base64), c => c.charCodeAt(0));
}

export function login(ctx: LoginContext) {

  ctx.setLoading("webathn");

  API.assertionOptions({ userName: ctx.userName.current!.value! })
    .then(aor => {

      var options = aor.assertionOptions;

      // todo: switch this to coercebase64
      options.challenge = fromBase64UrlToUint8Array(options.challenge as any as string);

      // fix escaping. Change this to coerce
      options.allowCredentials!.forEach(credDescr => {
        credDescr.id = fromBase64UrlToUint8Array(credDescr.id as any as string);
      });

      return navigator.credentials.get({ publicKey: aor.assertionOptions })
        .then(credential => {

          const assertedCredential = credential as PublicKeyCredential;

          const response = assertedCredential.response as AuthenticatorAssertionResponse;

          let authData = new Uint8Array(response.authenticatorData);
          let clientDataJSON = new Uint8Array(response.clientDataJSON);
          let rawId = new Uint8Array(assertedCredential.rawId);
          let sig = new Uint8Array(response.signature);

          return API.makeAssertion({
            assertionOptionsId: aor.assertionOptionsId,
            assertionRawResponse: {
              id: assertedCredential.id,
              rawId: toBase64Url(rawId),
              type: assertedCredential.type,
              extensions: assertedCredential.getClientExtensionResults(),
              response: {
                authenticatorData: toBase64Url(authData),
                clientDataJson: toBase64Url(clientDataJSON),
                signature: toBase64Url(sig)
              }
            }
          }).then(lr => {
            AuthClient.setAuthToken(lr.token, lr.authenticationType);
            AuthClient.setCurrentUser(lr.userEntity);
            AuthClient.Options.onLogin();
          })
        }); 
    })
    .catch(e => {
      ctx.setLoading(undefined);
      throw e;
    })
    .done();

}


function toArrayBuffer(thing: unknown, name: string): ArrayBuffer{
  if (typeof thing === "string") {
    // base64url to base64
    thing = thing.replace(/-/g, "+").replace(/_/g, "/");

    // base64 to Uint8Array
    var str = window.atob(thing as string);
    var bytes = new Uint8Array(str.length);
    for (var i = 0; i < str.length; i++) {
      bytes[i] = str.charCodeAt(i);
    }
    thing = bytes;
  }

  // Array to Uint8Array
  if (Array.isArray(thing)) {
    thing = new Uint8Array(thing);
  }

  // Uint8Array to ArrayBuffer
  if (thing instanceof Uint8Array) {
    thing = thing.buffer;
  }

  // error if none of the above worked
  if (!(thing instanceof ArrayBuffer)) {
    throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
  }

  return thing;
};


function toBase64Url(thing: unknown) : string {
  // Array or ArrayBuffer to Uint8Array
  if (Array.isArray(thing)) {
    thing = Uint8Array.from(thing);
  }

  if (thing instanceof ArrayBuffer) {
    thing = new Uint8Array(thing);
  }

  // Uint8Array to base64
  if (thing instanceof Uint8Array) {
    var str = "";
    var len = thing.byteLength;

    for (var i = 0; i < len; i++) {
      str += String.fromCharCode(thing[i]);
    }
    thing = window.btoa(str);
  }

  if (typeof thing !== "string") {
    throw new Error("could not coerce to string");
  }

  // base64 to base64url
  // NOTE: "=" at the end of challenge is optional, strip it off here
  thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

  return thing as string;
}

export module API {

  export function makeCredentialOptions(): Promise<MakeCredentialOptionsResponse> {
    return ajaxPost({ url: "~/api/webauthn/makeCredentialOptions" }, null);
  }

  export function makeCredential(request: { createOptionsId: string, attestationRawResponse: AuthenticatorAttestationRawResponse }): Promise<MakeCredentialOptionsResponse> {
    return ajaxPost({ url: "~/api/webauthn/makeCredential" }, request);
  }

  export function assertionOptions(request: { userName: string }): Promise<AssertionOptionsResponse> {
    return ajaxPost({ url: "~/api/webauthn/assertionOptions" }, request);
  }

  export function makeAssertion(request: { assertionOptionsId: string, assertionRawResponse: AuthenticatorAssertionRawResponse }): Promise<AuthClient.API.LoginResponse> {
    return ajaxPost({ url: "~/api/webauthn/makeAssertion" }, request);
  }
}

export interface MakeCredentialOptionsResponse {
  createOptionsId: string;
  credentialCreateOptions: PublicKeyCredentialCreationOptions;
}

export interface AuthenticatorAttestationRawResponse {
  id: string;
  rawId: string;
  type: string;
  response: {
    attestationObject: string;
    clientDataJson: string;
  },
  extensions: AuthenticationExtensionsClientOutputs;
}

export interface AssertionOptionsResponse {
  assertionOptions: PublicKeyCredentialRequestOptions;
  assertionOptionsId: string;
}

export interface AuthenticatorAssertionRawResponse {
  id: string;
  rawId: string;
  type: string;
  response: {
    authenticatorData: string;
    signature: string;
    clientDataJson: string;
    userHandle?: string;
  },
  extensions: AuthenticationExtensionsClientOutputs;
}
