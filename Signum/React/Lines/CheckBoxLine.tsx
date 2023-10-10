import * as React from 'react'
import { Dic, addClass, classes } from '../Globals'
import { LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { getTimeMachineIcon } from './TimeMachineIcon'

export interface CheckBoxLineProps extends LineBaseProps {
  inlineCheckbox?: boolean | "block";
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  initiallyFocused?: boolean | number;

  valueRef?: React.Ref<HTMLElement>;
}

export class CheckBoxLineController extends LineBaseController<CheckBoxLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: CheckBoxLineProps) {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);

    useInitiallyFocused(this.props.initiallyFocused, this.inputElement);
  }

  setRefs = (node: HTMLElement | null) => {

    setRefProp(this.props.valueRef, node);

    (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  }

  overrideProps(state: CheckBoxLineProps, overridenProps: CheckBoxLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }
}

export const CheckBoxLine = React.memo(React.forwardRef(function CheckBoxLine(props: CheckBoxLineProps, ref: React.Ref<CheckBoxLineController>) {

  const c = useController(CheckBoxLineController, props, ref);

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
          className={addClass(c.props.valueHtmlAttributes, classes("form-check-input"))}
        />
        {" "}{s.label}
        {s.helpText && <small className="form-text text-muted">{s.helpText}</small>}
      </label>
    );
  }
  else {
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
        {inputId => <>
          {getTimeMachineIcon({ ctx: s.ctx })}
          <input id={inputId} type="checkbox" {...c.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
            className={addClass(c.props.valueHtmlAttributes, classes("form-check-input"))} disabled={s.ctx.readOnly} />
        </>
        }
      </FormGroup>
    );
  }

}), (prev, next) => {

  return LineBaseController.propEquals(prev, next);
});
