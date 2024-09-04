import * as React from 'react'
import { DropdownList, Combobox, Value } from 'react-widgets'
import { Dic, classes } from '../Globals'
import { MemberInfo, tryGetTypeInfo } from '../Reflection'
import { LineBaseController, LineBaseProps, genericForwardRef, genericForwardRefWithMemo, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum } from '../Signum.Entities'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'

export interface EnumLineProps<V extends string | number | boolean | null> extends ValueBaseProps<V> {
  lineType?:
  "DropDownList" | /*For Enums! (only values in optionItems can be selected)*/
  "ComboBoxText" | /*For Text! (with freedom to choose a different value not in optionItems)*/
  "RadioGroup";
  optionItems?: (OptionItem | MemberInfo | V)[];
  onRenderDropDownListItem?: (oi: OptionItem) => React.ReactNode;
  columnCount?: number;
  columnWidth?: number;
}

export class EnumLineController<V extends string | number | boolean | null> extends ValueBaseController<EnumLineProps<V>, V>{

}

export interface OptionItem {
  value: any;
  label: string;
}

export const EnumLine: <V extends string | number | boolean | null>(props: EnumLineProps<V> & React.RefAttributes<EnumLineController<V>>) => React.ReactNode | null =
  genericForwardRefWithMemo(function EnumLine<V extends string | number | boolean | null>(props: EnumLineProps<V>, ref: React.Ref<EnumLineController<V>>) {

  const c = useController(EnumLineController<V>, props, ref);

  if (c.isHidden)
    return null;

  return props.lineType == 'ComboBoxText' ? internalComboBoxText(c) : props.lineType == 'RadioGroup' ? internalRadioGroup(c) : internalDropDownList(c);
}, (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

function internalDropDownList<V extends string | number | boolean | null>(vl: EnumLineController<V>) {

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

    function renderElement({item} : any){
      var result = vl.props.onRenderDropDownListItem!(item) as React.ReactElement;
      return React.cloneElement(result, {'data-value': item.value});
    }

    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(
          <DropdownList<OptionItem> className={classes(vl.props.valueHtmlAttributes?.className, s.ctx.formControlClass, vl.mandatoryClass, "p-0")} data={optionItems}
            id={inputId}
            onChange={(oe, md) => vl.setValue(oe.value, md.originalEvent)}
            value={oi}
            filter={false}
            autoComplete="off"
            dataKey="value"
            textField="label"
            renderValue={renderElement}
            renderListItem={renderElement}
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
          <select id={inputId} {...vl.props.valueHtmlAttributes} value={toStr(s.ctx.value)} className={classes(vl.props.valueHtmlAttributes?.className, s.ctx.formSelectClass, vl.mandatoryClass)} onChange={handleEnumOnChange} >
            {!optionItems.some(a => toStr(a.value) == toStr(s.ctx.value)) && <option key={-1} value={toStr(s.ctx.value)}>{toStr(s.ctx.value)}</option>}
            {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
          </select>)
        }
      </FormGroup>
    );
  }
}

function internalComboBoxText<V extends string | number | boolean | null>(el: EnumLineController<V>) {

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
        <Combobox<OptionItem> id={inputId} className={classes(el.props.valueHtmlAttributes?.className, s.ctx.formControlClass, el.mandatoryClass)} data={optionItems} onChange={(e: string | OptionItem, md) => {
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

function internalRadioGroup<V extends string | number | boolean | null>(elc: EnumLineController<V>) {

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


function getOptionsItems(el: EnumLineController<any>): OptionItem[] {

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
