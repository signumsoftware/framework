import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, MemberType, TypeReference } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, } from './LineBase'
import { FormGroup } from './FormGroup'
import { FormControlReadonly } from './FormControlReadonly'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { EntityBase, EntityBaseProps } from './EntityBase'

export interface EntityRadioButtonListProps extends EntityBaseProps {
    data?: Lite<Entity>[];
    columnCount?: number;
    columnWidth?: number;
    avoidFieldSet?: boolean;
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
    groupKey: string;
}

export class EntityRadioButtonList extends EntityBase<EntityRadioButtonListProps, EntityRadioButtonListProps> {

    calculateDefaultState(state: EntityRadioButtonListProps) {
        super.calculateDefaultState(state);

        state.remove = false;
        state.create = false;
        state.view = false;
        state.find = false;
        state.columnWidth = 200;
    }

    renderInternal() {
        const s = this.state;

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("SF-RadioButton-list", s.ctx.errorClass)} {...{ ...this.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
                    {this.renderButtons()}
                    {this.renderRadioButtonList()}
                </div>
            );

        return (
            <fieldset className={classes("SF-RadioButton-list", s.ctx.errorClass)} {...{ ...this.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {this.renderButtons()}
                    </div>
                </legend>
                {this.renderRadioButtonList()}
            </fieldset>
        );
    }

    renderButtons() {
        return (
            <span className="float-right">
                {this.renderCreateButton(false)}
                {this.renderFindButton(false)}
            </span>
        );
    }

    renderRadioButtonList() {
        const s = this.state;
        return (
            <EntityRadioButtonListSelect
                ctx={s.ctx}
                onChange={this.handleOnChange}
                type={s.type!}
                data={s.data}
                findOptions={s.findOptions}
                columnCount={s.columnCount}
                columnWidth={s.columnWidth}
                groupKey={s.groupKey} />
        );
    }

    handleOnChange = (lite: Lite<Entity> | null) => {

        if (lite == null)
            this.setValue(lite);
        else
            this.convert(lite)
                .then(v => this.setValue(v))
                .done();
    }
}

interface EntityRadioButtonListSelectProps {
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
    onChange: (lite: Lite<Entity> | null) => void;
    type: TypeReference;
    findOptions?: FindOptions;
    data?: Lite<Entity>[];
    columnCount?: number;
    columnWidth?: number;
    groupKey: string;
}

interface EntityRadioButtonSelectState {
    data?: Lite<Entity>[]
}

export default class EntityRadioButtonListSelect extends React.Component<EntityRadioButtonListSelectProps, EntityRadioButtonSelectState> {

    constructor(props: EntityRadioButtonListSelectProps) {
        super(props);
        this.state = { data: props.data };
    }

    componentWillMount() {
        if (this.props.data == null)
            this.reloadData(this.props);
    }

    componentWillReceiveProps(newProps: EntityRadioButtonListSelectProps, newContext: any) {
        if (newProps.data) {
            if (this.props.data == null)
                console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityCombo: ${this.props.type!.name}`);

            this.setState({ data: newProps.data });
        } else {
            if (EntityRadioButtonListSelect.getFindOptions(newProps.findOptions) != EntityRadioButtonListSelect.getFindOptions(this.props.findOptions) ||
                newProps.type.name != this.props.type.name)
                this.reloadData(newProps);
        }
    }

    static getFindOptions(fo: FindOptions | undefined) {
        if (fo == undefined)
            return undefined;

        return Finder.findOptionsPath(fo);
    }

    render() {
        return (
            <div className="sf-radiobutton-elements switch-field" style={this.getColumnStyle()}>
                {this.renderContent()}
            </div>
        );
    }


    getColumnStyle(): React.CSSProperties | undefined {
        const p = this.props;

        if (p.columnCount && p.columnWidth)
            return {
                columns: `${p.columnCount} ${p.columnWidth}px`,
                MozColumns: `${p.columnCount} ${p.columnWidth}px`,
                WebkitColumns: `${p.columnCount} ${p.columnWidth}px`,
            };

        if (p.columnCount)
            return {
                columnCount: p.columnCount,
                MozColumnCount: p.columnCount,
                WebkitColumnCount: p.columnCount,
            };

        if (p.columnWidth)
            return {
                columnWidth: p.columnWidth,
                MozColumnWidth: p.columnWidth,
                WebkitColumnWidth: p.columnWidth,
            };

        return undefined;
    }


    maybeToLite(entityOrLite: Entity | Lite<Entity>) {
        const entity = entityOrLite as Entity;

        if (entity.Type)
            return toLite(entity, entity.isNew);

        return entityOrLite as Lite<Entity>;
    }

    getLite() {
        const v = this.props.ctx.value;
        if (v == undefined)
            return undefined;

        if ((v as Entity).Type)
            return toLite(v as Entity);

        return v as Lite<Entity>;
    }

    getLiteKey() {
        const lite = this.getLite();

        return lite ? liteKey(lite) : undefined;
    }

    renderContent() {
        if (this.state.data == undefined)
            return undefined;

        const data = [...this.state.data];

        const lite = this.getLite();

        const elements = [undefined, ...this.state.data];
        if (lite && !elements.some(a => is(a, lite)))
            elements.insertAt(1, lite);

        return data.map((litem, i) =>
            <label className="sf-radiobutton-element" key={this.props.groupKey + i}>
                <input type="radio"
                    radioGroup={this.props.groupKey}
                    checked={is(lite, litem)}
                    disabled={this.props.ctx.readOnly}
                    name={liteKey(litem) + this.props.groupKey}
                    onChange={e => this.props.onChange(litem)} />
                &nbsp;
                <span className="sf-entitStrip-link">{litem.toStr}</span>
            </label>);

    }

    reloadData(props: EntityRadioButtonListSelectProps) {
        const fo = props.findOptions;
        if (fo) {
            Finder.expandParentColumn(fo);
            var limit = fo && fo.pagination && fo.pagination.elementsPerPage || 999;
            Finder.fetchEntitiesWithFilters(fo.queryName, fo.filterOptions || [], fo.orderOptions || [], limit)
                .then(data => this.setState({ data: fo.orderOptions && fo.orderOptions.length ? data : data.orderBy(a => a.toStr) } as any))
                .done();
        }
        else
            Finder.API.fetchAllLites({ types: this.props.type!.name })
                .then(data => this.setState({ data: data.orderBy(a => a.toStr) } as any))
                .done();

    }


}



