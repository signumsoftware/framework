import * as React from "react";
import * as msal from "@azure/msal-browser";
import { LoginContext } from "../Signum.Authorization/Login/LoginPage";
import { AuthClient } from "../Signum.Authorization/AuthClient";
export declare namespace AzureADAuthenticator {
    function registerAzureADAuthenticator(): void;
    namespace Config {
        let scopes: string[];
        let scopesB2C: string[];
    }
    type B2C_UserFlows = "signInSignUp_UserFlow" | "signIn_UserFlow" | "signUp_UserFlow" | "resetPassword_UserFlow";
    function getAuthority(mode?: "B2C" | undefined, b2c_UserFlow?: B2C_UserFlows): string;
    function signIn(ctx: LoginContext, b2c: boolean, b2c_UserFlow?: B2C_UserFlows): Promise<void>;
    function resetPasswordB2C(ctx: LoginContext): Promise<void>;
    function isB2C(): boolean;
    function loginWithAzureADSilent(): Promise<AuthClient.API.LoginResponse | undefined>;
    function cleantMsalAccount(): void;
    function setMsalAccount(accountName: string, isB2C: boolean): void;
    function getMsalAccount(): msal.AccountInfo | null | undefined;
    function getAccessToken(): Promise<string>;
    function signOut(): Promise<void>;
    function MicrosoftSignIn({ ctx }: {
        ctx: LoginContext;
    }): React.JSX.Element;
    function AzureB2CSignIn({ ctx }: {
        ctx: LoginContext;
    }): React.JSX.Element;
    namespace MicrosoftSignIn {
        var iconUrl: string;
    }
    namespace API {
        function loginWithAzureAD(jwt: string, accessToken: string, opts: {
            throwErrors: boolean;
            azureB2C: boolean;
        }): Promise<AuthClient.API.LoginResponse | undefined>;
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
export {};
//# sourceMappingURL=AzureADAuthenticator.d.ts.map