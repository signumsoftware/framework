import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType, Type } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { EntityBase } from './EntityBase'
import { EntityListBase, EntityListBaseProps, DragConfig } from './EntityListBase'
import DynamicComponent from './DynamicComponent'
import { RenderEntity } from './RenderEntity'

export interface EntityTableProps extends EntityListBaseProps {
    createAsLink?: boolean | ((er: EntityTable) => React.ReactElement<any>);
    /**Consider using EntityTable.typedColumns to get Autocompletion**/
    columns?: EntityTableColumn<any /*T*/, any>[],
    fetchRowState?: (ctx: TypeContext<any /*T*/>, row: EntityTableRow) => Promise<any>;
    onRowHtmlAttributes?: (ctx: TypeContext<any /*T*/>, row: EntityTableRow, rowState: any) => React.HTMLAttributes<any> | null | undefined;
    avoidFieldSet?: boolean;
    avoidEmptyTable?: boolean;
}

export interface EntityTableColumn<T, RS> {
    property?: ((a: T) => any) | string;
    header?: React.ReactNode | null;
    headerHtmlAttributes?: React.ThHTMLAttributes<any>;
    cellHtmlAttributes?: (ctx: TypeContext<T>, row: EntityTableRow, rowState: RS) => React.TdHTMLAttributes<any> | null | undefined;
    template?: (ctx: TypeContext<T>, row: EntityTableRow, rowState: RS) => React.ReactChild | null | undefined | false;
}

export class EntityTable extends EntityListBase<EntityTableProps, EntityTableProps> {

    static typedColumns<T extends ModifiableEntity>(columns: (EntityTableColumn<T, any> | null | undefined)[]): EntityTableColumn<ModifiableEntity, any>[] {
        return columns.filter(a => a != null).map(a => a!) as EntityTableColumn<ModifiableEntity, any>[];
    }

    static typedColumnsWithRowState<T extends ModifiableEntity, RS>(columns: (EntityTableColumn<T, RS> | null | undefined)[]): EntityTableColumn<ModifiableEntity, RS>[]{
        return columns.filter(a => a != null).map(a => a!) as EntityTableColumn<ModifiableEntity, RS>[];
    }

    calculateDefaultState(state: EntityTableProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.createAsLink = true;
    }

    overrideProps(state: EntityTableProps, overridenProps: EntityTableProps) {
        super.overrideProps(state, overridenProps);

        if (!state.columns) {
            var elementPr = state.ctx.propertyRoute.add(a => a[0].element);

            state.columns = Dic.getKeys(elementPr.subMembers())
                .filter(a => a != "Id")
                .map(memberName => ({
                    property: eval("(function(e){ return e." + memberName.firstLower() + "; })")
                }) as EntityTableColumn<ModifiableEntity, any>);
        }
    }


    renderInternal() {

        if (this.state.type!.isLite)
            throw new Error("Lite not supported");

        let ctx = (this.state.ctx as TypeContext<MList<ModifiableEntity>>).subCtx({ formGroupStyle: "SrOnly" });

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("SF-table-field SF-control-container", ctx.errorClass)} {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                    {this.renderButtons()}
                    {this.renderTable(ctx)}
                </div>
            );

        return (
            <fieldset className={classes("SF-table-field SF-control-container", ctx.errorClass)} {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {this.renderButtons()}
                    </div>
                </legend>
                {this.renderTable(ctx)}
            </fieldset>
        );
    }

    renderButtons() {
        const buttons = (
            <span className="pull-right">
                {this.state.createAsLink == false && this.renderCreateButton(false)}
                {this.renderFindButton(false)}
            </span>
        );

        return (EntityBase.hasChildrens(buttons) ? buttons : undefined);
    }

    renderTable(ctx: TypeContext<MList<ModifiableEntity>>) {

        const readOnly = ctx.readOnly;
        const elementPr = ctx.propertyRoute.add(a => a[0].element);

        return (
            <table className="table table-sm sf-table">
                {
                    (!this.props.avoidEmptyTable || ctx.value.length > 0) && <thead>
                        <tr className="bg-light">
                            <th></th>
                            {
                                this.state.columns!.map((c, i) => <th key={i} {...c.headerHtmlAttributes}>
                                    {c.header === undefined && c.property ? elementPr.add(c.property).member!.niceName : c.header}
                                </th>)
                            }
                        </tr>
                    </thead>
                }
                <tbody>
                    {
                        mlistItemContext(ctx).map((mlec, i) =>
                            (<EntityTableRow key={i}
                                index={i}
                                onRowHtmlAttributes={this.props.onRowHtmlAttributes}
                                fetchRowState={this.props.fetchRowState}
                                onRemove={this.canRemove(mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, i) : undefined}
                                draggable={this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, "v") : undefined}
                                columns={this.state.columns!}
                                ctx={mlec} />))
                    }
                    {
                        this.state.createAsLink && this.state.create && !readOnly &&
                        <tr>
                            <td colSpan={1 + this.state.columns!.length}>
                                {typeof this.state.createAsLink == "function" ? this.state.createAsLink(this) :
                                    <a href="#" title={EntityControlMessage.Create.niceToString()}
                                        className="sf-line-button sf-create"
                                        onClick={this.handleCreateClick}>
                                        <span className="fa fa-plus sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
                                    </a>}
                            </td>
                        </tr>
                    }
                </tbody>
            </table>);
    }
}


export interface EntityTableRowProps {
    ctx: TypeContext<ModifiableEntity>;
    index: number;
    columns: EntityTableColumn<ModifiableEntity, any>[],
    onRemove?: (event: React.MouseEvent<any>) => void;
    draggable?: DragConfig;
    fetchRowState?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow) => Promise<any>;
    onRowHtmlAttributes?: (ctx: TypeContext<ModifiableEntity>, row: EntityTableRow, rowState: any) => React.HTMLAttributes<any> | null | undefined;
}

export class EntityTableRow extends React.Component<EntityTableRowProps, { rowState?: any }> {

    constructor(props: EntityTableRowProps) {
        super(props);

        this.state = {};

        if (props.fetchRowState)
            props.fetchRowState(this.props.ctx, this)
                .then(val => this.setState({ rowState: val }))
                .done();
    }

    render() {
        var ctx = this.props.ctx;
        var rowAtts = this.props.onRowHtmlAttributes && this.props.onRowHtmlAttributes(ctx, this, this.state.rowState);
        const drag = this.props.draggable;
        return (
            <tr style={{ backgroundColor: rowAtts && rowAtts.style && rowAtts.style.backgroundColor || undefined }}
                onDragEnter={drag && drag.onDragOver}
                onDragOver={drag && drag.onDragOver}
                onDrop={drag && drag.onDrop}
                className={drag && drag.dropClass}>
                <td>
                    <div className="item-group">
                        {this.props.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
                            onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString()}>
                            <span className="fa fa-remove"/>
                        </a>}
                        &nbsp;
                        {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
                            draggable={true}
                            onDragStart={drag.onDragStart}
                            onDragEnd={drag.onDragEnd}
                            title={EntityControlMessage.Move.niceToString()}>
                            <span className="fa fa-bars"/>
                        </a>}
                    </div>
                </td>
                {this.props.columns.map((c, i) => <td key={i} {...c.cellHtmlAttributes && c.cellHtmlAttributes(ctx, this, this.state.rowState) }>{this.getTemplate(c)}</td>)}
            </tr>
        );
    }


    getTemplate(col: EntityTableColumn<ModifiableEntity, any>): React.ReactChild | undefined | null | false {

        if (col.template === null)
            return null;

        if (col.template !== undefined)
            return col.template(this.props.ctx, this, this.state.rowState);

        if (col.property == null)
            throw new Error("Column has no property and no template");

        if (typeof col.property == "string")
            return DynamicComponent.getAppropiateComponent(this.props.ctx.subCtx(col.property));
        else
            return DynamicComponent.getAppropiateComponent(this.props.ctx.subCtx(col.property));
    }
}