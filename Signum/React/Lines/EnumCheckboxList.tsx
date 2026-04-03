import * as React from 'react'
import { classes, Dic } from '../Globals'
import { mlistItemContext, TypeContext } from '../TypeContext'
import { getTypeInfo } from '../Reflection'
import { genericMemo, LineBaseController, LineBaseProps, useController } from '../Lines/LineBase'
import { MList, newMListElement } from '../Signum.Entities'
import { getTimeMachineCheckboxIcon, getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'
import { JSX } from 'react'

export interface EnumCheckboxListProps<V extends string> extends LineBaseProps<MList<V>> {
  data?: V[];
  columnCount?: number;
  columnWidth?: number;
  avoidFieldSet?: boolean | HeaderType;
  ref?: React.Ref<EnumCheckboxListController<V>>
}

export class EnumCheckboxListController<V extends string> extends LineBaseController<EnumCheckboxListProps<V>, MList<V>> {

  override getDefaultProps(p: EnumCheckboxListProps<V>): void {
    super.getDefaultProps(p);
    p.columnWidth = 200;
    if (p.type) {
      const ti = getTypeInfo(p.type.name);
      p.data = Dic.getKeys(ti.members) as V[];
    }
  }

  handleOnChange = (event: React.ChangeEvent<HTMLInputElement>, val: V): void => {

    var list = this.props.ctx.value;
    var toRemove = list.filter(mle => mle.element == val)

    if (toRemove.length) {
      toRemove.forEach(mle => list.remove(mle));
      this.setValue(list);
    }
    else {
      list.push(newMListElement(val));
      this.setValue(list);
    }
  }

}

export const EnumCheckboxList: <V extends string>(props: EnumCheckboxListProps<V>) => React.ReactNode | null =
  genericMemo(function EnumCheckboxList<V extends string>(props: EnumCheckboxListProps<V>) {
    const c = useController<EnumCheckboxListController<V>, EnumCheckboxListProps<V>, MList<V>>(EnumCheckboxListController, props);
    const p = c.props;

    if (c.isHidden)
      return null;

    return (
      <GroupHeader className={classes("sf-checkbox-list", c.getErrorClass("border"))}
        label={p.label}
        labelIcon={p.labelIcon}
        avoidFieldSet={p.avoidFieldSet}
        buttons={undefined}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
        {renderContent()}
      </GroupHeader >
    );

    function renderContent() {
      if (p.data == null)
        return null;

      var data = [...p.data];

      p.ctx.value.forEach(mle => {
        if (!data.some(d => d == mle.element))
          data.insertAt(0, mle.element)
      });

      var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
      const requiredIndicator = p.ctx.propertyRoute?.member?.required && !ariaAtts['aria-readonly'];

      const ti = getTypeInfo(p.type!.name);

      var listCtx = mlistItemContext(p.ctx);

      return (
        <div className="sf-checkbox-elements" style={getColumnStyle()}>
          {data.map((val, i) => {
            var controlId = React.useId();
            var ectx = listCtx.firstOrNull(ec => ec.value == val);
            var oldCtx = p.ctx.previousVersion == null || p.ctx.previousVersion.value == null ? null :
              listCtx.firstOrNull(el => el.previousVersion?.value == val);

            return (
              <label className="sf-checkbox-element" key={val} htmlFor={controlId}>
                {getTimeMachineCheckboxIcon({ newCtx: ectx, oldCtx: oldCtx, type: ti })}
                <input type="checkbox"
                  id={controlId}
                  className="form-check-input"
                  checked={p.ctx.value.some(mle => mle.element == val)}
                  disabled={p.ctx.readOnly}
                  name={val}
                  onChange={e => c.handleOnChange(e, val)} />
                &nbsp;
                <span>{ti.members[val].niceName}{requiredIndicator && <span aria-hidden="true" className="required-indicator">*</span>}</span>
            </label>);
          })}
        </div>
      );
    }

    function getColumnStyle(): React.CSSProperties | undefined {

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
  });
