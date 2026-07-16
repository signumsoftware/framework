import * as React from "react";
import * as msal from "@azure/msal-browser";
import { LoginContext } from "../Signum.Authorization/Login/LoginPage";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { AzureADType } from "./Signum.Authorization.AzureAD";
export declare namespace AzureADAuthenticator {
    namespace Options {
        let getAzureADConfig: (adVariant: string) => AzureADConfig | undefined;
    }
    function registerAzureADAuthenticator(): void;
    type B2C_UserFlows = "signInSignUp_UserFlow" | "signIn_UserFlow" | "signUp_UserFlow" | "resetPassword_UserFlow" | "editProfile_UserFlow";
    function getAuthority(config: AzureADConfig, b2c_UserFlow?: B2C_UserFlows): string;
    function signIn(ctx: LoginContext, adVariant: string, b2c_UserFlow?: B2C_UserFlows, e?: React.MouseEvent): Promise<void>;
    function resetPasswordB2C(ctx: LoginContext, adVariant: string): Promise<void>;
    function loginWithAzureADSilent(): Promise<AuthClient.API.LoginResponse | undefined>;
    function cleantMsalAccount(): void;
    function setMsalAccount(accountName: string, adVariant: string): void;
    function getCurrentMsalAccount(): msal.AccountInfo | null | undefined;
    function getCurrentADVariant(): string | null;
    function getCurrentADConfig(): AzureADConfig | undefined;
    function getAccessToken(): Promise<string>;
    function signOut(): Promise<void>;
    namespace API {
        function loginWithAzureAD(jwt: string, accessToken: string, opts: {
            throwErrors: boolean;
            adVariant: string | null;
        }): Promise<AuthClient.API.LoginResponse | undefined>;
    }
}
export declare namespace MicrosoftSignIn {
    var iconUrl: string;
}
export declare namespace MicrosoftSignIn {
    var iconUrl: string;
}
export declare function MicrosoftSignIn({ ctx, adVariant }: {
    ctx: LoginContext;
    adVariant?: string;
}): React.JSX.Element;
export declare function AzureB2CSignIn({ ctx, adVariant }: {
    ctx: LoginContext;
    adVariant?: string;
}): React.JSX.Element;
declare global {
    interface Window {
        __azureADConfig?: AzureADConfig;
    }
}
export interface AzureADConfig {
    type: AzureADType;
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
//# sourceMappingURL=AzureADAuthenticator.d.ts.map