import * as React from 'react'
import HtmlEditor from './HtmlEditor';
import { ErrorBoundary } from '@framework/Components';
import { ReadonlyBinding } from '@framework/Lines';
import { LinkExtension } from './Extensions/LinkExtension';
import { HtmlEditorExtension } from './Extensions/types';
import './HtmlViewer.css'

const defaultExtensions: HtmlEditorExtension[] = [new LinkExtension()];

export default function HtmlViewer(p: {
  text: string | null;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  extensionsMemo?: HtmlEditorExtension[];
}): React.JSX.Element {
  const extensions = p.extensionsMemo ?? defaultExtensions;
  const binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer">
      <ErrorBoundary>
        <HtmlEditor readOnly binding={binding} htmlAttributes={p.htmlAttributes} small extensionsMemo={extensions} />
      </ErrorBoundary>
    </div>
  );
}
