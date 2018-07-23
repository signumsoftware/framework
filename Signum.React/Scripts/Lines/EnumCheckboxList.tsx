import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, MemberType } from '../Reflection'
import { LineBase, LineBaseProps } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, newMListElement } from '../Signum.Entities'
import { Typeahead } from '../Components'

export interface EnumCheckboxListProps extends LineBaseProps {
    data?: string[];
    ctx: TypeContext<MList<string>>;
    columnCount?: number;
    columnWidth?: number;
    avoidFieldSet?: boolean;
}

export class EnumCheckboxList extends LineBase<EnumCheckboxListProps, EnumCheckboxListProps> {

    calculateDefaultState(state: EnumCheckboxListProps) {
        super.calculateDefaultState(state);
        state.columnWidth = 200;
        const ti = getTypeInfo(state.type!.name);
        state.data = Dic.getKeys(ti.members);
    }

    handleOnChange = (event: React.ChangeEvent<HTMLInputElement>, val: string) => {
        const current = event.currentTarget;

        var list = this.state.ctx.value;
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

    getColumnStyle(): React.CSSProperties | undefined {
        var s = this.state;

        if (s.columnCount && s.columnWidth)
            return {
                columns: `${s.columnCount} ${s.columnWidth}px`,
                MozColumns: `${s.columnCount} ${s.columnWidth}px`,
                WebkitColumns: `${s.columnCount} ${s.columnWidth}px`,
            };

        if (s.columnCount)
            return {
                columnCount: s.columnCount,
                MozColumnCount: s.columnCount,
                WebkitColumnCount: s.columnCount,
            };

        if (s.columnWidth)
            return {
                columnWidth: s.columnWidth,
                MozColumnWidth: s.columnWidth,
                WebkitColumnWidth: s.columnWidth,
            };

        return undefined;
    }
    

    renderInternal() {

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("SF-checkbox-list", this.state.ctx.errorClass)} {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                    {this.renderContent()}
                </div>
            );
       
        return (
            <fieldset className={classes("SF-checkbox-list", this.state.ctx.errorClass)} {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                    </div>
                </legend>
                {this.renderContent()}
            </fieldset>
        );
    }

    renderContent() {
        if (this.state.data == null)
            return null;

        var data = [...this.state.data];

        this.state.ctx.value.forEach(mle => {
            if (!data.some(d => d == mle.element))
                data.insertAt(0, mle.element)
        });

        const ti = getTypeInfo(this.state.type!.name);

        return (
            <div className="sf-checkbox-elements" style={this.getColumnStyle()}>
                {data.map((val, i) =>
                    <label className="sf-checkbox-element" key={i}>
                        <input type="checkbox"
                            checked={this.state.ctx.value.some(mle => mle.element == val)}
                            disabled={this.state.ctx.readOnly}
                            name={val}
                            onChange={e => this.handleOnChange(e, val)} />
                        &nbsp;
                        <span className="sf-entitStrip-link">{ti.members[val].niceName}</span>
                    </label>)}
            </div>
        );
    }
}
