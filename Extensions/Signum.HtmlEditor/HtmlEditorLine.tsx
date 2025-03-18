import * as React from 'react'
import * as draftjs from 'draft-js';
import HtmlEditor, { HtmlEditorController, HtmlEditorProps } from './HtmlEditor';
import { TypeContext } from '@framework/TypeContext';
import { ErrorBoundary } from '@framework/Components';
import { getTimeMachineIcon } from '@framework/Lines/TimeMachineIcon';
import ListCommandsPlugin from './Plugins/ListCommandsPlugin';
import BasicCommandsPlugin from './Plugins/BasicCommandsPlugin';
import './HtmlEditorLine.css';
import { classes } from '@framework/Globals';
import { FormGroup } from '@framework/Lines';
import { useForceUpdate } from '@framework/Hooks';

export interface HtmlEditorLineProps extends Omit<HtmlEditorProps & Partial<draftjs.EditorProps>, "binding"> {
  ctx: TypeContext<string | null | undefined>;
  htmlEditorRef?: React.Ref<HtmlEditorController>;
  extraButtons?: () => React.ReactNode;
  extraButtonsBefore?: () => React.ReactNode;
}

export default function HtmlEditorLine({ ctx, htmlEditorRef, readOnly, extraButtons, extraButtonsBefore, ...p }: HtmlEditorLineProps): React.JSX.Element {

  const forceUpdate = useForceUpdate();
  return (
    <FormGroup ctx={ctx} >
      {id =>
        <ErrorBoundary>
          <div className="d-flex">
            {extraButtonsBefore && <div className={ctx.inputGroupVerticalClass("before")}>
              {extraButtonsBefore()}
            </div>}
            <div className={classes("html-editor-line", !ctx.value && (p.mandatory || ctx.propertyRoute?.member?.required) ? "sf-mandatory" : undefined)} style={{
              backgroundColor: readOnly ? "#e9ecef" : undefined,
              flexGrow: 1,
              ...p.htmlAttributes?.style
            }} data-property-path={ctx.propertyPath} >
              {getTimeMachineIcon({ ctx: ctx })}
              <HtmlEditor
                readOnly={ctx.readOnly}
                binding={ctx.binding}
                ref={htmlEditorRef}
                plugins={p.plugins ?? [
                  new ListCommandsPlugin(),
                  new BasicCommandsPlugin(),
                ]}
                {...p}
                onEditorBlur={(e, controller) => { forceUpdate(); p.onEditorBlur?.(e, controller); }}
              />
            </div>
            {extraButtons && <div className={ctx.inputGroupVerticalClass("after")}>
              {extraButtons()}
            </div>
            }
          </div>
        </ErrorBoundary>
      }
    </FormGroup>
  );
}
