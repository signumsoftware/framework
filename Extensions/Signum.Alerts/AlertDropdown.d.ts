import * as React from 'react';
import { Entity } from '@framework/Signum.Entities';
import "./AlertDropdown.css";
import { Lite } from '@framework/Signum.Entities';
import { AlertEntity } from './Signum.Alerts';
export default function AlertDropdown(props: {
    keepRingingFor?: number;
}): JSX.Element | null;
interface AlertGroupWithSize {
    groupTarget?: Lite<Entity>;
    alerts: AlertWithSize[];
    totalHight?: number;
    maxDate: string;
    removing?: boolean;
}
interface AlertWithSize {
    alert: AlertEntity;
    height?: number;
    removing?: boolean;
}
export declare function AlertGroupToast(p: {
    group: AlertGroupWithSize;
    onClose: (e: AlertWithSize | AlertGroupWithSize) => void;
    onRefresh: () => void;
    style?: React.CSSProperties | undefined;
    onSizeSet: () => void;
}): JSX.Element;
export declare function AlertToast(p: {
    alert: AlertWithSize;
    onSizeSet: () => void;
    expanded: boolean | "comming";
    onClose: (e: AlertWithSize) => void;
    refresh: () => void;
    className?: string;
    style?: React.CSSProperties | undefined;
}): JSX.Element;
export declare namespace AlertToast {
    var icons: {
        [alertTypeKey: string]: React.ReactNode;
    };
}
export {};
