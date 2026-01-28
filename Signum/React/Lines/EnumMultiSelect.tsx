import * as React from 'react'
import { classes, Dic } from '../Globals'
import { mlistItemContext, TypeContext } from '../TypeContext'
import { getTypeInfo } from '../Reflection'
import { genericMemo, LineBaseController, LineBaseProps, useController } from '../Lines/LineBase'
import { isMListElement, MList, MListElement, newMListElement } from '../Signum.Entities'
import { getTimeMachineCheckboxIcon, getTimeMachineIcon } from './TimeMachineIcon'
import { Multiselect } from 'react-widgets-up';
import { HeaderType } from './GroupHeader'
import { FormGroup } from './FormGroup'

export interface EnumMultiSelectProps<V extends string> extends LineBaseProps<MList<V>> {
  onRenderItem?: (item: V) => React.ReactNode;
  data?: V[];
  avoidFieldSet?: boolean | HeaderType;
  ref?: React.Ref<EnumMultiSelectController<V>>
}

export class EnumMultiSelectController<V extends string> extends LineBaseController<EnumMultiSelectProps<V>, MList<V>> {

  override getDefaultProps(p: EnumMultiSelectProps<V>): void {
    super.getDefaultProps(p);
    if (p.type) {
      const ti = getTypeInfo(p.type.name);
      p.data = Dic.getKeys(ti.members) as V[];
    }
  }

  handleOnSelect = (values: V[], e?: React.SyntheticEvent) => {
    var current = this.props.ctx.value;

    values.filter(val => !current.some(mle => mle.element == val)).forEach(val => {
      current.push(newMListElement(val));
    });

    current.filter(mle => !values.some(lite => lite == mle.element)).forEach(mle => {
      current.remove(mle);
    });

    this.setValue(current, e);

    this.forceUpdate();

    return "";
  }
}

export const EnumMultiSelect: <V extends string>(props: EnumMultiSelectProps<V>) => React.ReactNode | null
  = genericMemo(function EnumMultiSelect<V extends string>(props: EnumMultiSelectProps<V>) {
    const c = useController<EnumMultiSelectController<V>, EnumMultiSelectProps<V>, MList<V>>(EnumMultiSelectController, props);
    const p = c.props;

    if (c.isHidden)
      return null;

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);
    const ti = getTypeInfo(p.type!.name);

    const renderItem = p.onRenderItem || ((item: V) => ti.members[item].niceName ?? item);

    //TODO add TimeMachineIcon
    return (
      <FormGroup ctx={p.ctx!} error={p.error} label={p.label} labelIcon={p.labelIcon}
        labelHtmlAttributes={p.labelHtmlAttributes}
        helpText={helpText}
        helpTextOnTop={helpTextOnTop}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}>
        {inputId => <div className={classes(p.ctx.rwWidgetClass, c.mandatoryClass ? c.mandatoryClass + "-widget" : undefined)}>
          <Multiselect<unknown>
            id={inputId}
            readOnly={p.ctx.readOnly}
            dataKey={e => isMListElement(e) ? e.element as V : e as V}
            textField={a => ti.members[a as string].niceName ?? a}
            value={p.ctx.value}
            data={p.data}
            onChange={(value, meta) => c.handleOnSelect(value.map(e => isMListElement(e) ? e.element as V : e as V, meta.originalEvent))}
            renderListItem={({ item }) => renderItem(item as V)}
            renderTagValue={({ item }) => isMListElement(item) ? renderItem(item.element as V) : renderItem(item as V)}
          />
        </div>}
      </FormGroup>
    );
  });
