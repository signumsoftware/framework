import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
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

        state.remove = false;
        state.create = false;
        state.view = false;
        state.find = false;
        state.columnWidth = 200;

        if (!state.data) {
            if (this.state && this.state.type.name == state.type.name)
                state.data = this.state.data;
        }
    }

    componentWillMount() {
        if (!this.state.data) {
            Finder.API.findAllLites({ types: this.state.type.name })
                .then(data => this.setState({ data: data.orderBy(a => a.toStr) } as any))
                .done();
        }
    }

    handleOnChange = (event: React.FormEvent, lite: Lite<Entity>) => {
        const current = event.currentTarget as HTMLSelectElement;

        var list = this.state.ctx.value;
        var toRemove = list.filter(mle => is(mle.element as Lite<Entity> | Entity, lite))

        if (toRemove.length) {
            toRemove.forEach(mle => list.remove(mle));
            this.forceUpdate();
        }
        else {
            this.convert(lite).then(e => {
                list.push({
                    rowId: null,
                    element: e
                });
                this.forceUpdate();
            }).done();
        }
    }

    getColumnStyle(): React.CSSProperties {
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

        return null;
    }

    maybeToLite(entityOrLite: Entity | Lite<Entity>) {
        var entity = entityOrLite as Entity;

        if (entity.Type)
            return toLite(entity, entity.isNew);

        return entityOrLite as Lite<Entity>;
    }

    renderInternal() {
       
        return (
            <fieldset className={classes("SF-checkbox-list", this.state.ctx.binding.errorClass) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        <span className="pull-right">
                            {this.renderCreateButton(false) }
                            {this.renderFindButton(false) }
                        </span>
                    </div>
                </legend>
                <div style={this.getColumnStyle() }>
                    { this.renderContent() }
                </div>
            </fieldset>
        );
    }


    renderContent() {
        if (this.state.data == null)
            return null;

        var data = [...this.state.data];

        this.state.ctx.value.forEach(mle => {
            if (!data.some(d => is(d, mle.element as Entity | Lite<Entity>)))
                data.insertAt(0, this.maybeToLite(mle.element as Entity | Lite<Entity>))
        });

        return data.map((lite, i) =>
            <label className="sf-checkbox-element" key={i}>
                <input type="checkbox"
                    checked={this.state.ctx.value.some(mle => is(mle.element as Entity | Lite<Entity>, lite)) }
                    disabled={this.state.ctx.readOnly}
                    name={liteKey(lite) }
                    onChange={e => this.handleOnChange(e, lite) }  />
                &nbsp;
                <span className="sf-entitStrip-link">{lite.toStr}</span>
            </label>);

    }
}
