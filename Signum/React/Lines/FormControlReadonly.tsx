import * as React from 'react'
import { StyleContext } from '../Lines';
import { classes } from '../Globals';
import "./Lines.css"

export interface FormControlReadonlyProps {
  ctx: StyleContext;
  htmlAttributes?: React.HTMLAttributes<any>;
  className?: string;
  innerRef?: React.Ref<HTMLElement>;
  children?: React.ReactNode;
  id: string;
}

export function FormControlReadonly({ ctx, htmlAttributes: attrs, className, innerRef, children, id }: FormControlReadonlyProps): React.ReactElement {

  const array = React.Children.toArray(children);
  // An empty field renders the input too, with no value. As the div below, role="group", it was read by NVDA as
  // just "Domain, grouping" after a failed save: aria-invalid is not supported on a group and its description was
  // not read, so a required read-only field that was empty never said it was invalid or why (WCAG 3.3.1; NVDA
  // check 2026-10-01). An input is read as "edit, read only, invalid entry" with its message, as the Date field
  // beside it is. Both look the same (Lines.css styles .readonly and [readonly] alike).
  const onlyText = array.length == 1 && typeof array[0] == "string" ? array[0] as string : array.length == 0 ? "" : undefined;

  // FormGroup's <label for> points at this id, but a div is not labelable, so the label reached nobody: a
  // screen reader got the value without the field's name, and an empty field got nothing at all (WCAG 1.3.1,
  // 4.1.2; keyboard walk, initiation request). Found after rendering, because the label is the FormGroup's.
  const explicitName = attrs?.["aria-label"] || attrs?.["aria-labelledby"];
  const [labelId, setLabelId] = React.useState<string | undefined>(undefined);
  React.useLayoutEffect(() => {
    if (onlyText != undefined || explicitName) {
      setLabelId(undefined);
      return;
    }
    const label = document.querySelector<HTMLLabelElement>(`label[for="${CSS.escape(id)}"]`);
    if (label && !label.id)
      label.id = id + "_label";
    setLabelId(label?.id);
  }, [id, onlyText, explicitName]);

  if (onlyText != undefined) { //Text is scrollable in inputs
    if (ctx.readonlyAsPlainText) {
      return (
        <input id={id} {...attrs} readOnly className={classes(ctx.formControlPlainTextClass, attrs?.className, className)} tabIndex={0} value={onlyText} ref={innerRef as React.RefObject<HTMLInputElement>} />
      );
    } else {
      return (
        <input id={id} {...attrs} readOnly className={classes(ctx.formControlClass, attrs?.className, className)} tabIndex={0} value={onlyText} ref={innerRef as React.RefObject<HTMLInputElement>} />
      );
    }
  }
  else {
    // Empty or non-text content renders a div, which gets the same aria attributes as the input above. On a
    // div without a role, aria-label is prohibited and aria-readonly is not allowed at all (WCAG 4.1.2), so
    // the name was dropped. aria-readonly only means something on a widget and goes; a named div becomes a
    // group, which may carry the name and announces it before the content.
    const { "aria-readonly": _readonly, ...labelledAttrs } = attrs ?? {};
    // Named by the label only. It was the label and then the div's own content, so that a reader announcing only
    // the name would get the value too - but NVDA reads the group's content after its name, so every such field
    // was read twice: "Domain Project Durchführung … Project, grouping, Project Durchführung … Project" (NVDA
    // check 2026-10-01). JAWS is still to be checked.
    const divAttrs = labelId ? { ...labelledAttrs, "aria-labelledby": labelId } : labelledAttrs;
    const role = divAttrs.role ?? (divAttrs["aria-label"] || divAttrs["aria-labelledby"] ? "group" : undefined);

    if (ctx.readonlyAsPlainText) {
      return (
        <div id={id}  {...divAttrs} role={role} className={classes(ctx.formControlPlainTextClass, "readonly", attrs?.className, className)} tabIndex={0} ref={innerRef as React.RefObject<HTMLDivElement>}>
          {children ?? <span>&nbsp;</span>}
        </div>
      );
    } else {
      return (
        <div id={id} {...divAttrs} role={role} className={classes(ctx.formControlClass, "readonly", attrs?.className, className)} tabIndex={0} ref={innerRef as React.RefObject<HTMLDivElement>}>
          {children ?? <span>&nbsp;</span>}
        </div>
      );
    }
  }
}
