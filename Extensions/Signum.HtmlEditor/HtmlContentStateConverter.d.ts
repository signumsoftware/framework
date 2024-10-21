import * as draftjs from 'draft-js';
import { IContentStateConverter } from "./HtmlEditor";
interface DraftToHtmlOptions {
    hashConfig?: {
        trigger: "#";
        separator: " ";
    };
    directional?: boolean;
    customEntityTransform?: (entity: draftjs.RawDraftEntity, text: string) => string | undefined;
}
interface HtmlToDraftOptions {
    customChunkRenderer?: (nodeName: string, node: HTMLElement) => draftjs.RawDraftEntity | undefined;
}
export declare class HtmlContentStateConverter implements IContentStateConverter {
    draftToHtmlOptions: DraftToHtmlOptions;
    htmlToDraftOptions: HtmlToDraftOptions;
    constructor(draftToHtmlOptions: DraftToHtmlOptions, htmlToDraftOptions: HtmlToDraftOptions);
    contentStateToText(content: draftjs.ContentState): string;
    textToContentState(html: string): draftjs.ContentState;
}
export {};
//# sourceMappingURL=HtmlContentStateConverter.d.ts.map