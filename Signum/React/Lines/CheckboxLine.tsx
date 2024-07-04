import * as React from 'react'
import { Dic, classes } from '../Globals'
import { LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { TypeContext } from '../Lines'

export interface CheckboxLineProps extends ValueBaseProps<boolean | null> {
  inlineCheckbox?: boolean | "block";
}

export class CheckboxLineController extends ValueBaseController<CheckboxLineProps, boolean | null>{

}

export const CheckboxLine: React.MemoExoticComponent<React.ForwardRefExoticComponent<CheckboxLineProps & React.RefAttributes<CheckboxLineController>>> =
  React.memo(React.forwardRef(function CheckboxLine(props: CheckboxLineProps, ref: React.Ref<CheckboxLineController>) {

  const c = useController(CheckboxLineController, props, ref);

  if (c.isHidden)
    return null;

  const s = c.props;
  const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    c.setValue(input.checked, e);
  };

  if (s.inlineCheckbox) {

    var { style, className, ...otherAtts } = { ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes, ...s.labelHtmlAttributes };
    return (
      <label style={{ display: s.inlineCheckbox == "block" ? "block" : undefined, ...style }} {...otherAtts} className={classes(s.ctx.labelClass, c.props.ctx.errorClass, className)}>
        {getTimeMachineIcon({ ctx: s.ctx })}
        <input type="checkbox" {...c.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly}
          className={classes(c.props.valueHtmlAttributes?.className, "form-check-input")}
        />
        {" "}{s.label}{s.labelIcon && " "}{s.labelIcon}
        {s.helpText && <small className="d-block form-text text-muted">{s.helpText}</small>}
      </label>
    );
  }
  else {
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
        {inputId => <>
          {getTimeMachineIcon({ ctx: s.ctx })}
          <input id={inputId} type="checkbox" {...c.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
            className={classes(c.props.valueHtmlAttributes?.className, "form-check-input")} disabled={s.ctx.readOnly} />
        </>
        }
      </FormGroup>
    );
  }

}), (prev, next) => {

  return LineBaseController.propEquals(prev, next);
});
