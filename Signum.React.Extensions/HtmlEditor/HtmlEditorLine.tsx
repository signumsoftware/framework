import * as React from 'react'
import * as draftjs from 'draft-js';
import HtmlEditor, { HtmlEditorProps } from './HtmlEditor';
import { TypeContext } from '@framework/TypeContext';
import { ErrorBoundary } from '@framework/Components';
import { getTimeMachineIcon } from '@framework/Lines/TimeMachineIcon';

export interface HtmlEditorLineProps extends Omit<HtmlEditorProps & Partial<draftjs.EditorProps>, "binding"> {
  ctx: TypeContext<string | null | undefined>
}

export default function HtmlEditorLine(p: HtmlEditorLineProps) {
  return (
    <ErrorBoundary>
      <div style={p.htmlAttributes?.style}>
        {getTimeMachineIcon({ ctx: p.ctx })}
        <HtmlEditor binding={p.ctx.binding} {...p} />
      </div>
    </ErrorBoundary>
  );
}
