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
  ariaAttributes?: React.AriaAttributes;
  helpText?: React.ReactNode;
  helpTextOnTop?: React.ReactNode;
  error?: string | null | undefined;
  children?: (inputId: string) => React.ReactNode;
}

export function FormGroup(p: FormGroupProps): React.ReactElement {
  const ctx = p.ctx;
  const controlId = React.useId();

  const tCtx = ctx as TypeContext<any>;
  const error = p.error === undefined ? tCtx.error : p.error;
  const errorClass = error && "has-error";
  const errorAtts = error && {
    title: error,
    "data-error-path": tCtx.prefix
  };

  // The control's aria-describedby points at this id whenever it is invalid, but nothing ever rendered it,
  // so the reference was dead: a screen reader said the field was invalid without saying why. The message
  // was only in a title attribute, which is a tooltip and reaches nobody who is not using a mouse. Hidden
  // rather than shown, because the visible red styling and the validation summary already cover the sighted
  // case and putting it inline would restyle every invalid field in the application.
  const errorMessage = error &&
    <span id={tCtx.getUniqueId("error")} className="visually-hidden">{error}</span>;

  if (ctx.formGroupStyle == "None") {
    const c = p.children?.(controlId);

    return (
      <span {...p.htmlAttributes} className={classes(errorClass, p.htmlAttributes?.className)} {...errorAtts}>
        {c}
        {errorMessage}
      </span>
    );
  }

  const requiredIndicator = tCtx.propertyRoute?.member?.required && !p.ariaAttributes?.['aria-readonly'];

  const labelClasses = classes(
    ctx.formGroupStyle == "SrOnly" && "visually-hidden",
    ctx.formGroupStyle == "LabelColumns" && ctx.labelColumnsCss,
    ctx.formGroupStyle == "LabelColumns" ? ctx.colFormLabelClass : ctx.labelClass,
  );

  let pr = tCtx.propertyRoute;
  var labelText = p.label ?? (pr?.member?.niceName);

  // Not every line puts controlId on a real element: one that hands it to a third-party widget ends up
  // with the id on a wrapper or suffixed (react-widgets makes `${id}_input`), and one that renders a link
  // rather than a field has nothing to put it on at all. The label then pointed at an id that does not
  // exist, so clicking it focused nothing - measurable on a date, an entity picker and a markdown line -
  // and the association it claimed was not there. Those controls carry their own aria-label, so the name
  // survives; what is restored here is the click. The attribute is dropped rather than left dangling, so
  // the markup does not claim an association it does not have.
  const groupRef = React.useRef<HTMLDivElement>(null);
  const [hasControl, setHasControl] = React.useState(true);
  React.useLayoutEffect(() => { setHasControl(document.getElementById(controlId) != null); });

  function focusControl(e: React.MouseEvent<HTMLLabelElement>) {
    if (hasControl)
      return;
    // Links too: an entity line holding a value renders the entity as a link rather than a field, and that
    // link is what the label names.
    const target = groupRef.current?.querySelector<HTMLElement>(
      'input:not([type=hidden]):not([disabled]), select:not([disabled]), textarea:not([disabled]), [contenteditable="true"], a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])');
    if (target) {
      e.preventDefault();
      target.focus();
    }
  }

  const label = (
    <label htmlFor={hasControl ? controlId : undefined} onClick={focusControl}
      {...p.labelHtmlAttributes} className={classes(p.labelHtmlAttributes?.className, labelClasses)} >
      {labelText}{requiredIndicator && <span aria-hidden="true" className="required-indicator">*</span>} {p.labelIcon}
    </label>
  );

  const formGroupClasses = classes(ctx.formGroupClass,
    ctx.formGroupStyle == "LabelColumns" ? "row" : undefined,
    ctx.formGroupStyle == "FloatingLabel" ? "form-floating" : undefined,
    errorClass);
  return (
    <div
      ref={groupRef}
      title={ctx.titleLabels && typeof labelText == "string" ? labelText : undefined}
      {...p.htmlAttributes}
      className={classes(p.htmlAttributes?.className, formGroupClasses)}
      {...errorAtts}>
      {(ctx.formGroupStyle == "Basic" || ctx.formGroupStyle == "LabelColumns" || ctx.formGroupStyle == "SrOnly") && label}
      {p.helpTextOnTop && ctx.formGroupStyle != "LabelColumns" && <small className="form-text d-block">{p.helpTextOnTop}</small>}
      {
        ctx.formGroupStyle != "LabelColumns" ? p.children?.(controlId) :
          (
            <div className={ctx.valueColumnsCss} >
              {p.helpTextOnTop && ctx.formGroupStyle == "LabelColumns" && <small className="form-text d-block">{p.helpTextOnTop}</small>}
              {p.children?.(controlId)}
              {p.helpText && ctx.formGroupStyle == "LabelColumns" && <small id={tCtx.getUniqueId("help")} className="form-text d-block">{p.helpText}</small>}
            </div>
          )
      }
      {(ctx.formGroupStyle == "BasicDown" || ctx.formGroupStyle == "FloatingLabel") && label}
      {p.helpText && ctx.formGroupStyle != "LabelColumns" && <small id={tCtx.getUniqueId("help")} className="form-text d-block">{p.helpText}</small>}
      {errorMessage}
    </div>
  );

}
