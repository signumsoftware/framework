import * as React from 'react'
import { RouteObject } from 'react-router'
import { RestLogEntity, RestApiKeyEntity } from './Signum.Rest'
import { Navigator, EntitySettings } from "@framework/Navigator";
import { ajaxGet } from "@framework/Services";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { QueryString } from '@framework/QueryString';

export namespace RestApiKeyClient {
  
  export function registerAuthenticator(): void {
    AuthClient.authenticators.insertAt(0, loginFromApiKey);
  }
  
  export function start(options: { routes: RouteObject[] }): void {
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
      return ajaxGet({ url: "/api/restApiKey/generate" });
    }
  
    export function getCurrentRestApiKey(): Promise<string> {
      return ajaxGet({ url: "/api/restApiKey/current" });
    }
  
    export function loginFromApiKey(apiKey: string): Promise<AuthClient.API.LoginResponse> {
      return ajaxGet({ url: "/api/auth/loginFromApiKey?apiKey=" + apiKey, avoidAuthToken: true });
    }
  }
}
