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
  const onlyText = array.length == 1 && typeof array[0] == "string" ? array[0] as string : undefined;

  if (onlyText) { //Text is scrollable in inputs
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
    const { "aria-readonly": _readonly, ...divAttrs } = attrs ?? {};
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
