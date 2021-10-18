import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Entity, getToString, is, Lite, MListElement, toLite } from '@framework/Signum.Entities'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import * as DashboardClient from '../DashboardClient'
import { DashboardEntity, PanelPartEmbedded, IPartEntity } from '../Signum.Entities.Dashboard'
import "../Dashboard.css"
import { ErrorBoundary } from '@framework/Components';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import { translated } from '../../Translation/TranslatedInstanceTools'
import { FilterConditionOptionParsed, FilterGroupOptionParsed, FilterOption, FilterOptionParsed, QueryToken, ResultRow } from '@framework/FindOptions'
import { ChartRow } from '../../Chart/ChartClient'
import { FilterConditionComponent } from '../../../Signum.React/Scripts/SearchControl/FilterBuilder'
import { FilterGroupOperation } from '../../../Signum.React/Scripts/Signum.Entities.DynamicQuery'
import { ChartRequestModel } from '../../Chart/Signum.Entities.Chart'


export class DashboardFilterController {


  forceUpdate: () => void;

  filters: Map<PanelPartEmbedded, DashboardFilter> = new Map();
  lastChange: Map<string /*queryKEy*/, number> = new Map();

  constructor(forceUpdate: () => void) {
    this.forceUpdate = forceUpdate;
  }

  setFilter(filter: DashboardFilter) {
    this.lastChange.set(filter.queryKey, new Date().getTime());
    this.filters.set(filter.partEmbedded, filter);
    this.forceUpdate();
  }

  clear(partEmbedded: PanelPartEmbedded) {
    this.filters.delete(partEmbedded);
    this.forceUpdate();
  }
  
  getFilterOptions(partEmbedded: PanelPartEmbedded, queryKey: string): FilterOptionParsed[] {
    var otherFilters = Array.from(this.filters.values()).filter(f => f.partEmbedded != partEmbedded && f.rows?.length);

    var result = otherFilters.filter(a => a.queryKey == queryKey).map(
      df => groupFilter("Or", df.rows.map(
        r => groupFilter("And", r.filters.map(
          f => ({ token: f.token, operation: "EqualTo", value: f.value, frozen: false }) as FilterConditionOptionParsed
        ))
      ).notNull())
    ).notNull();

    return result;
  }

  

}

function groupFilter(groupOperation: FilterGroupOperation, filters: FilterOptionParsed[]): FilterOptionParsed | undefined {

  if (filters.length == 0)
    return undefined;

  if (filters.length == 1)
    return filters[0];

  return ({
    groupOperation: groupOperation,
    filters: filters
  }) as FilterGroupOptionParsed;
}

export interface DashboardFilter {
  queryKey: string;
  rows: DashboardFilterRow[];
  partEmbedded: PanelPartEmbedded;
}

export interface DashboardFilterRow {
  filters: { token: QueryToken, value: unknown }[];
}

export function equalsDFR(row1: DashboardFilterRow, row2: DashboardFilterRow): boolean {
  if (row1.filters.length != row2.filters.length)
    return false;

  for (var i = 0; i < row1.filters.length; i++) {
    var f1 = row1.filters[i];
    var f2 = row2.filters[i];

    if (!(f1.token.fullKey == f2.token.fullKey &&
      (f1.value === f2.value || is(f1.value as Lite<Entity>, f2.value as Lite<Entity>, false, false))))
      return false;
  }

  return true;
}

export default function DashboardView(p: { dashboard: DashboardEntity, entity?: Entity, deps?: React.DependencyList; }) {

  const forceUpdate = useForceUpdate();
  var filterController = React.useMemo(() => new DashboardFilterController(forceUpdate), [p.dashboard]);


  function renderBasic() {
    const db = p.dashboard;
    const ctx = TypeContext.root(db);

    return (
      <div className="sf-dashboard-view">
        {
          mlistItemContext(ctx.subCtx(a => a.parts))
            .groupBy(c => c.value.row!.toString())
            .orderBy(gr => Number(gr.key))
            .map(gr =>
              <div className="row row-control-panel" key={"row" + gr.key}>
                {gr.elements.orderBy(ctx => ctx.value.startColumn).map((c, j, list) => {

                  const prev = j == 0 ? undefined : list[j - 1].value;

                  const offset = c.value.startColumn! - (prev ? (prev.startColumn! + prev.columns!) : 0);

                  return (
                    <div key={j} className={`col-sm-${c.value.columns} offset-sm-${offset}`}>
                      <PanelPart ctx={c} entity={p.entity} filterController={filterController} />
                    </div>
                  );
                })}
              </div>)
        }
      </div>
    );
  }

  function renderCombinedRows() {
    const db = p.dashboard;
    const ctx = TypeContext.root(db);

    var rows = mlistItemContext(ctx.subCtx(a => a.parts))
      .groupBy(c => c.value.row!.toString())
      .orderBy(g => Number(g.key))
      .map(g => ({
        columns: g.elements.orderBy(a => a.value.startColumn).map(p => ({
          startColumn: p.value.startColumn,
          columnWidth: p.value.columns,
          parts: [p],
        }) as CombinedColumn)
      }) as CombinedRow);

    var combinedRows = combineRows(rows);

    return (
      <div className="sf-dashboard-view">
        {combinedRows.map((r, i) =>
          <div className="row row-control-panel" key={"row" + i}>
            {r.columns.orderBy(ctx => ctx.startColumn).map((c, j, list) => {
              const last = j == 0 ? undefined : list[j - 1];
              const offset = c.startColumn! - (last ? (last.startColumn! + last.columnWidth!) : 0);
              return (
                <div key={j} className={`col-sm-${c.columnWidth} offset-sm-${offset}`}>
                  {c.parts.map((pctx, i) => <PanelPart key={i} ctx={pctx} entity={p.entity} filterController={filterController} />)}
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  }


  if (p.dashboard.combineSimilarRows)
    return renderCombinedRows();
  else
    return renderBasic();
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
    let j = 1;
    for (; i + j < rows.length; j++) {
      if (!tryCombine(row, rows[i + j])) {
        break;
      }
    }

    i = i + j - 1;
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
  deps?: React.DependencyList;
  filterController: DashboardFilterController;
}

export interface PanelPartState {
  component?: React.ComponentClass<DashboardClient.PanelPartContentProps<IPartEntity>>;
  lastType?: string;
}


export function PanelPart(p: PanelPartProps) {
  const content = p.ctx.value.content;

  const state = useAPI(signal => DashboardClient.partRenderers[content.Type].component().then(c => ({ component: c, lastType: content.Type })),
    [content.Type], { avoidReset: true });

  if (state == null || state.lastType == null)
    return null;

  const part = p.ctx.value;

  const renderer = DashboardClient.partRenderers[content.Type];

  const lite = p.entity ? toLite(p.entity) : undefined;

  if (renderer.withPanel && !renderer.withPanel(content)) {
    return React.createElement(state.component, {
      partEmbedded: part,
      part: content,
      entity: lite,
      deps: p.deps,
      filterController: p.filterController
    });
  }

  const titleText = translated(part, p => p.title) ?? (renderer.defaultTitle ? renderer.defaultTitle(content) : getToString(content));
  const defaultIcon = renderer.defaultIcon(content);
  const icon = coalesceIcon(parseIcon(part.iconName), defaultIcon?.icon);
  const color = part.iconColor ?? defaultIcon?.iconColor;

  const title = !icon ? titleText :
    <span>
      <FontAwesomeIcon icon={icon} color={color} />&nbsp;{titleText}
    </span>;

  var style = part.style == undefined ? undefined : part.style.toLowerCase();

  return (
    <div className={classes("card", style && ("border-" + style), "shadow-sm", "mb-4")}>
      <div className={classes("card-header", "sf-show-hover",
        style && style != "light" && "text-white",
        style && ("bg-" + style)
      )}>
        {renderer.handleEditClick &&
          <a className="sf-pointer float-right flip sf-hide" onMouseUp={e => renderer.handleEditClick!(content, lite, e)}>
            <FontAwesomeIcon icon="edit" />&nbsp;Edit
          </a>
        }
        &nbsp;
      {renderer.handleTitleClick == undefined ? title :
          <a className="sf-pointer" onMouseUp={e => renderer.handleTitleClick!(content, lite, e)}>{title}</a>
        }
      </div>
      <div className="card-body py-2 px-3">
        <ErrorBoundary>
          {
            React.createElement(state.component, {
              partEmbedded: part,
              part: content,
              entity: lite,
              deps: p.deps,
              filterController: p.filterController
            } as DashboardClient.PanelPartContentProps<IPartEntity>)
          }
        </ErrorBoundary>
      </div>
    </div>
  );
}
