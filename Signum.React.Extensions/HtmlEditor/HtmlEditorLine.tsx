import * as React from 'react'
import * as draftjs from 'draft-js';
import HtmlEditor, { HtmlEditorProps } from './HtmlEditor';
import { TypeContext } from '@framework/TypeContext';
import { ErrorBoundary } from '@framework/Components';
import { getTimeMachineIcon } from '@framework/Lines/TimeMachineIcon';

export interface HtmlEditorLineProps extends Omit<HtmlEditorProps & Partial<draftjs.EditorProps>, "binding"> {
  ctx: TypeContext<string | null | undefined>;
  htmlEditorRef?: React.Ref<HtmlEditorController>;
}

export default function HtmlEditorLine({ ctx, htmlEditorRef, ...p }: HtmlEditorLineProps) {
  return (
    <ErrorBoundary>
      <div style={p.htmlAttributes?.style}>
        {getTimeMachineIcon({ ctx: ctx })}
        <HtmlEditor binding={ctx.binding} ref={htmlEditorRef} {...p} />
      </div>
    </ErrorBoundary>
  );
}
