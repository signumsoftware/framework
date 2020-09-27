import * as React from "react";
import * as Services from '@framework/Services';
import * as AuthClient from '../AuthClient';
import { ImportRoute } from "@framework/AsyncImport";
import Login, { LoginWithWindowsButton } from "../Login/Login";
import * as AppContext from "@framework/AppContext";
import { UserEntity, PermissionSymbol } from "../Signum.Entities.Authorization";
import { ajaxPost, ajaxGet, ServiceError } from "@framework/Services";
import { is, Lite, toLite } from "@framework/Signum.Entities";
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


export function register() {
  var user = AuthClient.currentUser();
  API.makeCredentialOptions({ user: toLite(user) })
    .then(response => {

      const options = response.credentialCreateOptions;
      // Turn the challenge back into the accepted format of padded base64
      options.challenge = coerceToArrayBuffer(options.challenge, "challenge");
      // Turn ID into a UInt8Array Buffer for some reason
      options.user.id = coerceToArrayBuffer(options.user.id, "user.id");

      options.excludeCredentials?.forEach((c) => {
        c.id = coerceToArrayBuffer(c.id, "excludeCredentials/id");
        return c;
      });

      if (options.authenticatorSelection?.authenticatorAttachment === null)
        options.authenticatorSelection.authenticatorAttachment = undefined;

      navigator.credentials.create({
        publicKey: options
      }).then(credential => {
        if (credential == null)
          return;

        const newCredential = credential as PublicKeyCredential; 

        const attestationObject = new Uint8Array((newCredential.response as any).attestationObject);
        const clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
        const rawId = new Uint8Array(newCredential.rawId);

        API.makeCredential({
          createOptionsId: response.createOptionsId,
          attestationRawResponse: {
            id: newCredential.id,
            rawId: coerceToBase64Url(rawId),
            type: newCredential.type,
            extensions: newCredential.getClientExtensionResults(),
            response: {
              attestationObject: coerceToBase64Url(attestationObject),
              clientDataJson: coerceToBase64Url(clientDataJSON)
            }
          }
        })
          .then(() => MessageModal.show({ title: "Success", message: "WebAuthn Credentials created sucessfully!", buttons: "ok" }))
          .done();

      });
    }).done();
}

export function login() {

}


function coerceToArrayBuffer(thing: unknown, name: string): ArrayBuffer{
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


function coerceToBase64Url(thing: unknown) : string {
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

  export function makeCredentialOptions(request: { user: Lite<UserEntity> }): Promise<MakeCredentialOptionsResponse> {
    return ajaxPost({ url: "~/api/webauthn/makeCredentialOptions" }, request);
  }

  export function makeCredential(request: { createOptionsId: string, attestationRawResponse: AuthenticatorAttestationRawResponse }): Promise<MakeCredentialOptionsResponse> {
    return ajaxPost({ url: "~/api/webauthn/makeCredential" }, request);
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

