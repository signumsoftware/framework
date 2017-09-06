
import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { TypeContext, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, { FileTypeSymbol } from '../../Files/FileLine'
import * as DashboardClient from '../DashboardClient'
import { DashboardEntity, PanelPartEmbedded, IPartEntity } from '../Signum.Entities.Dashboard'



import "../Dashboard.css"


export default class DashboardView extends React.Component<{ dashboard: DashboardEntity, entity?: Entity }> {

    render() {
        if (this.props.dashboard.combineSimilarRows)
            return this.renderCombinedRows();
        else
            return this.renderBasic();

    }


    renderBasic() {
        const db = this.props.dashboard;

        const ctx = TypeContext.root(db);

        return (
            <div>
                {
                    mlistItemContext(ctx.subCtx(a => a.parts))
                        .groupBy(c => c.value.row!.toString())
                        .orderBy(gr => gr.key)
                        .map(gr =>
                            <div className="row row-control-panel" key={"row" + gr.key}>
                                {gr.elements.orderBy(ctx => ctx.value.startColumn).map((c, j, list) => {

                                    const prev = j == 0 ? undefined : list[j - 1].value;

                                    const offset = c.value.startColumn! - (prev ? (prev.startColumn! + prev.columns!) : 0);

                                    return (
                                        <div key={j} className={`col-sm-${c.value.columns} col-sm-offset-${offset}`}>
                                            <PanelPart ctx={c} entity={this.props.entity} />
                                        </div>
                                    );
                                })}
                            </div>)
                }
            </div>
        );
    }


    renderCombinedRows() {
        const db = this.props.dashboard;
        const ctx = TypeContext.root(db);

        var rows = mlistItemContext(ctx.subCtx(a => a.parts))
            .groupBy(c => c.value.row!.toString())
            .orderBy(g => g.key)
            .map(g => ({
                columns: g.elements.orderBy(a => a.value.startColumn).map(p => ({
                    startColumn: p.value.startColumn,
                    columnWidth: p.value.columns,
                    parts: [p],
                }) as CombinedColumn)
            }) as CombinedRow);

        debugger;

        var combinedRows = combineRows(rows);

        return (
            <div>
                {combinedRows.map((r, i) =>
                    <div className="row row-control-panel" key={"row" + i}>
                        {r.columns.orderBy(ctx => ctx.startColumn).map((c, j, list) => {

                            const last = j == 0 ? undefined : list[j - 1];

                            const offset = c.startColumn! - (last ? (last.startColumn! + last.columnWidth!) : 0);

                            return (
                                <div key={j} className={`col-sm-${c.columnWidth} col-sm-offset-${offset}`}>
                                    {c.parts.map(p => <PanelPart ctx={p} entity={this.props.entity} />)}
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>
        );
    }
}

function combineRows(rows: CombinedRow[]): CombinedRow[] {

    const newRows: CombinedRow[] = [];

    for (let i = 0; i < rows.length; i++) {

        const row = {
            columns: rows[i].columns.map(c =>
                ({
                    startColumn: c.startColumn,
                    columnWidth: c.columnWidth,
                    parts: [...c.parts]
                }) as CombinedColumn)
        } as CombinedRow;

        newRows.push(row);

        for (let j = 1; i + j < rows.length; j++) {

            if (!tryCombine(row, rows[i + j])) {
                i = i + j - 1;
                break;
            }

        }
    }

    return newRows;
}

function tryCombine(row: CombinedRow, newRow: CombinedRow): boolean {
    if (!newRow.columns.every(nc =>
        row.columns.some(c => identical(nc, c)) ||
        !row.columns.some(c => overlaps(nc, c))))
        return false;

    newRow.columns.forEach(nc => {
        var c = row.columns.singleOrNull(c => identical(c, nc));

        if (c)
            c.parts.push(...nc.parts);
        else
            row.columns.push(nc);
    });

    return true;
}

export function identical(col1: CombinedColumn, col2: CombinedColumn): boolean {
    return col1.startColumn == col2.startColumn && col1.columnWidth == col2.columnWidth;
}

export function overlaps(col1: CombinedColumn, col2: CombinedColumn): boolean {

    var columnEnd1 = col1.startColumn + col1.columnWidth;
    var columnEnd2 = col2.startColumn + col2.columnWidth;


    return !(columnEnd1 <= col2.startColumn || columnEnd2 <= col1.startColumn);

}


interface CombinedRow {
    columns: CombinedColumn[];
}

interface CombinedColumn {
    startColumn: number;
    columnWidth: number;

    parts: TypeContext<PanelPartEmbedded>[];
}

export interface PanelPartProps {
    ctx: TypeContext<PanelPartEmbedded>;
    entity?: Entity;
}

export interface PanelPartState {
    component?: React.ComponentClass<DashboardClient.PanelPartContentProps<IPartEntity>>;
    lastType?: string;
}


export class PanelPart extends React.Component<PanelPartProps, PanelPartState>{

    state = { component: undefined, lastType: undefined } as PanelPartState;

    componentWillMount() {
        this.loadComponent(this.props);
    }

    componentWillReceiveProps(nextProps: PanelPartProps): void {

        if (this.state.lastType != nextProps.ctx.value.content!.Type) {
            this.loadComponent(nextProps);
        }
    }

    loadComponent(props: PanelPartProps) {
        const content = props.ctx.value.content!;
        this.setState({ component: undefined, lastType: undefined })
        DashboardClient.partRenderers[content.Type].component()
            .then(c => this.setState({ component: c, lastType: content.Type }))
            .done();
    }

    render() {

        if (!this.state.component)
            return null;

        const p = this.props.ctx.value;
        const content = p.content!;

        const renderer = DashboardClient.partRenderers[content.Type];

        const lite = this.props.entity ? toLite(this.props.entity) : undefined

        const title = p.iconName ? <span><span className={p.iconName || undefined} style={{ color: p.iconColor || undefined }} />&nbsp;{p.title || getToString(content)}</span > :
            (p.title || getToString(content));

        return (
            <div className={classes("panel", "panel-" + (p.style == undefined ? "default" : p.style.firstLower()))}>
                <div className="panel-heading sf-show-hover">
                    {renderer.handleEditClick &&
                        <a className="sf-pointer pull-right sf-hide" onMouseUp={e => renderer.handleEditClick!(content, lite, e)}>
                            <span className="glyphicon glyphicon-edit"></span>&nbsp;Edit
                        </a>}
                    &nbsp;
                    {renderer.handleTitleClick == undefined ? title : <a className="sf-pointer" onMouseUp={e => renderer.handleTitleClick!(content, lite, e)}>{title}</a>}

                </div>
                <div className="panel-body">
                    {
                        React.createElement(this.state.component, {
                            part: content,
                            entity: lite,
                        } as DashboardClient.PanelPartContentProps<IPartEntity>)
                    }
                </div>
            </div>
        );
    }
}



