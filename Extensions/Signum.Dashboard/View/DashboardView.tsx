import * as React from 'react'
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, getColorContrasColorBWByHex} from '@framework/Globals'
import { Entity, getToString, toLite, translated } from '@framework/Signum.Entities'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { DashboardClient, PanelPartContentProps } from '../DashboardClient'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, DashboardMessage } from '../Signum.Dashboard'
import "../Dashboard.css"
import { ErrorBoundary } from '@framework/Components';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { fallbackIcon, parseIcon } from '@framework/Components/IconTypeahead'
import { DashboardController } from './DashboardFilterController'
import { CachedQueryJS } from '../CachedQueryExecutor'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder'
import { Navigator } from '@framework/Navigator'

export default function DashboardView(p: { dashboard: DashboardEntity, cachedQueries: { [userAssetKey: string]: Promise<CachedQueryJS> }, entity?: Entity, embedded?: boolean, deps?: React.DependencyList; reload: () => void; hideEditButton?: boolean }): React.JSX.Element {

  const forceUpdate = useForceUpdate();
  const dashboardController = React.useMemo(() => new DashboardController(forceUpdate, p.dashboard), [p.dashboard]);
  dashboardController.setIsLoading();

  function renderBasic() {
    const db = p.dashboard;
    const ctx = TypeContext.root(db);
  
    return (
      <div>
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
                        <PanelPart ctx={c} entity={p.entity}
                          dashboardController={dashboardController} reload={p.reload} cachedQueries={p.cachedQueries} deps={p.deps} />
                      </div>
                    );
                  })}
                </div>)
          }
        </div>
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
                <div key={j} className={`col-sm-${c.columnWidth} offset-sm-${offset}`} style={{ display: "flex", flexDirection: "column" }}>
                  {c.parts.map((pctx, i) => <PanelPart key={i} ctx={pctx} entity={p.entity} dashboardController={dashboardController} reload={p.reload} cachedQueries={p.cachedQueries} deps={p.deps} flex />)}
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  }


  return (
    <div className={p.embedded ? "sf-dashboard-view-embedded" : undefined}>
      {p.hideEditButton != true && !Navigator.isReadOnly(DashboardEntity) &&
        <div className="d-flex flex-row-reverse m-1">
          <Link className="sf-hide" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(p.dashboard)} title={DashboardMessage.Edit.niceToString()}>
            <FontAwesomeIcon aria-hidden={true} icon="pen-to-square" />
          </Link>
        </div>}
      <div>
        {dashboardController.pinnedFilters.size > 0 && <PinnedFilterBuilder
          filterOptions={Array.from(dashboardController.pinnedFilters.values()).flatMap(a => a.pinnedFilters)}
          onFiltersChanged={forceUpdate} />}
        {
          p.dashboard.combineSimilarRows ?
            renderCombinedRows() :
            renderBasic()
        }
      </div>
    </div>
  );
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
  dashboardController: DashboardController;
  flex?: boolean;
  reload: () => void;
  cachedQueries: { [userAssetKey: string]: Promise<CachedQueryJS>, }
}

export function PanelPart(p: PanelPartProps): React.JSX.Element | null {
  const content = p.ctx.value.content;

  const customDataRef = React.useRef<any>(undefined);

  const state = useAPI(signal => DashboardClient.partRenderers[content.Type].component().then(c => ({ component: c, lastType: content.Type })),
    [content.Type], { avoidReset: true });

  if (state == null || state.lastType == null)
    return null;

  const part = p.ctx.value;

  const renderer = DashboardClient.partRenderers[content.Type];

  const lite = p.entity ? toLite(p.entity) : undefined;

  if (renderer.withPanel && !renderer.withPanel(content, lite)) {
    return (
      <ErrorBoundary>
        {React.createElement(state.component, {
          partEmbedded: part,
          content: content,
          entity: lite,
          deps: p.deps,
          dashboardController: p.dashboardController,
          cachedQueries: p.cachedQueries,
          customDataRef: customDataRef,
        } as PanelPartContentProps<IPartEntity>)}
      </ErrorBoundary >
    );
  }

  const titleText = translated(part, p => p.title) ?? (renderer.defaultTitle ? renderer.defaultTitle(content) : getToString(content));
  const icon = parseIcon(part.iconName);
  const iconColor = part.iconColor;

  const title = !icon ? titleText :
    <span>
      <FontAwesomeIcon aria-hidden={true} icon={fallbackIcon(icon)} color={iconColor ?? undefined} className="me-1" />{titleText}
    </span>;

  var dashboardFilter = p.dashboardController?.filters.get(p.ctx.value);

  function handleClearFilter(e: React.MouseEvent) {
    p.dashboardController.clearFilters(p.ctx.value);
  }

  return (
    <div className={classes("card", !part.customColor && "border-tertiary", "shadow-sm", "mb-4")} style={{ flex: p.flex ? 1 : undefined, overflow: "hidden" }}>
      <div className={classes("card-header fw-bold", "sf-show-hover", "d-flex", !part.customColor)}
        style={{ backgroundColor: part.customColor ?? undefined, color: part.customColor ? getColorContrasColorBWByHex(part.customColor) : undefined}}
      >

        {renderer.handleTitleClick == undefined ? title :
          <a className="sf-pointer"
            style={{ color: part.titleColor ?? (part.customColor ? getColorContrasColorBWByHex(part.customColor) : undefined), textDecoration: "none" }}
            onClick={e => { e.preventDefault(); renderer.handleTitleClick!(content, lite, customDataRef, e); }}>
          {title}
          </a>
        }
        {
          dashboardFilter && <span className="badge bg-tertiary text-dark border ms-2 sf-filter-pill">
            {dashboardFilter.rows.length} {DashboardMessage.RowsSelected.niceToString().forGenderAndNumber(dashboardFilter.rows.length)}
            <button type="button" aria-label="Close" className="btn-close" onClick={handleClearFilter}/>
          </span>
        }

        <div className="ms-auto">
          {renderer.customTitleButtons?.(content, lite, customDataRef)}
          {
            renderer.handleEditClick &&
            <a role="button" tabIndex={0} className="sf-pointer sf-hide" onClick={e => { e.preventDefault(); renderer.handleEditClick!(content, lite, customDataRef, e).then(v => v && p.reload()); }} title={DashboardMessage.Edit.niceToString()}>
              <FontAwesomeIcon aria-hidden={true} icon="pen-to-square" className="me-1" />
            </a>
          }
        </div>
      </div>
      <div className="card-body py-2 px-3 d-flex flex-column">
        <ErrorBoundary>
          {
            React.createElement(state.component, {
              partEmbedded: part,
              content: content,
              entity: lite,
              deps: p.deps,
              dashboardController: p.dashboardController,
              cachedQueries: p.cachedQueries,
              customDataRef: customDataRef,
            } as PanelPartContentProps<IPartEntity>)
          }
        </ErrorBoundary>
      </div>
    </div>
  );
}
