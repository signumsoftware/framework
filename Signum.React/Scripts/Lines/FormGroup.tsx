import * as React from 'react'
import { StyleContext, TypeContext } from '../Lines';
import { classes, addClass } from '../Globals';
import "./Lines.css"

export interface FormGroupProps {
  label?: React.ReactNode;
  controlId?: string;
  ctx: StyleContext;
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  helpText?: React.ReactNode;
  children?: React.ReactNode;
}

export function FormGroup(p: FormGroupProps) {
  const ctx = p.ctx;
  const tCtx = ctx as TypeContext<any>;
  const errorClass = tCtx.errorClass;
  const errorAtts = tCtx.errorAttributes && tCtx.errorAttributes();

  if (ctx.formGroupStyle == "None") {
    const c = p.children as React.ReactElement<any>;

    return (
      <span {...p.htmlAttributes} className={errorClass} {...errorAtts}>
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
    <label htmlFor={p.controlId} {...p.labelHtmlAttributes} className={addClass(p.labelHtmlAttributes, labelClasses)} >
      {labelText}
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
      className={addClass(p.htmlAttributes, formGroupClasses)}
      {...errorAtts}>
      {(ctx.formGroupStyle == "Basic" || ctx.formGroupStyle == "LabelColumns" || ctx.formGroupStyle == "SrOnly") && label}
      {
        ctx.formGroupStyle != "LabelColumns" ? p.children :
          (
            <div className={ctx.valueColumnsCss} >
              {p.children}
              {p.helpText && ctx.formGroupStyle == "LabelColumns" && <small className="form-text d-block">{p.helpText}</small>}
            </div>
          )
      }
      {(ctx.formGroupStyle == "BasicDown" || ctx.formGroupStyle == "FloatingLabel") && label}
      {p.helpText && ctx.formGroupStyle != "LabelColumns" && <small className="form-text d-block">{p.helpText}</small>}
    </div>
  );

}
