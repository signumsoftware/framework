import * as React from 'react'
import { Dic, addClass, classes } from '../Globals'
import { LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'

export interface TextBoxLineProps extends ValueBaseProps<TextBoxLineController> {
  autoTrimString?: boolean;
  autoFixString?: boolean;
  datalist?: string[];
}

export class TextBoxLineController extends ValueBaseController<TextBoxLineProps>{
}

export const TextBoxLine = React.memo(React.forwardRef(function TextBoxLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "text");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const PasswordLine = React.memo(React.forwardRef(function PasswordLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "password");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const GuidLine = React.memo(React.forwardRef(function GuidLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "guid");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const ColorLine = React.memo(React.forwardRef(function ColorLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "color");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});


function internalTextBox(vl: TextBoxLineController, type: "password" | "color" | "text" | "guid") {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={htmlAtts} ctx={s.ctx} innerRef={vl.setRefs}>
          {s.ctx.value}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.value, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = TextBoxLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true, type == "guid");
      if (fixed != input.value)
        vl.setValue(fixed, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => <>
        {vl.withItemGroup(
          <input type={type == "color" || type == "guid" ? "text" : type}
            id={inputId}
            autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
            {...vl.props.valueHtmlAttributes}
            className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))}
            value={s.ctx.value ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
            placeholder={vl.getPlaceholder()}
            list={s.datalist ? s.ctx.getUniqueId("dataList") : undefined}
            ref={vl.setRefs} />,
          type == "color" ? <input type="color"
            className={classes(s.ctx.formControlClass, "sf-color")}
            value={s.ctx.value ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
          /> : undefined

        )
        }
        {s.datalist &&
          <datalist id={s.ctx.getUniqueId("dataList")}>
            {s.datalist.map((item, i) => <option key={i} value={item} />)}
          </datalist>
        }
      </>}
    </FormGroup>
  );
}

export interface ColorTextBoxProps {
  value: string | null;
  onChange: (newValue: string | null) => void;
  formControlClass?: string;
  groupClass?: string;
  textValueHtmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  groupHtmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: React.Ref<HTMLInputElement>;
}

export function ColorTextBox(p: ColorTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);

  const value = text != undefined ? text : p.value != undefined ? p.value : "";

  return (
    <span {...p.groupHtmlAttributes} className={addClass(p.groupHtmlAttributes, classes(p.groupClass))}>
      <input type="text"
        autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
        {...p.textValueHtmlAttributes}
        className={addClass(p.textValueHtmlAttributes, classes(p.formControlClass))}
        value={value}
        onBlur={handleOnBlur}
        onChange={handleOnChange}
        onFocus={handleOnFocus}
        ref={p.innerRef} />
      <input type="color"
        className={classes(p.formControlClass, "sf-color")}
        value={value}
        onBlur={handleOnBlur}
        onChange={handleOnChange}
      />
    </span>);

  function handleOnFocus(e: React.FocusEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;

    input.setSelectionRange(0, input.value != null ? input.value.length : 0);

    if (p.textValueHtmlAttributes?.onFocus)
      p.textValueHtmlAttributes.onFocus(e);
  };

  function handleOnBlur(e: React.FocusEvent<any>) {

    const input = e.currentTarget as HTMLInputElement;

    var result = input.value == undefined || input.value.length == 0 ? null : input.value;

    setText(undefined);
    if (p.value != result)
      p.onChange(result);
    if (p.textValueHtmlAttributes?.onBlur)
      p.textValueHtmlAttributes.onBlur(e);
  }

  function handleOnChange(e: React.SyntheticEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;
    setText(input.value);
    if (p.onChange)
      p.onChange(input.value);
  }
}
