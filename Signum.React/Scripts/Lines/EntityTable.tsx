import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType, Type } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import DynamicComponent from './DynamicComponent'
import { RenderEntity } from './RenderEntity'

export interface EntityTableProps extends EntityListBaseProps {
    createAsLink?: boolean;
    columns: EntityTableColumn<ModifiableEntity>[],
}

export interface EntityTableColumn<T> {
    property: (a: T) => any;
    header?: React.ReactNode | null;
    headerProps?: React.HTMLProps<any>;
    cellProps?: React.HTMLProps<any>;
    template?: (ctx: TypeContext<T>, row: EntityTableRow) => React.ReactNode | null;

}

export class EntityTable extends EntityListBase<EntityTableProps, EntityTableProps> {

    static typedColumns<T extends ModifiableEntity>(type: Type<T>, columns: (EntityTableColumn<T> | null | undefined)[]): EntityTableColumn<T>[]{
        return columns.filter(a => a != null).map(a => a!);
    }

    calculateDefaultState(state: EntityTableProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.createAsLink = true;
    }


    renderInternal() {

        const buttons = (
            <span className="pull-right">
                {!this.state.createAsLink && this.renderCreateButton(false)}
                {this.renderFindButton(false)}
            </span>
        );

        if (this.state.type!.isLite)
            throw new Error("Lite not supported");

        let ctx = (this.state.ctx as TypeContext<MList<ModifiableEntity>>).subCtx({ formGroupStyle: "SrOnly" });

        const readOnly = this.state.ctx.readOnly;

        const elementPr = ctx.propertyRoute.add(a => a[0].element);

        return (
            <fieldset className={classes("SF-table-field SF-control-container", ctx.errorClass)} {...Dic.extend(this.baseHtmlProps(), this.state.formGroupHtmlProps) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {React.Children.count(buttons) ? buttons : undefined}
                    </div>
                </legend>
                <table className="table table-condensed form-vertical">
                    <thead>
                        <tr>
                            <th style={{ width: "20px" }}></th>
                            {
                                this.props.columns.map((c, i) => <th key={i} {...c.headerProps}>
                                    {c.header === undefined && c.property ? elementPr.add(c.property).member!.niceName : c.header}
                                </th>)
                            }
                        </tr>
                    </thead>
                    <tbody>
                        {
                            mlistItemContext(ctx).map((mlec, i) =>
                                (<EntityTableRow key={i}
                                    onRemove={this.state.remove && !readOnly ? e => this.handleRemoveElementClick(e, i) : undefined}
                                    onMoveDown ={this.state.move && !readOnly ? e => this.moveDown(i) : undefined}
                                    onMoveUp ={this.state.move && !readOnly ? e => this.moveUp(i) : undefined}
                                    columns={this.props.columns}
                                    ctx={mlec} />))
                        }
                        {
                            this.state.createAsLink && this.state.create && !readOnly &&
                            <tr>
                                <td colSpan={1 + this.props.columns.length}>
                                    <a title={EntityControlMessage.Create.niceToString()}
                                        className="sf-line-button sf-create"
                                        onClick={this.handleCreateClick}>
                                        <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }}/>{EntityControlMessage.Create.niceToString()}
                                    </a>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </fieldset>
        );
    }
}


export interface EntityTableRowProps {
    ctx: TypeContext<ModifiableEntity>;
    columns: EntityTableColumn<ModifiableEntity>[],
    onRemove?: (event: React.MouseEvent) => void;
    onMoveUp?: (event: React.MouseEvent) => void;
    onMoveDown?: (event: React.MouseEvent) => void;
}

export class EntityTableRow extends React.Component<EntityTableRowProps, { entity: ModifiableEntity }> {

    render() {
        var ctx = this.props.ctx;
        return (
            <tr>
                <td>
                    <div className="item-group">
                        {this.props.onRemove && <a className={classes("sf-line-button", "sf-remove")}
                            onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString()}>
                            <span className="glyphicon glyphicon-remove"/>
                        </a>}

                        {this.props.onMoveUp && <a className={classes("sf-line-button", "move-up")}
                            onClick={this.props.onMoveUp}
                            title={EntityControlMessage.MoveUp.niceToString()}>
                            <span className="glyphicon glyphicon-chevron-up"/>
                        </a>}

                        {this.props.onMoveDown && <a className={classes("sf-line-button", "move-down")}
                            onClick={this.props.onMoveDown}
                            title={EntityControlMessage.MoveDown.niceToString()}>
                            <span className="glyphicon glyphicon-chevron-down"/>
                        </a>}
                    </div>
                </td>
                {this.props.columns.map((c, i) => <td key={i} {...c.cellProps}>{this.getTemplate(c)}</td>)}
            </tr>
        );
    }


    getTemplate(col: EntityTableColumn<ModifiableEntity>) {

        if (col.template === null)
            return null;

        if (col.template !== undefined)
            return col.template(this.props.ctx, this);

        if (col.property == null)
            throw new Error("Column " + JSON.stringify(col) + " has no property and no tempalte");

        return DynamicComponent.appropiateComponent(this.props.ctx.subCtx(col.property));
    }
}