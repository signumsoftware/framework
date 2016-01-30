import * as React from 'react'
import { Link } from 'react-router'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import { EntityBase, EntityBaseProps} from './EntityBase'


export interface EntityComboProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity | Lite<IEntity>>;

    data?: Lite<Entity>[];
}

export class EntityCombo extends EntityBase<EntityComboProps> {


    calculateDefaultState(state: EntityComboProps) {
        state.remove = false;
        state.create = false;
        state.view = false;
        state.find = false;

        if (!state.data) {
            if (this.state && this.state.type.name == state.type.name)
                state.data = this.state.data;

            if (!state.data) {
                Finder.API.findAllLites({ types: state.type.name })
                    .then(data => this.setState({ data: data } as any));
            }
        }
    }

    handleOnChange = (event: React.FormEvent) => {
        var current = event.currentTarget as HTMLSelectElement;

        if (current.value != liteKey(this.getLite())) {
            if (!current.value) {
                this.setValue(null);
            } else {
                var lite = this.state.data.filter(a => liteKey(a) == current.value).single();

                this.convert(lite).then(v => this.setValue(v));
            }
        }
    }

    getLite() {
        var v = this.state.ctx.value;
        if (v == null)
            return null;

        if ((v as ModifiableEntity).Type)
            return toLite(v as ModifiableEntity);

        return v as Lite<Entity>;
    }

    renderInternal() {

        var s = this.state;

        var hasValue = !!s.ctx.value;

        var lite = this.getLite();

        var elements: Lite<Entity>[] = [null].concat(s.data);
        if (lite && !elements.some(a => is(a, lite)))
            elements.insertAt(1, lite);

        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                <div className="SF-entity-combo">
                    <div className="input-group">
                        <select className="form-control" onChange={this.handleOnChange} value={liteKey(lite) || "" }>
                            {elements.map((e, i) => <option key={i} value={e ? liteKey(e) : ""}>{e ? e.toStr : " - "}</option>) }
                        </select>
                        <span className="input-group-btn">
                            {!hasValue && this.renderCreateButton(true) }
                            {!hasValue && this.renderFindButton(true) }
                            {hasValue && this.renderViewButton(true) }
                            {hasValue && this.renderRemoveButton(true) }
                        </span>
                    </div>
                </div>
            </FormGroup>
        );
    }
}

