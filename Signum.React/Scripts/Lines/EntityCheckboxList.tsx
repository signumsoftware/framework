import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString  } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'

export interface EntityCheckboxListProps extends EntityListBaseProps {
    data?: Lite<Entity>[];
    columnCount?: number;
    columnWidth?: number;
}

export class EntityCheckboxList extends EntityListBase<EntityCheckboxListProps, EntityCheckboxListProps> {

    calculateDefaultState(state: EntityCheckboxListProps) {
        super.calculateDefaultState(state);

        if (state.ctx.value == null)
            state.ctx.value = [];

        state.remove = false;
        state.create = false;
        state.view = false;
        state.find = false;
        state.columnWidth = 200;

        if (!state.data) {
            if (this.state && this.state.type!.name == state.type!.name)
                state.data = this.state.data;
        }
    }

    componentWillMount() {
        if (!this.state.data) {
            Finder.API.fetchAllLites({ types: this.state.type!.name })
                .then(data => this.setState({ data: data.orderBy(a => a.toStr) } as any))
                .done();
        }
    }

    componentWillReceiveProps(newProps: EntityCheckboxListProps, newContext: any) {
        if (!!newProps.data && !this.props.data)
            console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityCheckboxList: ${this.state.type!.name}`);

        super.componentWillReceiveProps(newProps, newContext);
    }

    handleOnChange = (event: React.FormEvent, lite: Lite<Entity>) => {
        const current = event.currentTarget as HTMLSelectElement;

        const list = this.state.ctx.value!;
        const toRemove = list.filter(mle => is(mle.element as Lite<Entity> | Entity, lite))

        if (toRemove.length) {
            toRemove.forEach(mle => list.remove(mle));
            this.setValue(list);
        }
        else {
            this.convert(lite).then(e => {
                this.addElement(e);
            }).done();
        }
    }

    getColumnStyle(): React.CSSProperties | undefined {
        const s = this.state;

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

    maybeToLite(entityOrLite: Entity | Lite<Entity>) {
        const entity = entityOrLite as Entity;

        if (entity.Type)
            return toLite(entity, entity.isNew);

        return entityOrLite as Lite<Entity>;
    }

    renderInternal() {
       
        return (
            <fieldset className={classes("SF-checkbox-list", this.state.ctx.errorClass)} {...{ ...this.baseHtmlProps(), ...this.state.formGroupHtmlProps } }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        <span className="pull-right">
                            {this.renderCreateButton(false) }
                            {this.renderFindButton(false) }
                        </span>
                    </div>
                </legend>
                <div className="sf-checkbox-elements" style={this.getColumnStyle() }>
                    { this.renderContent() }
                </div>
            </fieldset>
        );
    }


    renderContent() {
        if (this.state.data == undefined)
            return undefined;


        const data = [...this.state.data];


        const list = this.state.ctx.value!;

        list.forEach(mle => {
            if (!data.some(d => is(d, mle.element as Entity | Lite<Entity>)))
                data.insertAt(0, this.maybeToLite(mle.element as Entity | Lite<Entity>))
        });

        return data.map((lite, i) =>
            <label className="sf-checkbox-element" key={i}>
                <input type="checkbox"
                    checked={list.some(mle => is(mle.element as Entity | Lite<Entity>, lite))}
                    disabled={this.state.ctx.readOnly}
                    name={liteKey(lite) }
                    onChange={e => this.handleOnChange(e, lite) }  />
                &nbsp;
                <span className="sf-entitStrip-link">{lite.toStr}</span>
            </label>);

    }
}
