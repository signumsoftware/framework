import * as React from 'react'
import * as draftjs from 'draft-js';
import HtmlEditor, { HtmlEditorController, HtmlEditorProps } from './HtmlEditor';
import { TypeContext } from '@framework/TypeContext';
import { ErrorBoundary } from '@framework/Components';
import { getTimeMachineIcon } from '@framework/Lines/TimeMachineIcon';
import ListCommandsPlugin from './Plugins/ListCommandsPlugin';
import BasicCommandsPlugin from './Plugins/BasicCommandsPlugin';
import './HtmlEditorLine.css';

export interface HtmlEditorLineProps extends Omit<HtmlEditorProps & Partial<draftjs.EditorProps>, "binding"> {
  ctx: TypeContext<string | null | undefined>;
  htmlEditorRef?: React.Ref<HtmlEditorController>;
}

export default function HtmlEditorLine({ ctx, htmlEditorRef, ...p }: HtmlEditorLineProps): React.JSX.Element {
  return (
    <ErrorBoundary>
      <div style={p.htmlAttributes?.style} data-property-path={ctx.propertyPath} className="html-editor-line">
        {getTimeMachineIcon({ ctx: ctx })}
        <HtmlEditor 
          binding={ctx.binding}
          ref={htmlEditorRef}
          plugins={p.plugins ?? [
            new ListCommandsPlugin(),
            new BasicCommandsPlugin(),
          ]}
          {...p}
        />
      </div>
    </ErrorBoundary>
  );
}
