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
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString  } from '../Signum.Entities'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'

export interface EntityCheckboxListProps extends EntityListBaseProps {
    data?: Lite<Entity>[];
    columnCount?: number;
    columnWidth?: number;
    avoidFieldSet?: boolean;
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
    }

    renderInternal() {
        const s = this.state;

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("SF-checkbox-list", s.ctx.errorClass)} {...{ ...this.baseHtmlAttributes(), ...s.formGroupHtmlAttributes } }>
                    {this.renderButtons()}
                    {this.renderCheckboxList()}
                </div>
            );

        return (
            <fieldset className={classes("SF-checkbox-list", s.ctx.errorClass)} {...{ ...this.baseHtmlAttributes(), ...s.formGroupHtmlAttributes } }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {this.renderButtons()}
                    </div>
                </legend>
                {this.renderCheckboxList()}
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

    renderCheckboxList() {
        const s = this.state;
        return (
            <EntityCheckboxListSelect
                ctx={s.ctx}
                onChange={this.handleOnChange}
                type={s.type!}
                data={s.data}
                findOptions={s.findOptions}
                columnCount={s.columnCount}
                columnWidth={s.columnWidth} />
        );
    }

    handleOnChange = (lite: Lite<Entity>) => {

        const list = this.props.ctx.value!;
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
  
}

interface EntityCheckboxListSelectProps {
    ctx: TypeContext<MList<Lite<Entity> | ModifiableEntity >>;
    onChange: (lite: Lite<Entity>) => void;
    type: TypeReference;
    findOptions?: FindOptions;
    data?: Lite<Entity>[];
    columnCount?: number;
    columnWidth?: number;
}

interface EntityCheckboxListSelectState {
    data?: Lite<Entity>[]
}

export default class EntityCheckboxListSelect extends React.Component<EntityCheckboxListSelectProps, EntityCheckboxListSelectState> {

    constructor(props: EntityCheckboxListSelectProps) {
        super(props);
        this.state = { data : props.data };
    }

    componentWillMount() {
        if (this.props.data == null)
            this.reloadData(this.props);
    }

    componentWillReceiveProps(newProps: EntityCheckboxListSelectProps, newContext: any) {
        if (newProps.data) {
            if (this.props.data == null)
                console.warn(`The 'data' was set too late. Consider using [] as default value to avoid automatic query. EntityCombo: ${this.props.type!.name}`);

            this.setState({ data: newProps.data });
        } else {
            if (EntityCheckboxListSelect.getFindOptions(newProps.findOptions) != EntityCheckboxListSelect.getFindOptions(this.props.findOptions) ||
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
            <div className="sf-checkbox-elements" style={this.getColumnStyle()}>
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

    renderContent() {
        if (this.state.data == undefined)
            return undefined;


        const data = [...this.state.data];


        const list = this.props.ctx.value!;

        list.forEach(mle => {
            if (!data.some(d => is(d, mle.element as Entity | Lite<Entity>)))
                data.insertAt(0, this.maybeToLite(mle.element as Entity | Lite<Entity>))
        });

        return data.map((lite, i) =>
            <label className="sf-checkbox-element" key={i}>
                <input type="checkbox"
                    checked={list.some(mle => is(mle.element as Entity | Lite<Entity>, lite))}
                    disabled={this.props.ctx.readOnly}
                    name={liteKey(lite)}
                    onChange={e => this.props.onChange(lite)} />
                &nbsp;
                <span className="sf-entitStrip-link">{lite.toStr}</span>
            </label>);

    }

    reloadData(props: EntityCheckboxListSelectProps) {
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



