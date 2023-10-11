import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { DatePicker, DropdownList, Combobox } from 'react-widgets'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, setRefProp, tasks, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyCodes } from '../Components/Basic';
import { getTimeMachineIcon } from './TimeMachineIcon'
import { TextBoxLineController } from './TextBoxLine'

export interface TextAreaLineProps extends LineBaseProps {
  autoTrimString?: boolean;
  autoFixString?: boolean;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: TextAreaLineController) => React.ReactNode;
  initiallyFocused?: boolean | number;
  valueRef?: React.Ref<HTMLElement>;
}

export class TextAreaLineController extends LineBaseController<TextAreaLineProps>{

  inputElement!: React.RefObject<HTMLElement>;

  init(p: TextAreaLineProps) {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);

    useInitiallyFocused(this.props.initiallyFocused, this.inputElement);
  }

  setRefs = (node: HTMLElement | null) => {

    setRefProp(this.props.valueRef, node);

    (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  }

  static autoFixString(str: string | null | undefined, autoTrim: boolean, autoNull : boolean): string | null | undefined {

    if (autoTrim)
      str = str?.trim();

    return str == "" && autoNull ? null : str;
  }

  overrideProps(state: TextAreaLineProps, overridenProps: TextAreaLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }

  withItemGroup(input: JSX.Element, preExtraButton?: JSX.Element): JSX.Element {

    if (!this.props.extraButtons && !preExtraButton) {
      return <>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
      </>;
    }

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
        {preExtraButton}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </div>
    );
  }

  getPlaceholder(): string | undefined {
    const p = this.props;
    return p.valueHtmlAttributes?.placeholder ??
      ((p.ctx.placeholderLabels || p.ctx.formGroupStyle == "FloatingLabel") ? asString(p.label) :
      undefined);
  }
}

function asString(reactChild: React.ReactNode | undefined): string | undefined {
  if (typeof reactChild == "string")
    return reactChild as string;

  return undefined;
}

export const TextAreaLine = React.memo(React.forwardRef(function TextAreaLine(props: TextAreaLineProps, ref: React.Ref<TextAreaLineController>) {

  const c = useController(TextAreaLineController, props, ref);

  if (c.isHidden)
    return null;

  const s = c.props;

  var htmlAtts = c.props.valueHtmlAttributes;
  var autoResize = htmlAtts?.style?.height == null && htmlAtts?.rows == null;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => <>
          {getTimeMachineIcon({ ctx: s.ctx })}
          <TextArea id={inputId} {...htmlAtts} autoResize={autoResize} className={addClass(htmlAtts, classes(s.ctx.formControlClass, c.mandatoryClass))} value={s.ctx.value || ""}
            disabled />
        </>}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    c.setValue(input.value, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = TextAreaLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false, false);
      if (fixed != input.value)
        c.setValue(fixed, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  const handleOnFocus = (e: React.FocusEvent<any>) => {
    console.log("onFocus handler called");
    if (htmlAtts?.onFocus) {
      console.log("passed onFocus called");

      htmlAtts?.onFocus(e);
    }
  }

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => c.withItemGroup(
        <TextArea {...c.props.valueHtmlAttributes} autoResize={autoResize} className={addClass(c.props.valueHtmlAttributes, classes(s.ctx.formControlClass, c.mandatoryClass))} value={s.ctx.value || ""}
          id={inputId}
          minHeight={c.props.valueHtmlAttributes?.style?.minHeight?.toString()}
          onChange={handleTextOnChange}
          onBlur={handleBlur ?? htmlAtts?.onBlur}
          onFocus={handleOnFocus}
          placeholder={c.getPlaceholder()}
          innerRef={c.setRefs} />
      )}
    </FormGroup>
  );
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export let maxValueLineSize = 100;

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBaseController<any>, state: LineBaseProps) {
  const vl = lineBase instanceof TextBoxLineController || lineBase instanceof TextAreaLineController ? lineBase : undefined;
  const pr = state.ctx.propertyRoute;
  const s = state as TextAreaLineProps;
  if (vl && pr?.propertyRouteType == "Field") {

    var member = pr.member!;

    if (member.maxLength != undefined && !s.ctx.readOnly) {

      if (!s.valueHtmlAttributes)
        s.valueHtmlAttributes = {};

      if (s.valueHtmlAttributes.maxLength == undefined)
        s.valueHtmlAttributes.maxLength = member.maxLength;

      if (s.valueHtmlAttributes.size == undefined)
        s.valueHtmlAttributes.size = maxValueLineSize == undefined ? member.maxLength : Math.min(maxValueLineSize, member.maxLength);
    }
  }
}
