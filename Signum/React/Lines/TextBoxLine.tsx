import * as React from 'react'
import { addClass, classes } from '../Globals'
import { LineBaseController, genericForwardRefWithMemo, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { TextBaseController, TextBaseProps } from './TextBase'

export interface TextBoxLineProps extends TextBaseProps<string | null> {
  datalist?: string[];
}

export class TextBoxLineController extends TextBaseController<TextBoxLineProps, string | null> {
  init(p: TextBoxLineProps) {
    super.init(p);
    this.assertType("TextBoxLine", ["string"]);
  }
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

export class PasswordLineController extends TextBaseController<TextBoxLineProps, string | null> {
  init(p: TextBoxLineProps) {
    super.init(p);
    this.assertType("PasswordLine", ["string"]);
  }
}

export const PasswordLine = genericForwardRefWithMemo(function PasswordLine<V extends string | null>(props: TextBoxLineProps, ref: React.Ref<PasswordLineController>) {

  const c = useController(PasswordLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "password");
}, (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export class GuidLineController extends TextBaseController<TextBoxLineProps, string | null> {
  init(p: TextBoxLineProps) {
    super.init(p);
    this.assertType("TextBoxLine", ["Guid"]);
  }
}

export const GuidLine = genericForwardRefWithMemo(function GuidLine<V extends string | null>(props: TextBoxLineProps, ref: React.Ref<GuidLineController>) {

  const c = useController(GuidLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "guid");
}, (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const ColorLine = genericForwardRefWithMemo(function ColorLine<V extends string | null>(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "color");
}, (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});


function internalTextBox<V extends string | null>(vl: TextBoxLineController, type: "password" | "color" | "text" | "guid") {

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

    if (s.triggerChange == "onBlur")
      vl.setTempValue(input.value as V)
    else
      vl.setValue(input.value as V, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false || s.triggerChange == "onBlur") {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = TextBoxLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true, type == "guid");
      if (fixed != s.ctx.value)
        vl.setValue(fixed as V, e);

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
            value={vl.getValue() ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
            placeholder={vl.getPlaceholder()}
            list={s.datalist ? s.ctx.getUniqueId("dataList") : undefined}
            ref={vl.setRefs} />,
          type == "color" ? <input type="color"
            className={classes(s.ctx.formControlClass, "sf-color")}
            value={vl.getValue() ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
          /> : undefined)
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
