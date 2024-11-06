import * as React from 'react';
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { HtmlEditorController } from './HtmlEditor';
export declare function Separator(): React.JSX.Element;
export declare function HtmlEditorButton(p: {
    icon?: IconProp;
    content?: React.ReactNode;
    isActive?: boolean;
    title?: string;
    onClick: (e: React.MouseEvent) => void;
}): React.JSX.Element;
export declare namespace HtmlEditorButton {
    const defaultProps: {
        icon: string;
    };
}
export declare function InlineStyleButton(p: {
    controller: HtmlEditorController;
    style: string;
    icon?: IconProp;
    content?: React.ReactChild;
    title?: string;
}): React.JSX.Element;
export declare function BlockStyleButton(p: {
    controller: HtmlEditorController;
    blockType: string;
    icon?: IconProp;
    content?: React.ReactChild;
    title?: string;
}): React.JSX.Element;
export declare function SubMenuButton(p: {
    controller: HtmlEditorController;
    icon?: IconProp;
    content?: React.ReactChild;
    title?: string;
    children: React.ReactNode;
}): React.JSX.Element;
export declare function SubMenu(p: {
    controller: HtmlEditorController;
    children: React.ReactNode;
}): React.JSX.Element;
//# sourceMappingURL=HtmlEditorButtons.d.ts.map