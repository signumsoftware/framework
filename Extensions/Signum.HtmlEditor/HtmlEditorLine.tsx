import * as React from "react";
// import * as draftjs from "draft-js";
import { ErrorBoundary } from "@framework/Components";
import { classes } from "@framework/Globals";
import { useForceUpdate } from "@framework/Hooks";
import { FormGroup } from "@framework/Lines";
import { getTimeMachineIcon } from "@framework/Lines/TimeMachineIcon";
import { TypeContext } from "@framework/TypeContext";
import HtmlEditor, { HtmlEditorProps } from "./HtmlEditor";
import { HtmlEditorController } from "./HtmlEditorController";
import "./HtmlEditorLine.css";

export interface HtmlEditorLineProps
  extends Omit<HtmlEditorProps /*& Partial<draftjs.EditorProps>*/, "binding"> {
  ctx: TypeContext<string | null | undefined>;
  labelIcon?: React.ReactNode; 
  htmlEditorRef?: React.Ref<HtmlEditorController>;
  handleKeybindings?: HtmlEditorProps['handleKeybindings'];
  extraButtons?: () => React.ReactNode;
  extraButtonsBefore?: () => React.ReactNode;
}

export default function HtmlEditorLine({
  ctx,
  htmlEditorRef,
  readOnly,
  extraButtons,
  extraButtonsBefore,
  ...p
}: HtmlEditorLineProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  return (
    <FormGroup ctx={ctx} labelIcon={p.labelIcon} >
      {(id) => (
        <ErrorBoundary>
          <div className="d-flex">
            {extraButtonsBefore && (
              <div className={ctx.inputGroupVerticalClass("before")}>
              {extraButtonsBefore()}
              </div>
            )}
            <div
              className={classes(
                "html-editor-line",
                ((p.mandatory ?? ctx.propertyRoute?.member?.required) && !ctx.value) && "sf-mandatory"
              )}
              style={{
                backgroundColor: readOnly ? "var(--bs-secondary-bg)" : undefined,
              flexGrow: 1,
                ...p.htmlAttributes?.style,
              }}
              data-property-path={ctx.propertyPath}
            >
              {getTimeMachineIcon({ ctx: ctx })}
              <HtmlEditor
                readOnly={ctx.readOnly}
                binding={ctx.binding}
                ref={htmlEditorRef}
                extensions={p.extensions}
                {...p}
                onEditorBlur={(e, controller) => {
                  forceUpdate();
                  p.onEditorBlur?.(e, controller);
                }}
              />
            </div>
            {extraButtons && (
              <div className={ctx.inputGroupVerticalClass("after")}>
              {extraButtons()}
            </div>
            )}
          </div>
        </ErrorBoundary>
      )}
    </FormGroup>
  );
}
