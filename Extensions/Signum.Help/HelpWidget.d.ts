import * as React from 'react';
import { Entity } from '@framework/Signum.Entities';
import { WidgetContext } from '@framework/Frames/Widgets';
import { TypeHelpEntity } from './Signum.Help';
import { TypeContext } from '@framework/Lines';
import './HelpWidget.css';
export interface HelpWidgetProps {
    wc: WidgetContext<Entity>;
}
export declare function HelpWidget(p: HelpWidgetProps): React.JSX.Element;
export declare function HelpIcon(p: {
    ctx: TypeContext<any>;
    typeHelp?: TypeHelpEntity;
}): React.JSX.Element | undefined | null | boolean;
interface TypeHelpIconProps extends React.HTMLAttributes<HTMLAnchorElement> {
    type: string;
}
export declare function TypeHelpIcon({ type, className, ...props }: TypeHelpIconProps): React.JSX.Element;
export {};
