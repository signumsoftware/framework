import * as React from 'react'
import { Dic, classes } from '../Globals'
import { genericMemo, LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { TypeContext } from '../Lines'

export interface CheckboxLineProps extends ValueBaseProps<boolean | null> {
  inlineCheckbox?: boolean | "block";
  ref?: React.Ref<CheckboxLineController>;
}

export class CheckboxLineController extends ValueBaseController<CheckboxLineProps, boolean | null>{

}

export const CheckboxLine: (props: CheckboxLineProps) => React.ReactNode | null =
  genericMemo(function CheckboxLine(props: CheckboxLineProps) {

    const c = useController(CheckboxLineController, props);

    if (c.isHidden)
      return null;

    const p = c.props;
    const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      c.setValue(input.checked, e);
    };

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);

    if (p.inlineCheckbox) {

      var { style, className, ...otherAtts } = { ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...p.labelHtmlAttributes };
      return (
        <label style={{ display: p.inlineCheckbox == "block" ? "block" : undefined, ...style }} {...otherAtts} className={classes(p.ctx.labelClass, c.getErrorClass(), className)}>
          {getTimeMachineIcon({ ctx: p.ctx })}
          <input type="checkbox" {...c.props.valueHtmlAttributes} checked={p.ctx.value || false} onChange={handleCheckboxOnChange} disabled={p.ctx.readOnly}
            className={classes(c.props.valueHtmlAttributes?.className, "form-check-input")}
          />
          {" "}{p.label}{p.labelIcon && " "}{p.labelIcon}
          {p.helpText && <small className="d-block form-text text-muted">{helpText}</small>}
        </label>
      );
    }
    else {
      const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);
      return (
        <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
          {inputId => <>
            {getTimeMachineIcon({ ctx: p.ctx })}
            <input id={inputId} type="checkbox" {...c.props.valueHtmlAttributes} checked={p.ctx.value || false} onChange={handleCheckboxOnChange}
              className={classes(c.props.valueHtmlAttributes?.className, "form-check-input")} disabled={p.ctx.readOnly} />
          </>
          }
        </FormGroup>
      );
    }

  }, (prev, next) => {
    return LineBaseController.propEquals(prev, next);
  });
