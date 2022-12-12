import { RestLogEntity, RestApiKeyEntity } from './Signum.Entities.Rest'
import { EntitySettings } from "@framework/Navigator";
import * as Navigator from "@framework/Navigator";
import { ajaxGet } from "@framework/Services";
import * as AuthClient from "../Authorization/AuthClient";
import { QueryString } from '@framework/QueryString';

export function registerAuthenticator() {
  AuthClient.authenticators.insertAt(0, loginFromApiKey);
}

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(RestLogEntity, e => import('./Templates/RestLog')));
  Navigator.addSettings(new EntitySettings(RestApiKeyEntity, e => import('./Templates/RestApiKey')));
}

export function loginFromApiKey(): Promise<AuthClient.AuthenticatedUser | undefined> {
  const query = QueryString.parse(window.location.search);

  if ('apiKey' in query) {
    return API.loginFromApiKey(query.apiKey as string);
  }

  return Promise.resolve(undefined);
}

export module API {
  export function generateRestApiKey(): Promise<string> {
    return ajaxGet({ url: "~/api/restApiKey/generate" });
  }

  export function getCurrentRestApiKey(): Promise<string> {
    return ajaxGet({ url: "~/api/restApiKey/current" });
  }

  export function loginFromApiKey(apiKey: string): Promise<AuthClient.API.LoginResponse> {
    return ajaxGet({ url: "~/api/auth/loginFromApiKey?apiKey=" + apiKey, avoidAuthToken: true });
  }

  export function replayRestLog(restLogID: string | number, host: string) : Promise<string> {
    return ajaxGet({ url: "~/api/restLog?id=" + restLogID + "&url=" + host });

  }
}
