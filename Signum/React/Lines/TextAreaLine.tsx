import * as React from 'react'
import { classes } from '../Globals'
import { genericMemo, LineBaseController, LineBaseProps, tasks, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { EntityControlMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { getTimeMachineIcon } from './TimeMachineIcon'
import { TextBoxLineController } from './TextBoxLine'
import { useForceUpdate } from '../Hooks'
import { TextBaseController, TextBaseProps } from './TextBase'

export interface TextAreaLineProps extends TextBaseProps<string | null> {
  autoResize?: boolean;
  charCounter?: true | ((length: number) => React.ReactElement | string);
  ref?: React.Ref<TextAreaLineController>;
}

export class TextAreaLineController extends TextBaseController<TextAreaLineProps, string | null> {
  override init(p: TextAreaLineProps): void {
    super.init(p);
    this.assertType("TextAreaLine", ["string"]);
  }
}

export const TextAreaLine: (props: TextAreaLineProps) => React.ReactNode | null
  = genericMemo(function TextAreaLine(props: TextAreaLineProps) {

  const c = useController(TextAreaLineController, props);
  const ccRef = React.useRef<ChartCounterHandler>(null);
  if (c.isHidden)
    return null;

  const p = c.props;

  const isLabelVisible = !(p.ctx.formGroupStyle === "SrOnly" || "visually-hidden");
  var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
  if (!isLabelVisible && p.label) {
    ariaAtts = { ...ariaAtts, "aria-label": typeof p.label === "string" ? p.label : String(p.label) };
  }

  var htmlAtts = c.props.valueHtmlAttributes;
  var mergedHtmlReadOnly = { ...htmlAtts, ...ariaAtts };

  var autoResize = p.autoResize ?? (htmlAtts?.style?.height == null && htmlAtts?.rows == null);
  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  if (p.ctx.readOnly)
    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => <>
          {getTimeMachineIcon({ ctx: p.ctx })}
          <TextArea id={inputId} {...mergedHtmlReadOnly} autoResize={autoResize} className={classes(htmlAtts?.className, p.ctx.formControlClass, c.mandatoryClass)} value={p.ctx.value || ""}
            disabled />
        </>}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;

    if (p.triggerChange == "onBlur")
      c.setTempValue(input.value)
    else
      c.setValue(input.value, e);

    ccRef.current?.setCurrentLength(input.value.length);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (p.autoFixString != false || p.triggerChange == "onBlur") {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = TextAreaLineController.autoFixString(input.value, p.autoTrimString != null ? p.autoTrimString : false, false);
      if (fixed != (p.ctx.value ?? ""))
        c.setValue(fixed!, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }


  var cc = c.props.charCounter == null ? null :
    c.props.charCounter == true && c.props.valueHtmlAttributes?.maxLength ? <ChartCounter myRef={ccRef}>{v => {
      var rem = c.props.valueHtmlAttributes!.maxLength! - v;
      return EntityControlMessage._0CharactersRemaining.niceToString().forGenderAndNumber(rem).formatHtml(<strong className={rem <= 0 ? "text-danger" : undefined}>{rem}</strong>)
    }}</ChartCounter> :

      c.props.charCounter == true ? <ChartCounter myRef={ccRef}>{v => EntityControlMessage._0Characters.niceToString().forGenderAndNumber(p).formatHtml(<strong>{v}</strong>)}</ChartCounter > :
        <ChartCounter myRef={ccRef}>{c.props.charCounter}</ChartCounter>;

  var mergedHtml = { ...htmlAtts, ...ariaAtts, ...c.props.valueHtmlAttributes };
  return (
    <FormGroup ctx={p.ctx} error={c.getError()} label={p.label} labelIcon={p.labelIcon}
      helpText={helpText && cc ? <>{cc}<br />{helpText}</> : (cc ?? helpText)}
      helpTextOnTop={helpTextOnTop}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
      {inputId => c.withItemGroup(
        <TextArea {...mergedHtml}
          autoResize={autoResize}
          className={classes(c.props.valueHtmlAttributes?.className, p.ctx.formControlClass, c.mandatoryClass)}
          value={c.getValue() ?? ""}
          id={inputId}
          minHeight={c.props.valueHtmlAttributes?.style?.minHeight?.toString()}
          onChange={handleTextOnChange}
          onBlur={handleBlur ?? htmlAtts?.onBlur}

          placeholder={c.getPlaceholder()}
          innerRef={c.setRefs} />
        , undefined, true)}
    </FormGroup>
  );
}, (prev, next) => {
  return LineBaseController.propEquals(prev, next);
});

interface ChartCounterHandler {
  setCurrentLength: (length: number) => void;
}

function ChartCounter(p: { children: (length: number) => React.ReactElement | string, myRef: React.Ref<ChartCounterHandler> }) {

  var forceUpdate = useForceUpdate();
  var valueRef = React.useRef(0);

  React.useImperativeHandle(p.myRef, () => {

    var handle: number | undefined = undefined;
    var queued = false;

    return ({
      setCurrentLength: (val) => {
        valueRef.current = val;
        if (!handle) {
          forceUpdate();
          handle = window.setTimeout(() => {
            handle = undefined;
            if (queued) {
              queued = false;
              forceUpdate();
            }
          }, 200);
        } else {
          queued = true;
        }
      }
    });
  }, []);

  return p.children(valueRef.current);
}

export let maxValueLineSize = 100;

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
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

      // if(lineBase instanceof TextAreaLineController)
      //   s.charCounter = true;
    }
  }
}
