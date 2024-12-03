import * as React from 'react'
import { StyleContext, TypeContext } from '../Lines';
import { classes } from '../Globals';
import "./Lines.css"

export interface FormGroupProps {
  label?: React.ReactNode;
  labelIcon?: React.ReactNode;
  ctx: StyleContext;
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  helpText?: React.ReactNode;
  children?: (inputId: string) => React.ReactNode;
}

export function FormGroup(p: FormGroupProps): React.JSX.Element {
  const ctx = p.ctx;
  const controlId = React.useId();

  const tCtx = ctx as TypeContext<any>;
  const errorClass = tCtx.errorClass;
  const errorAtts = tCtx.errorAttributes && tCtx.errorAttributes();

  if (ctx.formGroupStyle == "None") {
    const c = p.children?.(controlId);

    return (
      <span {...p.htmlAttributes} className={classes(errorClass, p.htmlAttributes?.className)} {...errorAtts}>
        {c}
      </span>
    );
  }

  const labelClasses = classes(
    ctx.formGroupStyle == "SrOnly" && "visually-hidden",
    ctx.formGroupStyle == "LabelColumns" && ctx.labelColumnsCss,
    ctx.formGroupStyle == "LabelColumns" ? ctx.colFormLabelClass : ctx.labelClass,
  );

  let pr = tCtx.propertyRoute;
  var labelText = p.label ?? (pr?.member?.niceName);
  const label = (
    <label htmlFor={controlId} {...p.labelHtmlAttributes} className={classes(p.labelHtmlAttributes?.className, labelClasses)} >
      {labelText} {p.labelIcon}
    </label>
  );

  const formGroupClasses = classes(ctx.formGroupClass,
    ctx.formGroupStyle == "LabelColumns" ? "row" : undefined,
    ctx.formGroupStyle == "FloatingLabel" ? "form-floating" : undefined,
    errorClass);
  return (
    <div
      title={ctx.titleLabels && typeof labelText == "string" ? labelText : undefined}
      {...p.htmlAttributes}
      className={classes(p.htmlAttributes?.className, formGroupClasses)}
      {...errorAtts}>
      {(ctx.formGroupStyle == "Basic" || ctx.formGroupStyle == "LabelColumns" || ctx.formGroupStyle == "SrOnly") && label}
      {
        ctx.formGroupStyle != "LabelColumns" ? p.children?.(controlId) :
          (
            <div className={ctx.valueColumnsCss} >
              {p.children?.(controlId)}
              {p.helpText && ctx.formGroupStyle == "LabelColumns" && <small className="form-text d-block">{p.helpText}</small>}
            </div>
          )
      }
      {(ctx.formGroupStyle == "BasicDown" || ctx.formGroupStyle == "FloatingLabel") && label}
      {p.helpText && ctx.formGroupStyle != "LabelColumns" && <small className="form-text d-block">{p.helpText}</small>}
    </div>
  );

}
