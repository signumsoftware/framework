import * as React from 'react'
import { DropdownList, Combobox } from 'react-widgets'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, tryGetTypeInfo } from '../Reflection'
import { LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum } from '../Signum.Entities'
import { getTimeMachineIcon } from './TimeMachineIcon'

export interface EnumLineProps extends LineBaseProps {
  format?: string;
  unit?: React.ReactChild;
  optionItems?: (OptionItem | MemberInfo | string)[];
  onRenderDropDownListItem?: (oi: OptionItem) => React.ReactNode;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: EnumLineController) => React.ReactNode;
  initiallyFocused?: boolean | number;
  valueRef?: React.Ref<HTMLElement>;
  lineType?:
    "DropDownList" | /*For Enums! (only values in optionItems can be selected)*/
    "ComboBoxText" | /*For Text! (with freedom to choose a different value not in optionItems)*/
    "RadioGroup";
  columnCount?: number;
  columnWidth?: number;
}

export interface OptionItem {
  value: any;
  label: string;
}

export class EnumLineController extends LineBaseController<EnumLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: EnumLineProps) {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);

    useInitiallyFocused(this.props.initiallyFocused, this.inputElement);    
  }

  setRefs = (node: HTMLElement | null) => {

    setRefProp(this.props.valueRef, node);

    (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  }

  overrideProps(state: EnumLineProps, overridenProps: EnumLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }

  withItemGroup(input: JSX.Element, preExtraButton?: JSX.Element): JSX.Element {

    if (!this.props.unit && !this.props.extraButtons && !preExtraButton) {
      return <>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
      </>;
    }

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
        {this.props.unit && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unit}</span>}
        {preExtraButton}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </div>
    );
  }
}

export const EnumLine = React.memo(React.forwardRef(function EnumLine(props: EnumLineProps, ref: React.Ref<EnumLineController>) {

  const c = useController(EnumLineController, props, ref);

  if (c.isHidden)
    return null;

  return props.lineType == 'ComboBoxText' ? internalComboBoxText(c) : props.lineType == 'RadioGroup' ? internalRadioGroup(c) : internalDropDownList(c);
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

function internalDropDownList(vl: EnumLineController) {

  var optionItems = getOptionsItems(vl);

  const s = vl.props;
  if (!s.type!.isNotNullable || s.ctx.value == undefined)
    optionItems = [{ value: null, label: " - " }].concat(optionItems);

  if (s.ctx.readOnly) {

    var label: string | null = null;
    if (s.ctx.value != undefined) {

      var item = optionItems.filter(a => a.value == s.ctx.value).singleOrNull();

      label = item ? item.label : s.ctx.value.toString();
    }

    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId =>
          vl.withItemGroup(
            <FormControlReadonly
              id={inputId}
              htmlAttributes={{
                ...vl.props.valueHtmlAttributes,
                ...({ 'data-value': s.ctx.value } as any) /*Testing*/
              }} ctx={s.ctx} innerRef={vl.setRefs}>
              {label}
            </FormControlReadonly>)
        }
      </FormGroup>
    );
  }

  function toStr(val: any) {
    return val == null ? "" :
      val === true ? "True" :
        val === false ? "False" :
          val.toString();
  }

  if (vl.props.onRenderDropDownListItem) {

    var oi = optionItems.singleOrNull(a => a.value == s.ctx.value) ?? {
      value: s.ctx.value,
      label: s.ctx.value,
    };

    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(
          <DropdownList<OptionItem> className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass, "p-0"))} data={optionItems}
            id={inputId}
            onChange={(oe, md) => vl.setValue(oe.value, md.originalEvent)}
            value={oi}
            filter={false}
            autoComplete="off"
            dataKey="value"
            textField="label"
            renderValue={a => vl.props.onRenderDropDownListItem!(a.item)}
            renderListItem={a => vl.props.onRenderDropDownListItem!(a.item)}
            {...(s.valueHtmlAttributes as any)}
          />)
        }
      </FormGroup>
    );
  } else {

    const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      const option = optionItems.filter(a => toStr(a.value) == input.value).single();
      vl.setValue(option.value, e);
    };

    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(
          <select id={inputId} {...vl.props.valueHtmlAttributes} value={toStr(s.ctx.value)} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formSelectClass, vl.mandatoryClass))} onChange={handleEnumOnChange} >
            {!optionItems.some(a => toStr(a.value) == toStr(s.ctx.value)) && <option key={-1} value={toStr(s.ctx.value)}>{toStr(s.ctx.value)}</option>}
            {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
          </select>)
        }
      </FormGroup>
    );
  }
}

function internalComboBoxText(el: EnumLineController) {

  var optionItems = getOptionsItems(el);

  const s = el.props;
  if (!s.type!.isNotNullable || s.ctx.value == undefined)
    optionItems = [{ value: null, label: " - " }].concat(optionItems);

  if (s.ctx.readOnly) {

    var label: string | null = null;
    if (s.ctx.value != undefined) {

      var item = optionItems.filter(a => a.value == s.ctx.value).singleOrNull();

      label = item ? item.label : s.ctx.value.toString();
    }

    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...el.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => el.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={{
            ...el.props.valueHtmlAttributes,
            ...({ 'data-value': s.ctx.value } as any) /*Testing*/
          }} ctx={s.ctx} innerRef={el.setRefs}>
            {label}
          </FormControlReadonly>)}
      </FormGroup>
    );
  }


  var renderItem = el.props.onRenderDropDownListItem ? (a: any) => el.props.onRenderDropDownListItem!(a.item) : undefined;

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...el.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => el.withItemGroup(
        <Combobox<OptionItem> id={inputId} className={addClass(el.props.valueHtmlAttributes, classes(s.ctx.formControlClass, el.mandatoryClass))} data={optionItems} onChange={(e: string | OptionItem, md) => {
          el.setValue(e == null ? null : typeof e == "string" ? e : e.value, md.originalEvent);
        }} value={s.ctx.value}
          dataKey="value"
          textField="label"
          focusFirstItem
          autoSelectMatches
          renderListItem={renderItem}
          {...(s.valueHtmlAttributes as any)}
        />)
      }
    </FormGroup>
  );
}

function internalRadioGroup(elc: EnumLineController) {

  var optionItems = getOptionsItems(elc);

  const s = elc.props;

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => a.value == input.value).single();
    elc.setValue(option.value, e);
  };

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...elc.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => <>
        {getTimeMachineIcon({ ctx: s.ctx })}
        <div style={getColumnStyle()}>
          {optionItems.map((oi, i) =>
            <label {...elc.props.valueHtmlAttributes} className={classes("sf-radio-element", elc.props.ctx.errorClass)}>
              <input type="radio" key={i} value={oi.value} checked={s.ctx.value == oi.value} onChange={handleEnumOnChange} disabled={s.ctx.readOnly} />
              {" " + oi.label}
            </label>)}
        </div>
      </>}
    </FormGroup>
  );

  function getColumnStyle(): React.CSSProperties | undefined {

    const p = elc.props;

    if (p.columnCount && p.columnWidth)
      return {
        columns: `${p.columnCount} ${p.columnWidth}px`,
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
      };

    return undefined;
  }
}


function getOptionsItems(el: EnumLineController): OptionItem[] {

  var ti = tryGetTypeInfo(el.props.type!.name);

  if (el.props.optionItems) {
    return el.props.optionItems
      .map(a => typeof a == "string" && ti != null && ti.kind == "Enum" ? toOptionItem(ti.members[a]) : toOptionItem(a))
      .filter(a => !!a);
  }

  if (el.props.type!.name == "boolean")
    return ([
      { label: BooleanEnum.niceToString("False")!, value: false },
      { label: BooleanEnum.niceToString("True")!, value: true }
    ]);

  if (ti != null && ti.kind == "Enum")
    return Dic.getValues(ti.members).map(m => toOptionItem(m));

  throw new Error("Unable to get Options from " + el.props.type!.name);
}

function toOptionItem(m: MemberInfo | OptionItem | string): OptionItem {

  if (typeof m == "string")
    return {
      value: m,
      label: m,
    }

  if ((m as MemberInfo).name)
    return {
      value: (m as MemberInfo).name,
      label: (m as MemberInfo).niceName,
    };

  return m as OptionItem;
}
