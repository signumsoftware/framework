import * as draftjs from 'draft-js';
import { stateToHTML, Options as ExportOptions } from 'draft-js-export-html';
import { stateFromHTML, Options as ImportOptions } from 'draft-js-import-html';
import { IContentStateConverter, HtmlEditorController } from "./HtmlEditor"


export class HtmlContentStateConverter implements IContentStateConverter {

  constructor(
    public exportOptions: ExportOptions,
    public importOptions: ImportOptions)
  {
  }

  contentStateToText(content: draftjs.ContentState): string {
    return stateToHTML(content, this.exportOptions);
  }

  textToContentState(html: string): draftjs.ContentState {
    console.log("Parsing: " + html);
    const result = stateFromHTML(html, this.importOptions);
    console.log("Parsed: " + JSON.stringify(draftjs.convertToRaw(result), undefined, 2));
    return result;
  }
}
