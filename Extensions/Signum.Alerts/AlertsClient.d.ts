import * as React from 'react';
import { RouteObject } from 'react-router';
import { AlertEntity, AlertTypeSymbol } from './Signum.Alerts';
export declare function start(options: {
    routes: RouteObject[];
    showAlerts?: (typeName: string, when: "CreateAlert" | "QuickLink") => boolean;
}): void;
export declare function getTitle(titleField: string | null, type: AlertTypeSymbol | null): string | null;
export declare function format(text: string, alert: Partial<AlertEntity>, onNavigated?: () => void): React.ReactElement;
export declare module API {
    function myAlerts(): Promise<AlertEntity[]>;
    function myAlertsCount(): Promise<NumAlerts>;
}
export interface NumAlerts {
    numAlerts: number;
    lastAlert?: string;
}
