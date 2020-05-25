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
}

export function FormControlReadonly({ ctx, htmlAttributes: attrs, className, innerRef, children }: FormControlReadonlyProps) {

  const array = React.Children.toArray(children);
  const onlyText = array.length == 1 && typeof array[0] == "string" ? array[0] as string : undefined;

  if (onlyText) { //Text is scrollable in inputs
    if (ctx.readonlyAsPlainText) {
      return (
        <input {...attrs} readOnly className={classes(ctx.formControlPlainTextClass, attrs?.className, className)} tabIndex={-1} value={onlyText} ref={innerRef as React.RefObject<HTMLInputElement>} />
      );
    } else {
      return (
        <input {...attrs} readOnly className={classes(ctx.formControlClass, attrs?.className, className)} tabIndex={-1} value={onlyText} ref={innerRef as React.RefObject<HTMLInputElement>} />
      );
    }
  }
  else {
    if (ctx.readonlyAsPlainText) {
      return (
        <div {...attrs} className={classes(ctx.formControlPlainTextClass, "readonly", attrs?.className, className)} ref={innerRef as React.RefObject<HTMLDivElement>}>
          {children ?? <span>&nbsp;</span>}
        </div>
      );
    } else {
      return (
        <div {...attrs} className={classes(ctx.formControlClass, "readonly", attrs?.className, className)} ref={innerRef as React.RefObject<HTMLDivElement>}>
          {children ?? <span>&nbsp;</span>}
        </div>
      );
    }
  }
}
