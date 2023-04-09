import * as React from 'react';
import "./Login.css";
export interface LoginContext {
    loading: string | undefined;
    setLoading: (loading: string | undefined) => void;
    userName?: React.RefObject<HTMLInputElement>;
}
declare function LoginPage(): JSX.Element;
declare namespace LoginPage {
    var customLoginButtons: ((ctx: LoginContext) => React.ReactElement<any, string | React.JSXElementConstructor<any>>) | null;
    var showLoginForm: "yes" | "no" | "initially_not";
    var usernameLabel: () => string;
}
export default LoginPage;
export declare function LoginForm(p: {
    ctx: LoginContext;
}): JSX.Element;
export declare function LoginWithWindowsButton(): JSX.Element;
//# sourceMappingURL=LoginPage.d.ts.map