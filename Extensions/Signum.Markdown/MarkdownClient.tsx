import * as React from 'react';
import { Finder } from '@framework/Finder';
import Markdown from 'react-markdown';

export namespace MarkdownClient {
  export function start(): void {
    Finder.formatRules.push({
      name: "Markdown",
      isApplicable: qt => qt.format === "Markdown",
      formatter: () => new Finder.CellFormatter(
        (val: string | null) => val ? <div><Markdown>{val}</Markdown></div> : undefined,
        true
      )
    });
  }
}
