import * as React from "react";
import { RouteObject } from 'react-router'
import { Link } from 'react-router-dom'
import { ImportComponent } from '@framework/ImportComponent'
import { ajaxPost } from "@framework/Services";
import { LoginAuthMessage } from "../Signum.Authorization/Signum.Authorization";
import LoginPage from "../Signum.Authorization/Login/LoginPage";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { ChangeLogClient } from "@framework/Basics/ChangeLogClient";

export namespace ResetPasswordClient {
  
  export function startPublic(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.ResetPassword", () => import("./Changelog"));
  
    options.routes.push({ path: "/auth/forgotPasswordEmail", element: <ImportComponent onImport={() => import("./ForgotPasswordEmailPage")} /> });
    options.routes.push({ path: "/auth/resetPassword", element: <ImportComponent onImport={() => import("./ResetPassword")} /> });
  
    LoginPage.resetPasswordControl = () => <span>
      &nbsp;
      &nbsp;
      <Link to="/auth/forgotPasswordEmail">{LoginAuthMessage.IHaveForgottenMyPassword.niceToString()}</Link>
    </span>;
  }
  
  export namespace API {
  
    export function forgotPasswordEmail(request: ForgotPasswordEmailRequest): Promise<ForgotPasswordEmailResponse> {
      return ajaxPost({ url: "/api/auth/forgotPasswordEmail" }, request);
    }
  
    export function resetPassword(request: ResetPasswordRequest): Promise<AuthClient.API.LoginResponse> {
      return ajaxPost({ url: "/api/auth/resetPassword" }, request);
    }
  
    export function requestNewLink(code: string): Promise<void> {
      return ajaxPost({ url: "/api/auth/requestNewLink" }, code);
    }

  export interface ResetPasswordRequest {
    code: string;
    newPassword: string;
  }
  
  export interface ForgotPasswordEmailRequest {
    email: string;
  }
  
    export interface ForgotPasswordEmailResponse {
      success: boolean;
      message: string;
      title?: string;
    }
  }
}
