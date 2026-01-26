import * as React from 'react'
import { classes } from '../Globals'
import { genericMemo, LineBaseController, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { TextBaseController, TextBaseProps } from './TextBase'

export interface TextBoxLineProps extends TextBaseProps<string | null> {
  datalist?: string[];
  ref?: React.Ref<TextBoxLineController>;
}

export class TextBoxLineController extends TextBaseController<TextBoxLineProps, string | null> {
  override init(p: TextBoxLineProps): void {
    super.init(p);
    this.assertType("TextBoxLine", ["string"]);
  }
}

export const TextBoxLine: (props: TextBoxLineProps) => React.ReactNode | null =
  genericMemo(function TextBoxLine(props: TextBoxLineProps) {

    const c = useController(TextBoxLineController, props);

    if (c.isHidden)
      return null;

    return internalTextBox(c, "text");
  }, (prev, next) => {
    return LineBaseController.propEquals(prev, next);
  });

export interface PasswordLineProps extends TextBaseProps<string | null> {
  ref?: React.Ref<PasswordLineController>;
}


export class PasswordLineController extends TextBaseController<PasswordLineProps, string | null> {
  override init(p: PasswordLineProps): void {
    super.init(p);
    this.assertType("PasswordLine", ["string"]);
  }
}

export const PasswordLine: <V extends string | null>(props: PasswordLineProps) => React.ReactNode | null =
  genericMemo(function PasswordLine<V extends string | null>(props: PasswordLineProps): React.JSX.Element | null {

    const c = useController(PasswordLineController, props);

    if (c.isHidden)
      return null;

    return internalTextBox(c, "password");
  }, (prev, next) => {
    if (next.extraButtons || prev.extraButtons)
      return false;

    return LineBaseController.propEquals(prev, next);
  });

export interface GuidLineProps extends TextBaseProps<string | null> {
  ref?: React.Ref<GuidLineController>;
}

export class GuidLineController extends TextBaseController<GuidLineProps, string | null> {
  override init(p: GuidLineProps): void {
    super.init(p);
    this.assertType("GuidLine", ["Guid"]);
  }
}



export const GuidLine: <V extends string | null>(props: GuidLineProps) => React.ReactNode | null =
  genericMemo(function GuidLine<V extends string | null>(props: GuidLineProps) {

    const c = useController(GuidLineController, props);

    if (c.isHidden)
      return null;

    return internalTextBox(c, "guid");
  }, (prev, next) => {
    return LineBaseController.propEquals(prev, next);
  });

export interface ColorLineProps extends TextBaseProps<string | null> {
  ref?: React.Ref<TextBoxLineController>
}

export class ColorLineController extends TextBaseController<ColorLineProps, string | null> {
  override init(p: TextBoxLineProps): void {
    super.init(p);
    this.assertType("TextBoxLine", ["Guid"]);
  }
}

export const ColorLine: <V extends string | null>(props: ColorLineProps) => React.ReactNode | null
  = genericMemo(function ColorLine<V extends string | null>(props: ColorLineProps) {

  const c = useController(TextBoxLineController, props);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "color");
}, (prev, next) => {
  return LineBaseController.propEquals(prev, next);
});


function internalTextBox<V extends string | null>(c: TextBoxLineController, type: "password" | "color" | "text" | "guid") {

  const p = c.props;
  const isLabelVisible = !(p.ctx.formGroupStyle === "SrOnly" || "visually-hidden");
  var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
  if (!isLabelVisible && p.label) {
    ariaAtts = { ...ariaAtts, "aria-label": typeof p.label === "string" ? p.label : String(p.label) };
  }

  var htmlAtts = c.props.valueHtmlAttributes;
  var mergedHtmlReadOnly = { ...htmlAtts, ...ariaAtts };


  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  if (p.ctx.readOnly)
    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => c.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={mergedHtmlReadOnly} ctx={p.ctx} innerRef={c.setRefs}>
          {p.ctx.value}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;

    if (p.triggerChange == "onBlur")
      c.setTempValue(input.value as V)
    else
      c.setValue(input.value as V, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (p.autoFixString != false || p.triggerChange == "onBlur") {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = TextBoxLineController.autoFixString(input.value, p.autoTrimString != null ? p.autoTrimString : true, type == "guid");
      if (fixed != (p.ctx.value ?? ""))
        c.setValue(fixed as V, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  var mergedHtml = { ...htmlAtts, ...ariaAtts };
  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
      {inputId => <>
        {c.withItemGroup(
          <input type={type == "color" || type == "guid" ? "text" : type}
            id={inputId}
            autoComplete="off" 
            {...mergedHtml}
            className={classes(c.props.valueHtmlAttributes?.className, p.ctx.formControlClass, c.mandatoryClass)}
            value={c.getValue() ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
            placeholder={c.getPlaceholder()}
            list={p.datalist ? p.ctx.getUniqueId("dataList") : undefined}
            ref={c.setRefs} />,
          type == "color" ? <input type="color"
            className={classes(p.ctx.formControlClass, "sf-color")}
            value={c.getValue() ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
          /> : undefined)
        }
        {p.datalist &&
          <datalist id={p.ctx.getUniqueId("dataList")}>
            {p.datalist.map((item, i) => <option key={i} value={item} />)}
          </datalist>
        }
      </>}
    </FormGroup>
  );
}
