import * as React from 'react'
import { Link } from 'react-router'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityListBase'
import { RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines/RenderEntity'

interface IGridEntity {
    row: number;
    startColumn: number;
    columns: number
}

export interface EntityGridRepeaterProps extends EntityListBaseProps {
    getComponent?: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
    createAsLink?: boolean;
    move?: boolean;
    resize?: boolean;
}


export interface EntityGridRepaterState extends EntityGridRepeaterProps {
    dragMode?: string;
    initialPageX?: number;
    originalStartColumn?: number;
    currentItem?: TypeContext<ModifiableEntity & IGridEntity>;
    currentRow?: number;
}

export class EntityGridRepeater extends EntityListBase<EntityGridRepeaterProps, EntityGridRepaterState> {

    calculateDefaultState(state: EntityGridRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.move = true;
        state.resize = true;
        state.remove = true;
    }

    renderInternal() {
        var s = this.state;
        return (
            <fieldset className={classes("SF-grid-repeater-field SF-control-container", this.state.ctx.binding.errorClass) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        <span className="pull-right">
                            {this.renderCreateButton(false) }
                            {this.renderFindButton(false) }
                        </span>
                    </div>
                </legend>
                <div className="row rule">
                    { Array.range(0, 12).map(i =>
                        <div className="col-sm-1" key={i}>
                            <div className="ruleItem"/>
                        </div>
                    ) }
                </div>
                <div className={s.dragMode == "move" ? "sf-dragging" : null} onDrop={this.handleOnDrop}>
                    {
                        mlistItemContext(this.state.ctx)
                            .map((mlec, i) => ({
                                ctx: mlec as TypeContext<ModifiableEntity & IGridEntity>,
                                index: i
                            }))
                            .groupBy(p => p.ctx.value.row.toString())
                            .orderBy(gr => gr.key)
                            .flatMap((gr, i, groups) => [
                                this.renderSeparator(parseInt(gr.key)),
                                <div className="row items-row" key={"row" + gr.key} onDragOver={e => this.handleItemsRowDragOver(e, parseInt(gr.key)) }>
                                    { gr.elements.orderBy(a => a.ctx.value.startColumn).map((p, j, list) => {
                                        let item = this.props.getComponent(p.ctx, null);
                                        const s = this.state;
                                        item = React.cloneElement(item, {
                                            onResizerDragStart: p.ctx.readOnly || !s.resize ? null : (resizer, e) => this.handleResizeDragStart(resizer, e, p.ctx),
                                            onTitleDragStart: p.ctx.readOnly || !s.move ? null : (e) => this.handleMoveDragStart(e, p.ctx),
                                            onRemove: p.ctx.readOnly || !s.remove ? null : (e) => this.handleRemoveElementClick(e, p.index),
                                        });

                                        const last = j == 0 ? null : list[j - 1].ctx.value;

                                        const offset = p.ctx.value.startColumn - (last ? (last.startColumn + last.columns) : 0);

                                        return (
                                            <div key={j} className={`sf-grid-element col-sm-${p.ctx.value.columns} col-sm-offset-${offset}`}>
                                                {item}
                                                {/*StartColumn: {p.ctx.value.startColumn} | Columns: {p.ctx.value.columns} | Row: {p.ctx.value.row}*/}
                                            </div>
                                        );
                                    }) }
                                </div>,
                                i == groups.length - 1 && this.renderSeparator(parseInt(gr.key) + 1)
                            ])

                    }
                </div>
            </fieldset>
        );
    }

    renderSeparator(rowIndex: number) {
        return (
            <div className={classes("row separator-row", this.state.currentRow == rowIndex ? "sf-over" : null) } key={"sep" + rowIndex}
                onDragOver = {e => this.handleRowDragOver(e, rowIndex) }
                onDragEnter = {e => this.handleRowDragOver(e, rowIndex) }
                onDragLeave = {() => this.handleRowDragLeave() }
                onDrop = {e => this.handleRowDrop(e, rowIndex) } />
        );
    }


    handleRowDragOver = (e: React.DragEvent, row: number) => {
        e.dataTransfer.dropEffect = "move";
        e.preventDefault();
        if (this.state.currentRow != row)
            this.setState({ currentRow: row });
    }

    handleRowDragLeave = () => {
        this.setState({ currentRow: null });
    }

    handleRowDrop = (e: React.DragEvent, row: number) => {

        const list = this.state.ctx.value.map(a => a.element as ModifiableEntity & IGridEntity);

        var c = this.state.currentItem.value;

        list.filter(a => a != c && a.row >= row).forEach(a => a.row++);
        c.row = row;
        c.startColumn = 0;
        c.columns = 12;

        this.setState({
            dragMode: null,
            initialPageX: null,
            originalStartColumn: null,
            currentItem: null,
            currentRow: null,
        });
    }


    handleCreateClick = (event: React.SyntheticEvent) => {

        event.preventDefault();

        const onCreate = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        onCreate
            .then((e: ModifiableEntity & IGridEntity) => {

                if (!e)
                    return;

                if (!e.Type)
                    throw new Error("Should be an entity");

                const list = this.props.ctx.value;
                if (e.row == null)
                    e.row = list.length == 0 ? 0 : list.map(a => (a.element as any as IGridEntity).row).max() + 1;
                if (e.startColumn == null)
                    e.startColumn = 0;
                if (e.columns == null)
                    e.columns = 12;

                list.push({ rowId: null, element: e });
                this.setValue(list);
            }).done();
    };

    handleOnDrop = (event: React.SyntheticEvent) => {
        this.setState({
            dragMode: null,
            initialPageX: null,
            originalStartColumn: null,
            currentItem: null,
            currentRow: null,
        });
    }



    handleResizeDragStart = (resizer: "left" | "right", e: React.DragEvent, mlec: TypeContext<ModifiableEntity & IGridEntity>) => {
        e.dataTransfer.effectAllowed = "move";
        const de = e.nativeEvent as DragEvent;
        this.setState({
            dragMode: resizer,
            initialPageX: null,
            originalStartColumn: null,
            currentItem: mlec,
            currentRow: null,
        });
        this.forceUpdate();
    }

    handleMoveDragStart = (e: React.DragEvent, mlec: TypeContext<ModifiableEntity & IGridEntity>) => {
        e.dataTransfer.effectAllowed = "move";
        const de = e.nativeEvent as DragEvent;
        this.setState({
            dragMode: "move",
            initialPageX: de.pageX,
            originalStartColumn: mlec.value.startColumn,
            currentItem: mlec,
            currentRow: null,
        });
        this.forceUpdate();
    }

    handleItemsRowDragOver = (e: React.DragEvent, row: number) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = "move";
        const de = e.nativeEvent as DragEvent;
        var s = this.state;
        const list = s.ctx.value.map(a => a.element as ModifiableEntity & IGridEntity);
        const c = s.currentItem.value;
        const rect = (e.currentTarget as HTMLDivElement).getBoundingClientRect();



        if (s.dragMode == "move") {
            const offset = de.pageX - s.initialPageX;
            const dCol = Math.round((offset / rect.width) * 12);
            var newCol = s.originalStartColumn + dCol;
            let start = list.filter(a => a != c && a.row == row && a.startColumn <= newCol).map(a => a.startColumn + a.columns).max();
            if (!isFinite(start))
                start = 0;

            let end = list.filter(a => a != c && a.row == row && a.startColumn > newCol).map(a => a.startColumn - c.columns).min();
            if (!isFinite(end))
                end = 12 - c.columns;

            if (start > end) {
                e.dataTransfer.dropEffect = "none";
                return; //Doesn't fit
            }

            newCol = Math.max(start, Math.min(newCol, end));

            if (newCol != c.startColumn || c.row != row) {
                c.startColumn = newCol;
                c.row = row;
                c.modified = true;
                this.forceUpdate();
            }
        } else {
            const offsetX = (de.pageX + (s.dragMode == "right" ? 15 : -15)) - rect.left;
            let col = Math.round((offsetX / rect.width) * 12);

            if (s.dragMode == "left") {
                const max = list.filter(a => a != c && a.row == c.row && a.startColumn < c.startColumn).map(a => a.startColumn + a.columns).max();
                col = Math.max(col, max);

                const cx = c.startColumn - col;
                if (cx != 0) {
                    c.startColumn = col;
                    c.columns += cx;
                    c.modified = true;

                    this.forceUpdate();
                }
            }
            else if (s.dragMode == "right") {
                const min = list.filter(a => a != c && a.row == c.row && a.startColumn > c.startColumn).map(a => a.startColumn).min();
                col = Math.min(col, min);
                if (col != c.startColumn + c.columns) {
                    c.columns = col - c.startColumn;
                    c.modified = true;

                    this.forceUpdate();
                }
            }
        }
    }


}



export interface EntityGridItemProps {
    title: React.ReactElement<any>;
    bsStyle: string;
    children?: React.ReactNode;

    onResizerDragStart?: (resizer: "left" | "right", e: React.DragEvent) => void;
    onTitleDragStart?: (e: React.DragEvent) => void;
    onRemove?: (e: React.MouseEvent) => void;
}


export class EntityGridItem extends React.Component<EntityGridItemProps, void>{

    render() {

        return (
            <div className={"panel panel-" + (this.props.bsStyle ? this.props.bsStyle.toLowerCase() : "default") }>
                <div className="panel-heading form-inline" draggable={!!this.props.onTitleDragStart}
                    onDragStart={this.props.onTitleDragStart} >
                    {this.props.onRemove &&
                        <a className="sf-line-button sf-remove pull-right" onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString() }>
                            <span className="glyphicon glyphicon-remove"></span>
                        </a>
                    }
                    {this.props.title}
                </div>
                <div className="panel-body">
                    { this.props.children  }
                </div>
                {this.props.onResizerDragStart &&
                    <div className="sf-leftHandle" draggable={true}
                        onDragStart={e => this.props.onResizerDragStart("left", e) }>
                    </div>
                }
                {this.props.onResizerDragStart &&
                    <div className="sf-rightHandle" draggable={true}
                        onDragStart={e => this.props.onResizerDragStart("right", e) }>
                    </div>
                }
            </div>
        );

    }
}


