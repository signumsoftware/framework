import * as draftjs from 'draft-js';
import draftToHtml from 'draftjs-to-html';
import htmlToDraft from 'html-to-draftjs';
import { IContentStateConverter, HtmlEditorController } from "./HtmlEditor"

interface DraftToHtmlOptions {
  hashConfig?: { trigger: "#", separator: " " },
  directional?: boolean,
  customEntityTransform?: (entity: draftjs.RawDraftEntity, text: string) => string | undefined
}

interface HtmlToDraftOptions {
  customChunkRenderer?: (nodeName: string, node: HTMLElement) => draftjs.RawDraftEntity | null | undefined
}

export class HtmlContentStateConverter implements IContentStateConverter {

  constructor(
    public draftToHtmlOptions: DraftToHtmlOptions,
    public htmlToDraftOptions: HtmlToDraftOptions)
  {
  }

  contentStateToText(content: draftjs.ContentState): string {

    const rawContentState = draftjs.convertToRaw(content);
    const { hashConfig, directional, customEntityTransform } = this.draftToHtmlOptions;
    return draftToHtml(rawContentState, hashConfig, directional, customEntityTransform);
  }

  textToContentState(html: string): draftjs.ContentState {
    //console.log("Parsing: " + html);
    const { customChunkRenderer } = this.htmlToDraftOptions;
    const { contentBlocks, entityMap } = htmlToDraft(html, customChunkRenderer);
    const result = draftjs.ContentState.createFromBlockArray(contentBlocks, entityMap);
    //console.log("Parsed: " + JSON.stringify(draftjs.convertToRaw(result), undefined, 2));
    return result;
  }
}
