import * as React from 'react';
import { Finder } from '@framework/Finder';
import HtmlViewer from './HtmlViewer';

export namespace HtmlEditorClient {
  export function start(): void {
    Finder.formatRules.push({
      name: "Html",
      isApplicable: qt => qt.format === "Html",
      formatter: () => new Finder.CellFormatter(
        (val: string | null) => val ? <HtmlViewer text={val} /> : undefined,
        true
      )
    });
  }
}
