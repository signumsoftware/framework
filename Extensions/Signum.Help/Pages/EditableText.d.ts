import * as React from 'react';
import { PropertyRoute, TypeContext } from '@framework/Lines';
import { IBinding } from '@framework/Reflection';
import { ImageConverter } from '../../Signum.HtmlEditor/Extensions/ImageExtension/ImageConverter';
export declare function EditableTextComponent({ ctx, defaultText, onChange, defaultEditable }: {
    ctx: TypeContext<string | null>;
    defaultText?: string;
    onChange?: () => void;
    defaultEditable?: boolean;
}): React.JSX.Element;
export declare function EditableHtmlComponent({ ctx, defaultText, onChange, defaultEditable }: {
    ctx: TypeContext<string | undefined | null>;
    defaultText?: string;
    onChange?: () => void;
    defaultEditable?: boolean;
}): React.JSX.Element;
export declare function HelpHtmlEditor(p: {
    binding: IBinding<string | null | undefined>;
}): React.JSX.Element;
export declare function HtmlViewer(p: {
    text: string | null | undefined;
    htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
}): React.JSX.Element | null;
export interface ImageInfo {
    inlineImageId?: string;
    binaryFile?: string;
    fileName?: string;
}
export declare class InlineImageConverter implements ImageConverter<ImageInfo> {
    pr: PropertyRoute;
    constructor();
    toElement(val: ImageInfo): HTMLElement | undefined;
    uploadData(blob: Blob): Promise<ImageInfo>;
    renderImage(info: ImageInfo): React.ReactElement<any, string | ((props: any) => React.ReactElement<any, string | any | (new (props: any) => React.Component<any, any, any>)> | null) | (new (props: any) => React.Component<any, any, any>)>;
    toHtml(val: ImageInfo): string | undefined;
    fromElement(element: HTMLDivElement): ImageInfo | undefined;
}
