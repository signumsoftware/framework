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
import { EntityBase, EntityBaseProps } from './EntityBase'


export interface EntityComboProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity | Lite<IEntity>>;

    data?: Lite<Entity>[];
}

export class EntityCombo extends EntityBase<EntityComboProps, EntityComboProps> {


    calculateDefaultState(state: EntityComboProps) {
        state.remove = false;
        state.create = false;
        state.view = false;
        state.find = false;

        if (!state.data) {
            if (this.state && this.state.type.name == state.type.name)
                state.data = this.state.data;
        }
    }

    componentDidMount() {
        if (!this.state.data) {
            Finder.API.findAllLites({ types: this.state.type.name })
                .then(data => this.setState({ data: data } as any));
        }
    }

    handleOnChange = (event: React.FormEvent) => {
        const current = event.currentTarget as HTMLSelectElement;

        if (current.value != liteKey(this.getLite())) {
            if (!current.value) {
                this.setValue(null);
            } else {
                const lite = this.state.data.filter(a => liteKey(a) == current.value).single();

                this.convert(lite).then(v => this.setValue(v));
            }
        }
    }

    getLite() {
        const v = this.state.ctx.value;
        if (v == null)
            return null;

        if ((v as ModifiableEntity).Type)
            return toLite(v as ModifiableEntity);

        return v as Lite<Entity>;
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        const lite = this.getLite();

        const elements: Lite<Entity>[] = [null].concat(s.data);
        if (lite && !elements.some(a => is(a, lite)))
            elements.insertAt(1, lite);

        var buttons = (
            <span className="input-group-btn">
                {!hasValue && this.renderCreateButton(true) }
                {!hasValue && this.renderFindButton(true) }
                {hasValue && this.renderViewButton(true) }
                {hasValue && this.renderRemoveButton(true) }
            </span>
        );

        if (!buttons.props.children.some(a => a))
            buttons = null;

        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                <div className="SF-entity-combo">
                    <div className={buttons ? "input-group" : null}>
                        <select className="form-control" onChange={this.handleOnChange} value={liteKey(lite) || "" }>
                            {elements.map((e, i) => <option key={i} value={e ? liteKey(e) : ""}>{e ? e.toStr : " - "}</option>) }
                        </select>
                        {buttons}
                    </div>
                </div>
            </FormGroup>
        );
    }
}

