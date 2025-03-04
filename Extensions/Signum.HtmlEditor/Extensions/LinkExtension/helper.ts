import { LexicalEditor, RangeSelection, $createRangeSelection, $setSelection } from "lexical";

export function restoreSelection(editor: LexicalEditor, selectionToRestore: RangeSelection): void {
  editor.update(() => {
    if(!selectionToRestore) return;
    const selection = $createRangeSelection();
    selection.anchor.set(selectionToRestore.anchor.key, selectionToRestore.anchor.offset, "text");
    selection.focus.set(selectionToRestore.focus.key, selectionToRestore.focus.offset, "text");
    $setSelection(selection);    
   });
}

const SUPPORTED_URL_PROTOCOLS = new Set([
  'http:',
  'https:',
  'mailto:',
  'sms:',
  'tel:',
]);

export function sanitizeUrl(url: string): string {
  try {
    const parsedUrl = new URL(url);
    if (!SUPPORTED_URL_PROTOCOLS.has(parsedUrl.protocol)) {
      return ""
    }
  } catch {
    return url;
  }
  return url;
}

// Source: https://stackoverflow.com/a/8234912/2013580
const urlRegExp = new RegExp(
  /((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=+$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=+$,\w]+@)[A-Za-z0-9.-]+)((?:\/[+~%/.\w-_]*)?\??(?:[-+=&;%@.\w_]*)#?(?:[\w]*))?)/
);

export function validateUrl(url: string): boolean {
  return urlRegExp.test(url);
}
