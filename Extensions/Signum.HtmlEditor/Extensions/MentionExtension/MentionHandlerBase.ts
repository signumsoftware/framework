import * as React from 'react';

export interface MentionItem {
  key: string;   // unique identifier stored in data-mention-key (e.g. liteKey, slug, id)
  text: string;  // display text shown in the chip and the dropdown
}

export interface MentionHandlerBase {
  /** Character that opens the typeahead, e.g. '@' or '#'. */
  trigger: string;

  /** Return ALL candidates; the plugin filters them client-side. */
  getItems(): Promise<MentionItem[]>;

  /** Optional: custom rendering for a row in the dropdown list. */
  renderOption?(item: MentionItem): React.ReactNode;
}

declare module 'lexical' {
  export interface LexicalEditor {
    mentionHandler?: MentionHandlerBase;
  }
}
